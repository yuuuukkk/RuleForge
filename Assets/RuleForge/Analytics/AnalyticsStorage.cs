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
                RebuildSummary();
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
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
                RebuildSummary();
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool WasPreviousMatchingAttemptFailure(
            string requestType,
            string provider,
            string prompt)
        {
            bool found = false;
            bool successful = false;
            List<AIGenerationRecord> records = LoadJsonLines<AIGenerationRecord>(
                Path.Combine(OutputDirectory, AILogName));
            for (int index = 0; index < records.Count; index++)
            {
                AIGenerationRecord record = records[index];
                if (record != null &&
                    Same(record.requestType, requestType) &&
                    Same(record.provider, provider) &&
                    string.Equals(
                        NormalizePrompt(record.prompt),
                        NormalizePrompt(prompt),
                        StringComparison.Ordinal))
                {
                    found = true;
                    successful = record.Successful;
                }
            }

            return found && !successful;
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
            File.WriteAllText(
                Path.Combine(OutputDirectory, SummaryName),
                JsonUtility.ToJson(summary, true),
                new UTF8Encoding(false));
            return summary;
        }

        public static AnalyticsSummary CalculateSummary(
            IReadOnlyList<AIGenerationRecord> aiRecords,
            IReadOnlyList<GameplaySessionRecord> gameplayRecords)
        {
            AnalyticsSummary summary = new AnalyticsSummary
            {
                generatedAtUtc = DateTime.UtcNow.ToString(
                    "o",
                    CultureInfo.InvariantCulture),
                totalAIRequests = aiRecords != null ? aiRecords.Count : 0,
                gameplaySessions = gameplayRecords != null
                    ? gameplayRecords.Count
                    : 0
            };

            double latencyTotal = 0d;
            if (aiRecords != null)
            {
                for (int index = 0; index < aiRecords.Count; index++)
                {
                    AIGenerationRecord record = aiRecords[index];
                    if (record == null || !record.benchmarkEligible)
                    {
                        continue;
                    }

                    summary.benchmarkAIRequests++;
                    if (!string.Equals(
                            record.requestType,
                            "Generate",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    summary.generationBenchmarkRequests++;
                    latencyTotal += Math.Max(0f, record.generationTimeMs);
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
                }
            }

            summary.firstPassGenerationSuccessRate = Rate(
                summary.firstPassSuccesses,
                summary.firstPassAttempts);
            summary.validationRejectionRate = Rate(
                summary.validationRejections,
                summary.parsedBenchmarkOutputs);
            summary.retrySuccessRate = Rate(
                summary.retrySuccesses,
                summary.retryAttempts);
            summary.averageGenerationLatencyMs =
                summary.generationBenchmarkRequests > 0
                    ? (float)(latencyTotal / summary.generationBenchmarkRequests)
                    : 0f;

            double durationTotal = 0d;
            if (gameplayRecords != null)
            {
                for (int index = 0; index < gameplayRecords.Count; index++)
                {
                    GameplaySessionRecord record = gameplayRecords[index];
                    if (record == null)
                    {
                        continue;
                    }

                    durationTotal += Math.Max(0f, record.playDurationSeconds);
                    if (record.victory)
                    {
                        summary.victories++;
                    }

                    summary.totalKills += Math.Max(0, record.kills);
                    summary.totalHeadshots += Math.Max(0, record.headshots);
                    summary.totalRulesTriggered += Math.Max(
                        0,
                        record.rulesTriggered);
                }
            }

            summary.victoryRate = Rate(
                summary.victories,
                summary.gameplaySessions);
            summary.averagePlayDurationSeconds =
                summary.gameplaySessions > 0
                    ? (float)(durationTotal / summary.gameplaySessions)
                    : 0f;
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

        private static bool Same(string left, string right)
        {
            return string.Equals(
                left ?? string.Empty,
                right ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePrompt(string prompt)
        {
            return (prompt ?? string.Empty).Trim();
        }
    }
}
