using System;
using System.Collections;
using System.Diagnostics;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Rules;
using RuleForge.Validation;
using UnityEngine;

namespace RuleForge.AI
{
    [DisallowMultipleComponent]
    public sealed class AIGameplayController : MonoBehaviour
    {
        [SerializeField] private RuleEngine ruleEngine;
        [SerializeField] private MonoBehaviour[] providerComponents =
            Array.Empty<MonoBehaviour>();
        [SerializeField, Min(0)] private int activeProviderIndex;

        private bool isBusy;
        private string previousGenerationPrompt = string.Empty;
        private string previousGenerationStructure = string.Empty;

        public event Action<AIGenerationTelemetry> RequestCompleted;

        public bool IsBusy => isBusy;
        public EffectCatalog EffectCatalog =>
            ruleEngine != null ? ruleEngine.EffectCatalog : null;
        public string ActiveProviderName
        {
            get
            {
                IAIGameplayService provider = GetActiveProvider();
                return provider != null ? provider.ProviderName : "Not configured";
            }
        }

        public AIProviderKind ActiveProviderKind
        {
            get
            {
                IAIGameplayService provider = GetActiveProvider();
                return provider != null ? provider.ProviderKind : AIProviderKind.Unknown;
            }
        }

        public bool ActiveProviderIsConfigured
        {
            get
            {
                IAIGameplayService provider = GetActiveProvider();
                return provider != null && provider.IsConfigured;
            }
        }

        public void RefreshProviderSelection()
        {
            if (!isBusy)
            {
                SelectPreferredProvider();
            }
        }

        private void OnEnable()
        {
            SelectPreferredProvider();
        }

        public void Configure(
            RuleEngine engine,
            params MonoBehaviour[] providers)
        {
            ruleEngine = engine;
            providerComponents = providers ?? Array.Empty<MonoBehaviour>();
            activeProviderIndex = Mathf.Clamp(
                activeProviderIndex,
                0,
                Mathf.Max(0, providerComponents.Length - 1));
            SelectPreferredProvider();
        }

        public void SelectNextProvider()
        {
            if (isBusy || providerComponents == null ||
                providerComponents.Length < 2)
            {
                return;
            }

            for (int offset = 1; offset <= providerComponents.Length; offset++)
            {
                int candidate =
                    (activeProviderIndex + offset) % providerComponents.Length;
                if (providerComponents[candidate] is IAIGameplayService)
                {
                    activeProviderIndex = candidate;
                    return;
                }
            }
        }

        private void SelectPreferredProvider()
        {
            if (providerComponents == null || providerComponents.Length == 0)
            {
                activeProviderIndex = 0;
                return;
            }

            for (int index = 0; index < providerComponents.Length; index++)
            {
                if (providerComponents[index] is IAIGameplayService provider &&
                    provider.ProviderKind == AIProviderKind.Real &&
                    provider.IsConfigured)
                {
                    activeProviderIndex = index;
                    return;
                }
            }

            activeProviderIndex = Mathf.Clamp(
                activeProviderIndex,
                0,
                providerComponents.Length - 1);
            if (providerComponents[activeProviderIndex] is IAIGameplayService current &&
                current.IsConfigured)
            {
                return;
            }

            for (int index = 0; index < providerComponents.Length; index++)
            {
                if (providerComponents[index] is IAIGameplayService provider &&
                    provider.IsConfigured)
                {
                    activeProviderIndex = index;
                    return;
                }
            }
        }

        public bool AnalyzeGameplay(
            string prompt,
            GameplayProposal currentProposal,
            string refinementIntent,
            Action<AIGameplayResult<GameplayProposal>> onComplete)
        {
            if (!TryBeginRequest(prompt, error => onComplete?.Invoke(
                    AIGameplayResult<GameplayProposal>.Failed(error)),
                    out IAIGameplayService provider))
            {
                return false;
            }

            StartCoroutine(AnalyzeGameplayRoutine(
                provider, prompt, currentProposal, refinementIntent, onComplete));
            return true;
        }

