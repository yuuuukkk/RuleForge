using System;

namespace RuleForge.Analytics
{
    [Serializable]
    public sealed class AIGenerationRecord
    {
        public string timestampUtc;
        public string requestType;
        public string provider;
        public string prompt;
        public float generationTimeMs;
        public bool parseSuccess;
        public bool validationPassed;
        public string validationResult;
        public int ruleCount;
        public float rewardScore;
        public float penaltyScore;
        public bool isRetry;
        public bool benchmarkEligible;

        public bool Successful => parseSuccess && validationPassed;
    }

    [Serializable]
    public sealed class GameplaySessionRecord
    {
        public string timestampUtc;
        public string challengeId;
        public string challenge;
        public float playDurationSeconds;
        public bool victory;
        public string outcome;
        public int kills;
        public int headshots;
        public int rulesTriggered;
    }

    [Serializable]
    public sealed class AnalyticsSummary
    {
        public string generatedAtUtc;
        public int totalAIRequests;
        public int benchmarkAIRequests;
        public int generationBenchmarkRequests;
        public int firstPassAttempts;
        public int firstPassSuccesses;
        public float firstPassGenerationSuccessRate;
        public int parsedBenchmarkOutputs;
        public int validationRejections;
        public float validationRejectionRate;
        public int retryAttempts;
        public int retrySuccesses;
        public float retrySuccessRate;
        public float averageGenerationLatencyMs;
        public int gameplaySessions;
        public int victories;
        public float victoryRate;
        public float averagePlayDurationSeconds;
        public int totalKills;
        public int totalHeadshots;
        public int totalRulesTriggered;
    }
}
