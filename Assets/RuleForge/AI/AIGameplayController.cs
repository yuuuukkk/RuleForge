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

        public bool GenerateChallenge(
            string prompt,
            Action<AIChallengePreview> onComplete)
        {
            if (!TryBeginRequest(prompt, onComplete, out IAIGameplayService provider))
            {
                return false;
            }

            StartCoroutine(GenerateRoutine(provider, prompt, onComplete));
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
            Action<AIChallengePreview> onComplete)
        {
            Stopwatch timer = Stopwatch.StartNew();
            AIChallengeGenerationRequest request =
                new AIChallengeGenerationRequest(
                    prompt,
                    BuildGenerationPreferences(),
                    BuildVocabulary(),
                    GameplayVocabulary.BuildChallengeSchema(EffectCatalog));
            AIGameplayResult<ChallengeSpec> result = null;
            yield return provider.GenerateChallenge(
                request,
                completed => result = completed);
            timer.Stop();
            isBusy = false;

            if (result == null || !result.Success)
            {
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
                    BuildPreferences(),
                    BuildVocabulary(),
                    GameplayVocabulary.BuildPatchSchema(EffectCatalog));
            AIGameplayResult<ChallengePatch> result = null;
            yield return provider.ModifyChallenge(
                request,
                completed => result = completed);
            timer.Stop();
            isBusy = false;

            if (result == null || !result.Success)
            {
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

            if (!ChallengePatchApplier.TryApply(
                    currentChallenge,
                    result.Value,
                    out ChallengeSpec candidate,
                    out string patchError))
            {
                AIChallengePreview failed = AIChallengePreview.Failed(
                    "AI patch rejected: " + patchError);
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

            AIChallengePreview preview = CreatePreview(
                candidate,
                "AI MODIFIED");
            PublishTelemetry(
                "Modify",
                provider,
                prompt,
                timer.Elapsed.TotalMilliseconds,
                true,
                preview);
            onComplete?.Invoke(preview);
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
            provider = null;
            if (isBusy)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "An AI request is already running."));
                return false;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "Enter a natural-language request first."));
                return false;
            }

            if (ruleEngine == null)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "RuleEngine is not configured."));
                return false;
            }

            provider = GetActiveProvider();
            if (provider == null)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    "No IAIGameplayService provider is configured."));
                return false;
            }

            if (!provider.IsConfigured)
            {
                onComplete?.Invoke(AIChallengePreview.Failed(
                    provider.ProviderName + " is not configured."));
                return false;
            }

            isBusy = true;
            return true;
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
            ChallengeSpec active = ruleEngine.ActiveChallenge;
            return
                $"Reward strength multiplier: {ruleEngine.RewardMultiplier:0.##}. " +
                $"Penalty strength multiplier: {ruleEngine.PenaltyMultiplier:0.##}. " +
                $"Current goal: {active?.Goal ?? "KillCount"} " +
                $"target {(active?.GoalTarget ?? 10f):0.##}. " +
                $"Current weapon: {active?.Weapon ?? "Assault Rifle"}.";
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
            string error)
        {
            Success = success;
            Challenge = challenge;
            Validation = validation;
            Balance = balance;
            Source = source ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }
        public ChallengeSpec Challenge { get; }
        public ValidationResult Validation { get; }
        public BalanceEvaluation Balance { get; }
        public string Source { get; }
        public string Error { get; }

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