        public bool GenerateChallenge(
            string prompt,
            GameplayProposal confirmedProposal,
            Action<AIChallengePreview> onComplete)
        {
            if (confirmedProposal == null || !confirmedProposal.CanGenerate)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "Confirm a complete Gameplay Proposal before generation."));
                return false;
            }

            if (!TryBeginRequest(prompt, onComplete, out IAIGameplayService provider))
            {
                return false;
            }

            StartCoroutine(GenerateRoutine(
                provider, prompt, confirmedProposal, onComplete));
            return true;
        }

        public bool AnalyzeModification(
            string prompt,
            ChallengeSpec currentChallenge,
            Action<AIGameplayResult<GameplayModificationProposal>> onComplete)
        {
            if (currentChallenge == null)
            {
                onComplete?.Invoke(
                    AIGameplayResult<GameplayModificationProposal>.Failed(
                        "A current Creator challenge is required."));
                return false;
            }

            if (!TryBeginRequest(prompt, error => onComplete?.Invoke(
                    AIGameplayResult<GameplayModificationProposal>.Failed(error)),
                    out IAIGameplayService provider))
            {
                return false;
            }

            StartCoroutine(AnalyzeModificationRoutine(
                provider, prompt, currentChallenge, onComplete));
            return true;
        }

        public bool AnalyzeImprovements(
            ChallengeSpec currentChallenge,
            Action<AIGameplayResult<GameplayImprovementSet>> onComplete)
        {
            if (currentChallenge == null)
            {
                onComplete?.Invoke(AIGameplayResult<GameplayImprovementSet>.Failed(
                    "A current Creator challenge is required."));
                return false;
            }

            const string requestLabel = "Improve the current challenge";
            if (!TryBeginRequest(requestLabel, error => onComplete?.Invoke(
                    AIGameplayResult<GameplayImprovementSet>.Failed(error)),
                    out IAIGameplayService provider))
            {
                return false;
            }

            StartCoroutine(AnalyzeImprovementsRoutine(
                provider, currentChallenge, onComplete));
            return true;
        }

        public bool ModifyChallenge(
            string prompt,
            ChallengeSpec currentChallenge,
            Action<AIChallengePreview> onComplete)
        {
            if (currentChallenge == null)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "A current Creator challenge is required."));
                return false;
            }

            if (!TryBeginRequest(prompt, onComplete, out IAIGameplayService provider))
            {
                return false;
            }

            StartCoroutine(ModifyRoutine(
                provider,
                prompt,
                currentChallenge,
                onComplete));
            return true;
        }

        private IEnumerator GenerateRoutine(
            IAIGameplayService provider,
            string prompt,
            GameplayProposal confirmedProposal,
            Action<AIChallengePreview> onComplete)
        {
            Stopwatch timer = Stopwatch.StartNew();
            AIChallengeGenerationRequest request =
                new AIChallengeGenerationRequest(
                    prompt,
                    confirmedProposal,
                    BuildGenerationPreferences(),
                    BuildVocabulary(),
                    GameplayVocabulary.BuildChallengeSchema(EffectCatalog));
            AIGameplayResult<ChallengeSpec> result = null;
            yield return provider.GenerateChallenge(
                request,
                completed => result = completed);
            if (result == null || !result.Success)
            {
                timer.Stop();
                isBusy = false;
                AIChallengePreview failed = AIChallengePreview.Failed(
                    result != null
                        ? result.Error
                        : "AI provider completed without a result.");
                PublishTelemetry(
                    "Generate",
                    provider,
                    prompt,
                    timer.Elapsed.TotalMilliseconds,
                    false,
                    failed);
                onComplete?.Invoke(failed);
                yield break;
            }

            AIChallengePreview preview = CreatePreview(
                result.Value,
                "AI CREATED");
            if (preview.Validation != null && !preview.Validation.IsValid)
            {
                yield return RepairRoutine(
                    provider,
                    prompt,
                    confirmedProposal,
                    result.Value,
                    null,
                    preview,
                    repaired => preview = repaired);
            }
            timer.Stop();
            isBusy = false;
            if (preview.Success && preview.Challenge != null)
            {
                previousGenerationPrompt = prompt;
                previousGenerationStructure =
                    GameplayVocabulary.BuildStructureSummary(preview.Challenge);
            }
            PublishTelemetry(
                "Generate",
                provider,
                prompt,
                timer.Elapsed.TotalMilliseconds,
                true,
                preview);
            onComplete?.Invoke(preview);
        }

        private IEnumerator AnalyzeGameplayRoutine(
            IAIGameplayService provider,
            string prompt,
            GameplayProposal currentProposal,
            string refinementIntent,
            Action<AIGameplayResult<GameplayProposal>> onComplete)
        {
            AIProposalAnalysisRequest request = new AIProposalAnalysisRequest(
                prompt,
                currentProposal,
                refinementIntent,
                BuildGenerationPreferences(),
                BuildVocabulary(),
                GameplayVocabulary.BuildProposalSchema());
            AIGameplayResult<GameplayProposal> result = null;
            yield return provider.AnalyzeGameplay(request, value => result = value);
            isBusy = false;
            onComplete?.Invoke(result ?? AIGameplayResult<GameplayProposal>.Failed(
                "AI provider completed without a proposal."));
        }

        private IEnumerator AnalyzeModificationRoutine(
            IAIGameplayService provider,
            string prompt,
            ChallengeSpec currentChallenge,
            Action<AIGameplayResult<GameplayModificationProposal>> onComplete)
        {
            AIModificationProposalRequest request =
                new AIModificationProposalRequest(
                    prompt,
                    currentChallenge,
                    BuildPreferences(currentChallenge),
                    BuildVocabulary(),
                    GameplayVocabulary.BuildModificationProposalSchema());
            AIGameplayResult<GameplayModificationProposal> result = null;
            yield return provider.AnalyzeModification(
                request, value => result = value);
            isBusy = false;
            onComplete?.Invoke(
                result ?? AIGameplayResult<GameplayModificationProposal>.Failed(
                    "AI provider completed without a modification proposal."));
        }

        private IEnumerator AnalyzeImprovementsRoutine(
            IAIGameplayService provider,
            ChallengeSpec currentChallenge,
            Action<AIGameplayResult<GameplayImprovementSet>> onComplete)
        {
            AIImprovementAnalysisRequest request =
                new AIImprovementAnalysisRequest(
                    currentChallenge,
                    BuildPreferences(currentChallenge),
                    BuildVocabulary(),
                    GameplayVocabulary.BuildImprovementSchema());
            AIGameplayResult<GameplayImprovementSet> result = null;
            yield return provider.AnalyzeImprovements(
                request, value => result = value);
            isBusy = false;
            onComplete?.Invoke(
                result ?? AIGameplayResult<GameplayImprovementSet>.Failed(
                    "AI provider completed without improvement suggestions."));
        }

        private IEnumerator ModifyRoutine(
            IAIGameplayService provider,
            string prompt,
            ChallengeSpec currentChallenge,
            Action<AIChallengePreview> onComplete)
        {
            Stopwatch timer = Stopwatch.StartNew();
            AIChallengeModificationRequest request =
                new AIChallengeModificationRequest(
                    prompt,
                    currentChallenge,
                    BuildPreferences(currentChallenge),
                    BuildVocabulary(),
                    GameplayVocabulary.BuildPatchSchema(EffectCatalog));
            AIGameplayResult<ChallengePatch> result = null;
            yield return provider.ModifyChallenge(
                request,
                completed => result = completed);
            if (result == null || !result.Success)
            {
                timer.Stop();
                isBusy = false;
                AIChallengePreview failed = AIChallengePreview.Failed(
                    result != null
                        ? result.Error
                        : "AI provider completed without a result.");
                PublishTelemetry(
                    "Modify",
                    provider,
                    prompt,
                    timer.Elapsed.TotalMilliseconds,
                    false,
                    failed);
                onComplete?.Invoke(failed);
                yield break;
            }

            ChallengePatch appliedPatch = result.Value;
            if (!ChallengePatchApplier.TryApply(
                    currentChallenge,
                    appliedPatch,
                    out ChallengeSpec candidate,
                    out string patchError))
            {
                AIChallengePatchRepairRequest repairRequest =
                    new AIChallengePatchRepairRequest(
                        prompt,
                        currentChallenge,
                        appliedPatch,
                        patchError,
                        BuildPreferences(currentChallenge),
                        BuildVocabulary(),
                        GameplayVocabulary.BuildPatchSchema(EffectCatalog));
                AIGameplayResult<ChallengePatch> repairedPatch = null;
                yield return provider.RepairPatch(
                    repairRequest,
                    completed => repairedPatch = completed);
                string repairedPatchError =
                    "AI patch repair returned no usable patch.";
                bool repairedPatchApplied = repairedPatch != null &&
                    repairedPatch.Success &&
                    ChallengePatchApplier.TryApply(
                        currentChallenge,
                        repairedPatch.Value,
                        out candidate,
                        out repairedPatchError);
                if (!repairedPatchApplied)
                {
                    AIChallengePreview failed = AIChallengePreview.Failed(
                        "AI patch could not be applied. Original error: " +
                        patchError + " Repair result: " +
                        (repairedPatch != null && !repairedPatch.Success
                            ? repairedPatch.Error
                            : repairedPatchError));
                    timer.Stop();
                    isBusy = false;
                    PublishTelemetry(
                        "Modify",
                        provider,
                        prompt,
                        timer.Elapsed.TotalMilliseconds,
                        true,
                        failed);
                    onComplete?.Invoke(failed);
                    yield break;
                }
            }

            AIChallengePreview preview = CreatePreview(
                candidate,
                "AI MODIFIED");
            if (preview.Validation != null && !preview.Validation.IsValid)
            {
                yield return RepairRoutine(
                    provider,
                    prompt,
                    null,
                    candidate,
                    currentChallenge,
                    preview,
                    repaired => preview = repaired);
            }
            timer.Stop();
            isBusy = false;
            PublishTelemetry(
                "Modify",
                provider,
                prompt,
                timer.Elapsed.TotalMilliseconds,
                true,
                preview);
            onComplete?.Invoke(preview);
        }

        private IEnumerator RepairRoutine(
            IAIGameplayService provider,
            string prompt,
            GameplayProposal confirmedProposal,
            ChallengeSpec invalidChallenge,
            ChallengeSpec previousChallenge,
            AIChallengePreview invalidPreview,
            Action<AIChallengePreview> onComplete)
        {
            string validationContext = BuildValidationContext(
                invalidPreview.Validation);
            AIChallengeRepairRequest request = new AIChallengeRepairRequest(
                prompt,
                confirmedProposal,
                invalidChallenge,
                previousChallenge,
                validationContext,
                BuildBalanceContext(invalidPreview.Balance),
                BuildVocabulary(),
                GameplayVocabulary.BuildRepairSchema(EffectCatalog));
            AIGameplayResult<GameplayRepairResult> repair = null;
            yield return provider.RepairChallenge(request, value => repair = value);
            if (repair == null || !repair.Success ||
                repair.Value?.RepairedChallenge == null)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "Validator rejected the AI result:\n" +
                    validationContext +
                    "\nAI repair failed: " +
                    (repair != null ? repair.Error : "no repair result")));
                yield break;
            }

            AIChallengePreview repairedPreview = CreatePreview(
                repair.Value.RepairedChallenge,
                "AI REPAIRED");
            if (repairedPreview.Validation == null ||
                !repairedPreview.Validation.IsValid)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "AI repair was still rejected by Validator. " +
                    BuildValidationContext(repairedPreview.Validation)));
                yield break;
            }

            if (previousChallenge != null &&
                !ChallengeRepairScope.ContainsOnlyInitiallyChangedFields(
                    previousChallenge,
                    invalidChallenge,
                    repairedPreview.Challenge,
                    out string[] extraPaths))
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "AI repair changed fields outside the original modification: " +
                    string.Join(", ", extraPaths)));
                yield break;
            }

            onComplete?.Invoke(AIChallengePreview.Repaired(
                repairedPreview.Challenge,
                repairedPreview.Validation,
                repairedPreview.Balance,
                repair.Value.Summary,
                repair.Value.Changes,
                repair.Value.DesignReasoningSummary,
                invalidPreview.Validation?.Errors));
        }

        private void PublishTelemetry(
            string requestType,
            IAIGameplayService provider,
            string prompt,
            double generationTimeMs,
            bool parseSuccess,
            AIChallengePreview preview)
        {
            RequestCompleted?.Invoke(new AIGenerationTelemetry(
                requestType,
                provider != null ? provider.ProviderName : string.Empty,
                provider != null && provider.IsBenchmarkEligible,
                prompt,
                generationTimeMs,
                parseSuccess,
                preview != null ? preview.Challenge : null,
                preview != null ? preview.Validation : null,
                preview != null ? preview.Balance : null,
                preview != null ? preview.Error : string.Empty));
        }

        private bool TryBeginRequest(
            string prompt,
            Action<AIChallengePreview> onComplete,
            out IAIGameplayService provider)
        {
            return TryBeginRequest(
                prompt,
                error => onComplete?.Invoke(AIChallengePreview.Failed(error)),
                out provider);
        }

        private bool TryBeginRequest(
            string prompt,
            Action<string> onError,
            out IAIGameplayService provider)
        {
            provider = null;
            if (isBusy)
            {
                onError?.Invoke("An AI request is already running.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                onError?.Invoke("Enter a natural-language request first.");
                return false;
            }

            if (ruleEngine == null)
            {
                onError?.Invoke("RuleEngine is not configured.");
                return false;
            }

            provider = GetActiveProvider();
            if (provider == null)
            {
                onError?.Invoke("No IAIGameplayService provider is configured.");
                return false;
            }

            if (!provider.IsConfigured)
            {
                onError?.Invoke(provider.ProviderName + " is not configured.");
                return false;
            }

            isBusy = true;
            return true;
        }

        private static string BuildValidationContext(ValidationResult validation)
        {
            if (validation == null)
            {
                return "Validator returned no result.";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int index = 0; index < validation.Errors.Count; index++)
            {
                builder.Append("ERROR: ").AppendLine(validation.Errors[index]);
            }
            for (int index = 0; index < validation.Warnings.Count; index++)
            {
                builder.Append("WARNING: ").AppendLine(validation.Warnings[index]);
            }

            return builder.Length > 0
                ? builder.ToString()
                : validation.BuildSummary();
        }

        private static string BuildBalanceContext(BalanceEvaluation balance)
        {
            if (balance == null)
            {
                return "BalanceEvaluator returned no result.";
            }

            return $"Result={balance.Result}; Reward={balance.RewardScore:0.###}; " +
                   $"Risk={balance.PenaltyScore:0.###}; " +
                   $"Ratio={balance.RewardPenaltyRatio:0.###}; " +
                   $"Difficulty={balance.Difficulty}; " +
                   $"Growth={balance.Growth} ({balance.GrowthScore:0.###}).";
        }

        private AIChallengePreview CreatePreview(
            ChallengeSpec challenge,
            string source)
        {
            ValidationResult validation = ruleEngine.ValidateChallenge(challenge);
            BalanceEvaluation balance = ruleEngine.LastBalanceEvaluation;
            return AIChallengePreview.Succeeded(
                challenge,
                validation,
                balance,
                source);
        }

        private string BuildPreferences()
        {
            return BuildPreferences(ruleEngine.ActiveChallenge);
        }

        private string BuildPreferences(ChallengeSpec challenge)
        {
            return
                $"Reward strength multiplier: {ruleEngine.RewardMultiplier:0.##}. " +
                $"Penalty strength multiplier: {ruleEngine.PenaltyMultiplier:0.##}. " +
                $"Current goal: {challenge?.Goal ?? "KillCount"} " +
                $"target {(challenge?.GoalTarget ?? 10f):0.##}. " +
                $"Current weapon: {challenge?.Weapon ?? "Assault Rifle"}.";
        }

        private string BuildGenerationPreferences()
        {
            string preferences = BuildPreferences();
            if (string.IsNullOrWhiteSpace(previousGenerationPrompt) ||
                string.IsNullOrWhiteSpace(previousGenerationStructure))
            {
                return preferences;
            }

            return preferences +
                   " Previous user prompt: " + previousGenerationPrompt +
                   ". Previous structural result: " +
                   previousGenerationStructure +
                   ". Compare the new request with this context. If its gameplay " +
                   "intent differs, choose a materially different valid structure; " +
                   "do not vary names alone.";
        }

        private string BuildVocabulary()
        {
            return GameplayVocabulary.BuildDescription(
                ruleEngine.EffectCatalog,
                ruleEngine.BalanceConfig);
        }

        private IAIGameplayService GetActiveProvider()
        {
            if (providerComponents == null || providerComponents.Length == 0)
            {
                return null;
            }

            activeProviderIndex = Mathf.Clamp(
                activeProviderIndex,
                0,
                providerComponents.Length - 1);
            return providerComponents[activeProviderIndex] as IAIGameplayService;
        }
    }

    public sealed class AIChallengePreview
    {
        private AIChallengePreview(
            bool success,
            ChallengeSpec challenge,
            ValidationResult validation,
            BalanceEvaluation balance,
            string source,
            string error,
            bool requiresRepairConfirmation = false,
            string repairSummary = "",
            string[] repairChanges = null,
            string repairReasoning = "",
            string[] repairSourceErrors = null)
        {
            Success = success;
            Challenge = challenge;
            Validation = validation;
            Balance = balance;
            Source = source ?? string.Empty;
            Error = error ?? string.Empty;
            RequiresRepairConfirmation = requiresRepairConfirmation;
            RepairSummary = repairSummary ?? string.Empty;
            RepairChanges = repairChanges ?? Array.Empty<string>();
            RepairReasoning = repairReasoning ?? string.Empty;
            RepairSourceErrors = repairSourceErrors ?? Array.Empty<string>();
        }

        public bool Success { get; }
        public ChallengeSpec Challenge { get; }
        public ValidationResult Validation { get; }
        public BalanceEvaluation Balance { get; }
        public string Source { get; }
        public string Error { get; }
        public bool RequiresRepairConfirmation { get; }
        public string RepairSummary { get; }
        public string[] RepairChanges { get; }
        public string RepairReasoning { get; }
        public string[] RepairSourceErrors { get; }

        public static AIChallengePreview Succeeded(
            ChallengeSpec challenge,
            ValidationResult validation,
            BalanceEvaluation balance,
            string source)
        {
            return new AIChallengePreview(
                true,
                challenge,
                validation,
                balance,
                source,
                string.Empty);
        }

        public static AIChallengePreview Repaired(
            ChallengeSpec challenge,
            ValidationResult validation,
            BalanceEvaluation balance,
            string summary,
            string[] changes,
            string reasoning,
            System.Collections.Generic.IReadOnlyList<string> sourceErrors)
        {
            string[] errors = sourceErrors != null
                ? Copy(sourceErrors)
                : Array.Empty<string>();
            return new AIChallengePreview(
                true,
                challenge,
                validation,
                balance,
                "AI REPAIRED",
                string.Empty,
                true,
                summary,
                changes,
                reasoning,
                errors);
        }

        private static string[] Copy(
            System.Collections.Generic.IReadOnlyList<string> values)
        {
            string[] copy = new string[values.Count];
            for (int index = 0; index < values.Count; index++)
            {
                copy[index] = values[index];
            }
            return copy;
        }

        public static AIChallengePreview Failed(string error)
        {
            return new AIChallengePreview(
                false,
                null,
                null,
                null,
                string.Empty,
                error);
        }
    }
}
