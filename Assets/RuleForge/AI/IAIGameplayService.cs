using System;
using System.Collections;
using RuleForge.DSL;

namespace RuleForge.AI
{
    public interface IAIGameplayService
    {
        string ProviderName { get; }
        AIProviderKind ProviderKind { get; }
        bool IsConfigured { get; }
        bool IsBenchmarkEligible { get; }

        IEnumerator GenerateChallenge(
            AIChallengeGenerationRequest request,
            Action<AIGameplayResult<ChallengeSpec>> onComplete);

        IEnumerator ModifyChallenge(
            AIChallengeModificationRequest request,
            Action<AIGameplayResult<ChallengePatch>> onComplete);
    }

    public enum AIProviderKind
    {
        Unknown,
        Mock,
        Real
    }

    [Serializable]
    public sealed class AIChallengeGenerationRequest
    {
        public AIChallengeGenerationRequest(
            string prompt,
            string preferences,
            string vocabulary,
            string outputSchema)
        {
            UserPrompt = prompt ?? string.Empty;
            CreatorPreferences = preferences ?? string.Empty;
            GameplayVocabulary = vocabulary ?? string.Empty;
            OutputSchema = outputSchema ?? string.Empty;
        }

        public string UserPrompt { get; }
        public string CreatorPreferences { get; }
        public string GameplayVocabulary { get; }
        public string OutputSchema { get; }
    }

    [Serializable]
    public sealed class AIChallengeModificationRequest
    {
        public AIChallengeModificationRequest(
            string prompt,
            ChallengeSpec currentChallenge,
            string preferences,
            string vocabulary,
            string outputSchema)
        {
            UserPrompt = prompt ?? string.Empty;
            CurrentChallenge = currentChallenge;
            CreatorPreferences = preferences ?? string.Empty;
            GameplayVocabulary = vocabulary ?? string.Empty;
            OutputSchema = outputSchema ?? string.Empty;
        }

        public string UserPrompt { get; }
        public ChallengeSpec CurrentChallenge { get; }
        public string CreatorPreferences { get; }
        public string GameplayVocabulary { get; }
        public string OutputSchema { get; }
    }

    public sealed class AIGameplayResult<T> where T : class
    {
        private AIGameplayResult(bool success, T value, string error)
        {
            Success = success;
            Value = value;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }
        public T Value { get; }
        public string Error { get; }

        public static AIGameplayResult<T> Succeeded(T value)
        {
            return value != null
                ? new AIGameplayResult<T>(true, value, string.Empty)
                : Failed("AI provider returned no structured value.");
        }

        public static AIGameplayResult<T> Failed(string error)
        {
            return new AIGameplayResult<T>(false, null, error);
        }
    }
}
