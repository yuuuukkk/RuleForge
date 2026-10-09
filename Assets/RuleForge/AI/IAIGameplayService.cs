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
        AIProviderConnectionState ConnectionState { get; }
        string ConnectionMessage { get; }

        IEnumerator VerifyConnection(Action<bool, string> onComplete);

        IEnumerator AnalyzeGameplay(
            AIProposalAnalysisRequest request,
            Action<AIGameplayResult<GameplayProposal>> onComplete);

        IEnumerator GenerateChallenge(
            AIChallengeGenerationRequest request,
            Action<AIGameplayResult<ChallengeSpec>> onComplete);

        IEnumerator ModifyChallenge(
            AIChallengeModificationRequest request,
            Action<AIGameplayResult<ChallengePatch>> onComplete);

        IEnumerator AnalyzeModification(
            AIModificationProposalRequest request,
            Action<AIGameplayResult<GameplayModificationProposal>> onComplete);

        IEnumerator AnalyzeImprovements(
            AIImprovementAnalysisRequest request,
            Action<AIGameplayResult<GameplayImprovementSet>> onComplete);

        IEnumerator RepairChallenge(
            AIChallengeRepairRequest request,
            Action<AIGameplayResult<GameplayRepairResult>> onComplete);

        IEnumerator RepairPatch(
            AIChallengePatchRepairRequest request,
            Action<AIGameplayResult<ChallengePatch>> onComplete);
    }

    public enum AIProviderKind
    {
        Unknown,
        Mock,
        Real
    }

    public enum AIProviderConnectionState
    {
        NotConfigured,
        Unverified,
        Verifying,
        Verified,
        Failed
    }

    [Serializable]
    public sealed class AIProposalAnalysisRequest
    {
        public AIProposalAnalysisRequest(
            string prompt,
            GameplayProposal currentProposal,
            string refinementIntent,
            string preferences,
            string vocabulary,
            string outputSchema)
        {
            UserPrompt = prompt ?? string.Empty;
            CurrentProposal = currentProposal;
            RefinementIntent = refinementIntent ?? string.Empty;
            CreatorPreferences = preferences ?? string.Empty;
            GameplayVocabulary = vocabulary ?? string.Empty;
            OutputSchema = outputSchema ?? string.Empty;
        }

        public string UserPrompt { get; }
        public GameplayProposal CurrentProposal { get; }
        public string RefinementIntent { get; }
        public string CreatorPreferences { get; }
        public string GameplayVocabulary { get; }
        public string OutputSchema { get; }
    }

    [Serializable]
    public sealed class AIChallengeGenerationRequest
    {
        public AIChallengeGenerationRequest(
            string prompt,
            GameplayProposal confirmedProposal,
            string preferences,
            string vocabulary,
            string outputSchema)
        {
            UserPrompt = prompt ?? string.Empty;
            ConfirmedProposal = confirmedProposal;
            CreatorPreferences = preferences ?? string.Empty;
            GameplayVocabulary = vocabulary ?? string.Empty;
            OutputSchema = outputSchema ?? string.Empty;
        }

        public string UserPrompt { get; }
        public GameplayProposal ConfirmedProposal { get; }
        public string CreatorPreferences { get; }
        public string GameplayVocabulary { get; }
        public string OutputSchema { get; }
    }

    [Serializable]
    public sealed class AIModificationProposalRequest
    {
        public AIModificationProposalRequest(
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

    [Serializable]
    public sealed class AIChallengeRepairRequest
    {
        public AIChallengeRepairRequest(
            string originalPrompt,
            GameplayProposal confirmedProposal,
            ChallengeSpec invalidChallenge,
            ChallengeSpec previousChallenge,
            string validationContext,
            string balanceContext,
            string vocabulary,
            string outputSchema)
        {
            OriginalPrompt = originalPrompt ?? string.Empty;
            ConfirmedProposal = confirmedProposal;
            InvalidChallenge = invalidChallenge;
            PreviousChallenge = previousChallenge;
            ValidationContext = validationContext ?? string.Empty;
            BalanceContext = balanceContext ?? string.Empty;
            GameplayVocabulary = vocabulary ?? string.Empty;
            OutputSchema = outputSchema ?? string.Empty;
        }

        public string OriginalPrompt { get; }
        public GameplayProposal ConfirmedProposal { get; }
        public ChallengeSpec InvalidChallenge { get; }
        public ChallengeSpec PreviousChallenge { get; }
        public string ValidationContext { get; }
        public string BalanceContext { get; }
        public string GameplayVocabulary { get; }
        public string OutputSchema { get; }
    }

    [Serializable]
    public sealed class AIChallengePatchRepairRequest
    {
        public AIChallengePatchRepairRequest(
            string originalPrompt,
            ChallengeSpec currentChallenge,
            ChallengePatch rejectedPatch,
            string patchError,
            string preferences,
            string vocabulary,
            string outputSchema)
        {
            OriginalPrompt = originalPrompt ?? string.Empty;
            CurrentChallenge = currentChallenge;
            RejectedPatch = rejectedPatch;
            PatchError = patchError ?? string.Empty;
            CreatorPreferences = preferences ?? string.Empty;
            GameplayVocabulary = vocabulary ?? string.Empty;
            OutputSchema = outputSchema ?? string.Empty;
        }

        public string OriginalPrompt { get; }
        public ChallengeSpec CurrentChallenge { get; }
        public ChallengePatch RejectedPatch { get; }
        public string PatchError { get; }
        public string CreatorPreferences { get; }
        public string GameplayVocabulary { get; }
        public string OutputSchema { get; }
    }

    [Serializable]
    public sealed class AIImprovementAnalysisRequest
    {
        public AIImprovementAnalysisRequest(
            ChallengeSpec currentChallenge,
            string preferences,
            string vocabulary,
            string outputSchema)
        {
            CurrentChallenge = currentChallenge;
            CreatorPreferences = preferences ?? string.Empty;
            GameplayVocabulary = vocabulary ?? string.Empty;
            OutputSchema = outputSchema ?? string.Empty;
        }

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
