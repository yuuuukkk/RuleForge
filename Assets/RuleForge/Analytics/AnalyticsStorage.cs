using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace RuleForge.Analytics
{
    public static class AnalyticsStorage
    {
        private const string FolderName = "RuleForgeAnalytics";
        private const string AILogName = "ai_generation.jsonl";
        private const string AICsvName = "ai_generation.csv";
        private const string GameplayLogName = "gameplay_sessions.jsonl";
        private const string GameplayCsvName = "gameplay_sessions.csv";
        private const string SummaryName = "analytics_summary.json";

        private static readonly Dictionary<string, bool> LatestAttemptSuccess =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        private static AnalyticsSummary cachedSummary;
        private static bool summaryLoaded;
        private static bool attemptIndexLoaded;

        public static string OutputDirectory =>
            Path.Combine(Application.persistentDataPath, FolderName);

        public static bool AppendAIRecord(
            AIGenerationRecord record,
            out string error)
        {
            if (record == null)
            {
                error = "AI generation record is null.";
                return false;
            }

            try
            {
                EnsureDirectory();
                AnalyticsSummary summary = GetSummary();
                AppendJsonLine(Path.Combine(OutputDirectory, AILogName), record);
                AppendCsv(
                    Path.Combine(OutputDirectory, AICsvName),
                    "Timestamp UTC,Request Type,Provider,Prompt,Generation Time Ms," +
                    "Parse Success,Validation Passed,Validation Result,Rule Count," +
                    "Reward Score,Penalty Score,Is Retry,Benchmark Eligible",
                    string.Join(",", new[]
                    {
                        Csv(record.timestampUtc),
                        Csv(record.requestType),
                        Csv(record.provider),
                        Csv(record.prompt),
                        Number(record.generationTimeMs),
                        Bool(record.parseSuccess),
                        Bool(record.validationPassed),
                        Csv(record.validationResult),
                        record.ruleCount.ToString(CultureInfo.InvariantCulture),
                        Number(record.rewardScore),
                        Number(record.penaltyScore),
                        Bool(record.isRetry),
                        Bool(record.benchmarkEligible)
                    }));
                Accumulate(summary, record);
                UpdateAttemptIndex(record);
                WriteSummary(summary);
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                summaryLoaded = false;
                attemptIndexLoaded = false;
                error = exception.Message;
                return false;
            }
        }

        public static bool AppendGameplayRecord(
            GameplaySessionRecord record,
            out string error)
        {
            if (record == null)
            {
                error = "Gameplay session record is null.";
                return false;
            }

            try
            {
                EnsureDirectory();
                AnalyticsSummary summary = GetSummary();
                AppendJsonLine(
                    Path.Combine(OutputDirectory, GameplayLogName),
                    record);
                AppendCsv(
                    Path.Combine(OutputDirectory, GameplayCsvName),
                    "Timestamp UTC,Challenge ID,Challenge,Play Duration Seconds," +
                    "Victory,Outcome,Kills,Headshots,Rules Triggered",
                    string.Join(",", new[]
                    {
                        Csv(record.timestampUtc),
                        Csv(record.challengeId),
                        Csv(record.challenge),
                        Number(record.playDurationSeconds),
                        Bool(record.victory),
                        Csv(record.outcome),
                        record.kills.ToString(CultureInfo.InvariantCulture),
                        record.headshots.ToString(CultureInfo.InvariantCulture),
                        record.rulesTriggered.ToString(CultureInfo.InvariantCulture)
                    }));
                Accumulate(summary, record);
                WriteSummary(summary);
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                summaryLoaded = false;
                error = exception.Message;
                return false;
            }
        }

        public static bool WasPreviousMatchingAttemptFailure(
            string requestType,
            string provider,
            string prompt)
        {
            EnsureAttemptIndexLoaded();
            return LatestAttemptSuccess.TryGetValue(
                       BuildAttemptKey(requestType, provider, prompt),
                       out bool successful) &&
                   !successful;
        }

        public static AnalyticsSummary GetSummary()
        {
            if (summaryLoaded && cachedSummary != null)
            {
                return cachedSummary;
            }

            EnsureDirectory();
            string path = Path.Combine(OutputDirectory, SummaryName);
            if (File.Exists(path) && !LogsAreNewerThan(path))
            {
                try
                {
                    cachedSummary = JsonUtility.FromJson<AnalyticsSummary>(
                        File.ReadAllText(path, Encoding.UTF8));
                }
                catch (Exception)
                {
                    cachedSummary = null;
                }
            }

            if (cachedSummary == null)
            {
                return RebuildSummary();
            }

            summaryLoaded = true;
            return cachedSummary;
        }

        private static bool LogsAreNewerThan(string summaryPath)
        {
            DateTime summaryWriteTime = File.GetLastWriteTimeUtc(summaryPath);
            string aiPath = Path.Combine(OutputDirectory, AILogName);
            string gameplayPath = Path.Combine(OutputDirectory, GameplayLogName);
            return (File.Exists(aiPath) &&
                    File.GetLastWriteTimeUtc(aiPath) > summaryWriteTime) ||
                   (File.Exists(gameplayPath) &&
                    File.GetLastWriteTimeUtc(gameplayPath) > summaryWriteTime);
        }

        public static AnalyticsSummary RebuildSummary()
        {
            EnsureDirectory();
            List<AIGenerationRecord> aiRecords = LoadJsonLines<AIGenerationRecord>(
                Path.Combine(OutputDirectory, AILogName));
            List<GameplaySessionRecord> gameplayRecords =
                LoadJsonLines<GameplaySessionRecord>(
                    Path.Combine(OutputDirectory, GameplayLogName));
            AnalyticsSummary summary = CalculateSummary(aiRecords, gameplayRecords);
            cachedSummary = summary;
            summaryLoaded = true;
            RebuildAttemptIndex(aiRecords);
            WriteSummary(summary);
            return summary;
        }

        private static void Accumulate(
            AnalyticsSummary summary,
            AIGenerationRecord record)
        {
            if (summary == null || record == null)
            {
                return;
            }

            summary.generatedAtUtc = UtcNow();
            summary.totalAIRequests++;
            if (!record.benchmarkEligible)
            {
                return;
            }

            summary.benchmarkAIRequests++;
            if (!string.Equals(
                    record.requestType,
                    "Generate",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int previousGenerationCount = summary.generationBenchmarkRequests;
            summary.generationBenchmarkRequests++;
            summary.averageGenerationLatencyMs =
                (summary.averageGenerationLatencyMs * previousGenerationCount +
                 Math.Max(0f, record.generationTimeMs)) /
                summary.generationBenchmarkRequests;

            if (record.isRetry)
            {
                summary.retryAttempts++;
                if (record.Successful)
                {
                    summary.retrySuccesses++;
                }
            }
            else
            {
                summary.firstPassAttempts++;
                if (record.Successful)
                {
                    summary.firstPassSuccesses++;
                }
            }

            if (record.parseSuccess)
            {
                summary.parsedBenchmarkOutputs++;
                if (!record.validationPassed)
                {
                    summary.validationRejections++;
                }
            }

            RefreshRates(summary);
        }

        private static void Accumulate(
            AnalyticsSummary summary,
            GameplaySessionRecord record)
        {
            if (summary == null || record == null)
            {
                return;
            }

            summary.generatedAtUtc = UtcNow();
            int previousSessionCount = summary.gameplaySessions;
            summary.gameplaySessions++;
            summary.averagePlayDurationSeconds =
                (summary.averagePlayDurationSeconds * previousSessionCount +
                 Math.Max(0f, record.playDurationSeconds)) /
                summary.gameplaySessions;
            if (record.victory)
            {
                summary.victories++;
            }

            summary.totalKills += Math.Max(0, record.kills);
            summary.totalHeadshots += Math.Max(0, record.headshots);
            summary.totalRulesTriggered += Math.Max(0, record.rulesTriggered);
            RefreshRates(summary);
        }

        private static void RefreshRates(AnalyticsSummary summary)
        {
            summary.firstPassGenerationSuccessRate = Rate(
                summary.firstPassSuccesses,
                summary.firstPassAttempts);
            summary.validationRejectionRate = Rate(
                summary.validationRejections,
                summary.parsedBenchmarkOutputs);
            summary.retrySuccessRate = Rate(
                summary.retrySuccesses,
                summary.retryAttempts);
            summary.victoryRate = Rate(
                summary.victories,
                summary.gameplaySessions);
        }

        private static void WriteSummary(AnalyticsSummary summary)
        {
            AnalyticsSummary value = summary ?? new AnalyticsSummary();
            File.WriteAllText(
                Path.Combine(OutputDirectory, SummaryName),
                JsonUtility.ToJson(value, true),
                new UTF8Encoding(false));
            cachedSummary = value;
            summaryLoaded = true;
        }

        private static void EnsureAttemptIndexLoaded()
        {
            if (attemptIndexLoaded)
            {
                return;
            }

            List<AIGenerationRecord> records = LoadJsonLines<AIGenerationRecord>(
                Path.Combine(OutputDirectory, AILogName));
            RebuildAttemptIndex(records);
        }

        private static void RebuildAttemptIndex(
            IReadOnlyList<AIGenerationRecord> records)
        {
            LatestAttemptSuccess.Clear();
            if (records != null)
            {
                for (int index = 0; index < records.Count; index++)
                {
                    UpdateAttemptIndex(records[index]);
                }
            }

            attemptIndexLoaded = true;
        }

        private static void UpdateAttemptIndex(AIGenerationRecord record)
        {
            if (record == null)
            {
                return;
            }

            LatestAttemptSuccess[BuildAttemptKey(
                record.requestType,
                record.provider,
                record.prompt)] = record.Successful;
        }

        private static string BuildAttemptKey(
            string requestType,
            string provider,
            string prompt)
        {
            return (requestType ?? string.Empty).ToUpperInvariant() + "\u001f" +
                   (provider ?? string.Empty).ToUpperInvariant() + "\u001f" +
                   NormalizePrompt(prompt);
        }

        private static string UtcNow()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        public static AnalyticsSummary CalculateSummary(
            IReadOnlyList<AIGenerationRecord> aiRecords,
            IReadOnlyList<GameplaySessionRecord> gameplayRecords)
        {
            AnalyticsSummary summary = new AnalyticsSummary
            {
                generatedAtUtc = UtcNow()
            };

            if (aiRecords != null)
            {
                for (int index = 0; index < aiRecords.Count; index++)
                {
                    Accumulate(summary, aiRecords[index]);
                }
            }

            if (gameplayRecords != null)
            {
                for (int index = 0; index < gameplayRecords.Count; index++)
                {
                    Accumulate(summary, gameplayRecords[index]);
                }
            }

            return summary;
        }

        private static List<T> LoadJsonLines<T>(string path) where T : class
        {
            List<T> records = new List<T>();
            if (!File.Exists(path))
            {
                return records;
            }

            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    T record = JsonUtility.FromJson<T>(line);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
                catch (ArgumentException)
                {
                    // Preserve the raw log and ignore only its malformed line.
                }
            }

            return records;
        }

        private static void AppendJsonLine<T>(string path, T record)
        {
            File.AppendAllText(
                path,
                JsonUtility.ToJson(record) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static void AppendCsv(
            string path,
            string header,
            string row)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                File.AppendAllText(
                    path,
                    header + Environment.NewLine,
                    new UTF8Encoding(false));
            }

            File.AppendAllText(
                path,
                row + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static void EnsureDirectory()
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        private static string Csv(string value)
        {
            string normalized = value ?? string.Empty;
            if (normalized.Length > 0 &&
                (normalized[0] == '=' || normalized[0] == '+' ||
                 normalized[0] == '-' || normalized[0] == '@'))
            {
                normalized = "'" + normalized;
            }

            return "\"" + normalized.Replace("\"", "\"\"") + "\"";
        }

        private static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static float Rate(int numerator, int denominator)
        {
            return denominator > 0 ? (float)numerator / denominator : 0f;
        }

        private static string NormalizePrompt(string prompt)
        {
            return (prompt ?? string.Empty).Trim();
        }
    }
}
