using System;
using RuleForge.DSL;
using RuleForge.Validation;

namespace RuleForge.AI
{
    public sealed class AIGenerationTelemetry
    {
        public AIGenerationTelemetry(
            string requestType,
            string provider,
            bool benchmarkEligible,
            string prompt,
            double generationTimeMs,
            bool parseSuccess,
            ChallengeSpec challenge,
            ValidationResult validation,
            BalanceEvaluation balance,
            string resultMessage)
        {
            RequestType = requestType ?? string.Empty;
            Provider = provider ?? string.Empty;
            BenchmarkEligible = benchmarkEligible;
            Prompt = prompt ?? string.Empty;
            GenerationTimeMs = Math.Max(0d, generationTimeMs);
            ParseSuccess = parseSuccess;
            Challenge = challenge;
            Validation = validation;
            Balance = balance;
            ResultMessage = resultMessage ?? string.Empty;
        }

        public string RequestType { get; }
        public string Provider { get; }
        public bool BenchmarkEligible { get; }
        public string Prompt { get; }
        public double GenerationTimeMs { get; }
        public bool ParseSuccess { get; }
        public ChallengeSpec Challenge { get; }
        public ValidationResult Validation { get; }
        public BalanceEvaluation Balance { get; }
        public string ResultMessage { get; }
    }
}
