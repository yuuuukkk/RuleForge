using System;
using System.Collections.Generic;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Runtime.Services;
using RuleForge.Runtime.Stats;
using RuleForge.Validation;
using UnityEngine;

namespace RuleForge.Rules
{
    [DisallowMultipleComponent]
    public sealed class RuleEngine : MonoBehaviour
    {
        [Header("Challenge Data")]
        [SerializeField] private TextAsset challengeJson;

        [Header("Trusted Tuning Config")]
        [SerializeField] private GameplayBalanceConfig balanceConfig;
        [SerializeField] private EffectCatalog effectCatalog;
        [SerializeField] private float rewardMultiplier = 1f;
        [SerializeField] private float penaltyMultiplier = 1f;

        [Header("Runtime Services")]
        [SerializeField] private PlayerRuntimeService playerService;
        [SerializeField] private EnemyRuntimeService enemyService;
        [SerializeField] private WeaponRuntimeService weaponService;
        [SerializeField] private SpawnRuntimeService spawnService;
        [SerializeField] private TimeRuntimeService timeService;

        [Header("Runtime Debug")]
        [SerializeField] private ChallengeSpec activeChallenge;
        [SerializeField] private List<RuleExecutionDebugState> executionStates =
            new List<RuleExecutionDebugState>();

        [Header("Validation Debug")]
        [SerializeField] private ValidationResult lastValidationResult =
            new ValidationResult();
        [SerializeField] private BalanceEvaluation lastBalanceEvaluation =
            new BalanceEvaluation(0f, 0f, 1f, BalanceResult.NoSignal);

        private readonly Dictionary<string, int> appliedStackCounts =
            new Dictionary<string, int>();
        private readonly Dictionary<string, int> effectExecutionSequences =
            new Dictionary<string, int>();
        private readonly List<TimedModifierState> timedModifiers =
            new List<TimedModifierState>();

        private ConditionEvaluator conditionEvaluator;
        private ScalingEvaluator scalingEvaluator;
        private EffectExecutor effectExecutor;
        private ChallengeValidator challengeValidator;
        private BalanceEvaluator balanceEvaluator;

        public ChallengeSpec ActiveChallenge => activeChallenge;
        public TextAsset ChallengeJson => challengeJson;
        public float RewardMultiplier => rewardMultiplier;
        public float PenaltyMultiplier => penaltyMultiplier;
        public PlayerRuntimeService PlayerService => playerService;
        public EnemyRuntimeService EnemyService => enemyService;
        public WeaponRuntimeService WeaponService => weaponService;
        public GameplayBalanceConfig BalanceConfig => balanceConfig;
        public EffectCatalog EffectCatalog => effectCatalog;
        public ValidationResult LastValidationResult => lastValidationResult;
        public BalanceEvaluation LastBalanceEvaluation => lastBalanceEvaluation;

        public event Action<ChallengeSpec> ChallengeRestarted;
        public event Action<RuleTriggerFeedback> RuleTriggered;

        private void Awake()
        {
            ResolveServices();
            InitializeTuningFromConfig();
            InitializeValidationServices();
            conditionEvaluator = new ConditionEvaluator();
            scalingEvaluator = new ScalingEvaluator(playerService);
            effectExecutor = new EffectExecutor(
                playerService,
                enemyService,
                weaponService,
                spawnService);

            if (challengeJson != null)
            {
                LoadChallengeJson(challengeJson.text);
            }
        }

        private void OnEnable()
        {
            GameplayEventBus.EventPublished += HandleGameplayEvent;
        }

        private void Start()
        {
            ChallengeRestarted?.Invoke(activeChallenge);
            GameplayEventBus.Publish(new GameplayEvent(GameplayEventType.GameStarted));
        }

        private void Update()
        {
            ExpireTimedModifiers();
        }

        private void OnDisable()
        {
            GameplayEventBus.EventPublished -= HandleGameplayEvent;
        }

        public void Configure(
            TextAsset json,
            PlayerRuntimeService player,
            EnemyRuntimeService enemy,
            WeaponRuntimeService weapon,
            SpawnRuntimeService spawn,
            TimeRuntimeService time)
        {
            challengeJson = json;
            playerService = player;
            enemyService = enemy;
            weaponService = weapon;
            spawnService = spawn;
            timeService = time;
        }

        public void ConfigureTuning(
            GameplayBalanceConfig balance,
            EffectCatalog catalog)
        {
            balanceConfig = balance;
            effectCatalog = catalog;
            InitializeValidationServices();
        }

        public void SetStrengthMultipliers(float reward, float penalty)
        {
            rewardMultiplier = ClampStrengthMultiplier(reward);
            penaltyMultiplier = ClampStrengthMultiplier(penalty);
            if (activeChallenge != null)
            {
                InitializeValidationServices();
                lastBalanceEvaluation = balanceEvaluator.Evaluate(
                    activeChallenge,
                    rewardMultiplier,
                    penaltyMultiplier);
            }
        }

        public bool TryGetEffectDefinition(
            string effectId,
            out EffectDefinition definition)
        {
            if (effectCatalog != null &&
                effectCatalog.TryGetDefinition(effectId, out definition))
            {
                return true;
            }

            definition = null;
            return false;
        }

        public bool LoadChallengeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("RuleEngine cannot load an empty ChallengeSpec JSON.", this);
                return false;
            }

            ChallengeSpec parsedChallenge;
            try
            {
                parsedChallenge = JsonUtility.FromJson<ChallengeSpec>(json);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError(
                    $"RuleEngine could not parse ChallengeSpec JSON: {exception.Message}",
                    this);
                return false;
            }

            if (parsedChallenge == null)
            {
                Debug.LogError("RuleEngine parsed a null ChallengeSpec.", this);
                return false;
            }

            if (TryApplyRuntimeChallenge(parsedChallenge))
            {
                return true;
            }

            LogValidationErrors();
            return false;
        }

        public ValidationResult ValidateChallenge(ChallengeSpec challenge)
        {
            return ValidateChallenge(
                challenge,
                rewardMultiplier,
                penaltyMultiplier);
        }

        public ValidationResult ValidateChallenge(
            ChallengeSpec challenge,
            float previewRewardMultiplier,
            float previewPenaltyMultiplier)
        {
            InitializeValidationServices();
            lastValidationResult = challengeValidator.Validate(challenge);
            lastBalanceEvaluation = balanceEvaluator.Evaluate(
                challenge,
                previewRewardMultiplier,
                previewPenaltyMultiplier);
            return lastValidationResult;
        }

        public bool TryApplyRuntimeChallenge(ChallengeSpec challenge)
        {
            return TryApplyRuntimeChallenge(
                challenge,
                rewardMultiplier,
                penaltyMultiplier);
        }

        public bool TryApplyRuntimeChallenge(
            ChallengeSpec challenge,
            float candidateRewardMultiplier,
            float candidatePenaltyMultiplier)
        {
            float appliedRewardMultiplier = ClampStrengthMultiplier(
                candidateRewardMultiplier);
            float appliedPenaltyMultiplier = ClampStrengthMultiplier(
                candidatePenaltyMultiplier);
            ValidationResult validation = ValidateChallenge(
                challenge,
                appliedRewardMultiplier,
                appliedPenaltyMultiplier);
            if (!validation.IsValid)
            {
                return false;
            }

            ResetChallengeRuntime();
            activeChallenge = challenge;
            rewardMultiplier = appliedRewardMultiplier;
            penaltyMultiplier = appliedPenaltyMultiplier;
            return true;
        }

        private float ClampStrengthMultiplier(float value)
        {
            float minimum = balanceConfig != null
                ? balanceConfig.MinimumStrengthMultiplier
                : 0f;
            float maximum = balanceConfig != null
                ? balanceConfig.MaximumStrengthMultiplier
                : float.MaxValue;
            return Mathf.Clamp(value, minimum, maximum);
        }

        public void ResetChallengeRuntime()
        {
            effectExecutor?.ClearRuleModifiers();
            appliedStackCounts.Clear();
            effectExecutionSequences.Clear();
            timedModifiers.Clear();
            executionStates.Clear();
        }

        public void RestartChallenge()
        {
            ResetChallengeRuntime();
            timeService?.RestartClock();
            ChallengeRestarted?.Invoke(activeChallenge);
            weaponService?.ResetForChallenge();
            spawnService?.ResetForChallenge();
            playerService?.ResetForChallenge();
            GameplayEventBus.Publish(new GameplayEvent(GameplayEventType.GameStarted));
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (activeChallenge == null || effectExecutor == null)
            {
                return;
            }

            GameplayRule[] rules = activeChallenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null ||
                    !TriggerMatches(rule.Trigger, gameplayEvent.Type) ||
                    !conditionEvaluator.EvaluateAll(rule.Conditions, gameplayEvent))
                {
                    continue;
                }

                ExecuteEffects(rule, ruleIndex, gameplayEvent);
            }
        }

        private void ExecuteEffects(
            GameplayRule rule,
            int ruleIndex,
            GameplayEvent gameplayEvent)
        {
            List<RuleEffectFeedback> appliedFeedback =
                new List<RuleEffectFeedback>();
            RuleEffect[] effects = rule.Effects;
            for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
            {
                RuleEffect effect = effects[effectIndex];
                if (effect == null)
                {
                    continue;
                }

                string stateKey = BuildStateKey(rule, ruleIndex, effectIndex);
                if (effectExecutor.IsInstantAction(effect))
                {
                    if (ExecuteInstantAction(
                            rule,
                            effect,
                            stateKey,
                            out float appliedValue,
                            out int appliedStacks))
                    {
                        appliedFeedback.Add(CreateFeedback(
                            effect,
                            appliedValue,
                            appliedStacks,
                            0));
                    }

                    continue;
                }

                if (effect.Scaling != null &&
                    !string.IsNullOrWhiteSpace(effect.Scaling.Source))
                {
                    if (ExecuteScaledEffect(
                        rule,
                        effect,
                        stateKey,
                        gameplayEvent,
                        out float appliedValue))
                    {
                        appliedFeedback.Add(CreateFeedback(
                            effect,
                            appliedValue,
                            1,
                            1));
                    }

                    continue;
                }

                if (ExecuteStackedEffect(
                        rule,
                        effect,
                        stateKey,
                        out float stackedValue,
                        out int stack,
                        out int stackLimit))
                {
                    appliedFeedback.Add(CreateFeedback(
                        effect,
                        stackedValue,
                        stack,
                        stackLimit));
                }
            }

            if (appliedFeedback.Count > 0)
            {
                RuleTriggered?.Invoke(
                    new RuleTriggerFeedback(rule.Id, appliedFeedback));
            }
        }

        private bool ExecuteInstantAction(
            GameplayRule rule,
            RuleEffect effect,
            string stateKey,
            out float appliedValue,
            out int appliedStacks)
        {
            string source = BuildModifierSource(stateKey, 0);
            float tunedValue = GetTunedEffectValue(effect, effect.Value);
            appliedValue = tunedValue;
            appliedStacks = 0;
            if (!effectExecutor.TryApply(
                    effect,
                    source,
                    tunedValue,
                    false))
            {
                Debug.LogWarning(
                    $"Rule action '{effect.EffectId}' could not be applied.",
                    this);
                return false;
            }

            appliedStackCounts.TryGetValue(stateKey, out int executionCount);
            executionCount++;
            appliedStacks = executionCount;
            appliedStackCounts[stateKey] = executionCount;
            UpdateDebugState(
                stateKey,
                rule.Id,
                effect.EffectId,
                executionCount,
                0,
                tunedValue);
            return true;
        }

        private bool ExecuteScaledEffect(
            GameplayRule rule,
            RuleEffect effect,
            string stateKey,
            GameplayEvent gameplayEvent,
            out float appliedValue)
        {
            appliedValue = 0f;
            if (!scalingEvaluator.TryEvaluate(
                    effect.Scaling,
                    gameplayEvent,
                    out float scaledValue))
            {
                Debug.LogWarning(
                    $"Rule scaling for '{effect.EffectId}' could not be evaluated.",
                    this);
                return false;
            }

            string source = BuildScaledModifierSource(stateKey);
            float tunedValue = GetTunedEffectValue(effect, scaledValue);
            appliedValue = tunedValue;
            if (!effectExecutor.TryApply(
                    effect,
                    source,
                    tunedValue,
                    true))
            {
                Debug.LogWarning(
                    $"Scaled rule effect '{effect.EffectId}' could not be applied.",
                    this);
                return false;
            }

            TrackTimedModifier(effect, stateKey, source, true);

            appliedStackCounts[stateKey] = 1;
            UpdateDebugState(
                stateKey,
                rule.Id,
                effect.EffectId,
                1,
                1,
                tunedValue);
            return true;
        }

        private bool ExecuteStackedEffect(
            GameplayRule rule,
            RuleEffect effect,
            string stateKey,
            out float appliedValue,
            out int appliedStack,
            out int maximumStacks)
        {
            appliedValue = 0f;
            appliedStack = 0;
            maximumStacks = 0;
            if (!TryGetStackLimit(effect, out int stackLimit))
            {
                return false;
            }

            maximumStacks = stackLimit;

            appliedStackCounts.TryGetValue(stateKey, out int appliedStacks);
            if (appliedStacks >= stackLimit)
            {
                return false;
            }

            int nextStack = appliedStacks + 1;
            string source = BuildModifierSource(
                stateKey,
                NextExecutionSequence(stateKey));
            float tunedValue = GetTunedEffectValue(effect, effect.Value);
            appliedValue = tunedValue;
            if (!effectExecutor.TryApply(
                    effect,
                    source,
                    tunedValue,
                    false))
            {
                Debug.LogWarning(
                    $"Rule effect '{effect.EffectId}' could not be applied.",
                    this);
                return false;
            }

            TrackTimedModifier(effect, stateKey, source, false);

            appliedStackCounts[stateKey] = nextStack;
            appliedStack = nextStack;
            UpdateDebugState(
                stateKey,
                rule.Id,
                effect.EffectId,
                nextStack,
                stackLimit,
                tunedValue);
            return true;
        }

        private RuleEffectFeedback CreateFeedback(
            RuleEffect effect,
            float appliedValue,
            int appliedStacks,
            int maximumStacks)
        {
            string displayName = effect.EffectId;
            if (TryGetEffectDefinition(
                    effect.EffectId,
                    out EffectDefinition definition))
            {
                displayName = definition.DisplayName;
            }

            return new RuleEffectFeedback(
                effect.EffectId,
                displayName,
                effect.Kind,
                effect.Operation,
                appliedValue,
                effect.StringValue,
                appliedStacks,
                maximumStacks);
        }

        private void InitializeTuningFromConfig()
        {
            if (balanceConfig != null)
            {
                SetStrengthMultipliers(
                    balanceConfig.RewardMultiplier,
                    balanceConfig.PenaltyMultiplier);
                return;
            }

            rewardMultiplier = 1f;
            penaltyMultiplier = 1f;
        }

        private void InitializeValidationServices()
        {
            challengeValidator = new ChallengeValidator(
                balanceConfig,
                effectCatalog);
            balanceEvaluator = new BalanceEvaluator(
                balanceConfig,
                effectCatalog);
        }

        private void LogValidationErrors()
        {
            if (lastValidationResult == null)
            {
                return;
            }

            IReadOnlyList<string> errors = lastValidationResult.Errors;
            for (int index = 0; index < errors.Count; index++)
            {
                Debug.LogError(
                    $"Challenge validation: {errors[index]}",
                    this);
            }
        }

        private float GetTunedEffectValue(RuleEffect effect, float rawValue)
        {
            if (!TryGetEffectDefinition(effect.EffectId, out EffectDefinition definition))
            {
                return rawValue;
            }

            switch (definition.Polarity)
            {
                case EffectPolarity.Reward:
                    return rawValue * rewardMultiplier;
                case EffectPolarity.Penalty:
                    return rawValue * penaltyMultiplier;
                default:
                    return rawValue;
            }
        }

        private void ResolveServices()
        {
            playerService = playerService != null
                ? playerService
                : FindObjectOfType<PlayerRuntimeService>();
            enemyService = enemyService != null
                ? enemyService
                : FindObjectOfType<EnemyRuntimeService>();
            weaponService = weaponService != null
                ? weaponService
                : FindObjectOfType<WeaponRuntimeService>();
            spawnService = spawnService != null
                ? spawnService
                : FindObjectOfType<SpawnRuntimeService>();
            timeService = timeService != null
                ? timeService
                : FindObjectOfType<TimeRuntimeService>();
        }

        private static bool TriggerMatches(
            RuleTrigger trigger,
            GameplayEventType eventType)
        {
            return trigger != null &&
                   Enum.TryParse(trigger.Type, true, out GameplayEventType triggerType) &&
                   triggerType == eventType;
        }

        private static bool TryGetStackLimit(RuleEffect effect, out int stackLimit)
        {
            stackLimit = 0;
            if (!Enum.TryParse(effect.StackMode, true, out RuleStackMode stackMode))
            {
                return false;
            }

            switch (stackMode)
            {
                case RuleStackMode.None:
                    stackLimit = 1;
                    return true;
                case RuleStackMode.Stack:
                    stackLimit = effect.MaxStacks;
                    return stackLimit > 0;
                default:
                    return false;
            }
        }

        private static string BuildStateKey(
            GameplayRule rule,
            int ruleIndex,
            int effectIndex)
        {
            string ruleId = string.IsNullOrWhiteSpace(rule.Id)
                ? $"rule-{ruleIndex}"
                : rule.Id;
            return $"rule-{ruleIndex}:{ruleId}/effect-{effectIndex}";
        }

        private int NextExecutionSequence(string stateKey)
        {
            effectExecutionSequences.TryGetValue(stateKey, out int sequence);
            sequence++;
            effectExecutionSequences[stateKey] = sequence;
            return sequence;
        }

        private void TrackTimedModifier(
            RuleEffect effect,
            string stateKey,
            string source,
            bool refreshExisting)
        {
            if (effect == null || effect.Duration <= 0f)
            {
                return;
            }

            if (refreshExisting)
            {
                for (int index = timedModifiers.Count - 1; index >= 0; index--)
                {
                    if (timedModifiers[index].Source == source)
                    {
                        timedModifiers.RemoveAt(index);
                    }
                }
            }

            timedModifiers.Add(new TimedModifierState(
                stateKey,
                source,
                effect,
                Time.time + effect.Duration));
        }

        private void ExpireTimedModifiers()
        {
            for (int index = timedModifiers.Count - 1; index >= 0; index--)
            {
                TimedModifierState timed = timedModifiers[index];
                if (Time.time < timed.ExpiresAt)
                {
                    continue;
                }

                effectExecutor?.RemoveModifier(timed.Effect, timed.Source);
                timedModifiers.RemoveAt(index);

                appliedStackCounts.TryGetValue(timed.StateKey, out int count);
                int remaining = Mathf.Max(0, count - 1);
                if (remaining == 0)
                {
                    appliedStackCounts.Remove(timed.StateKey);
                }
                else
                {
                    appliedStackCounts[timed.StateKey] = remaining;
                }

                for (int stateIndex = 0;
                     stateIndex < executionStates.Count;
                     stateIndex++)
                {
                    if (executionStates[stateIndex].StateKey == timed.StateKey)
                    {
                        executionStates[stateIndex].Update(
                            remaining,
                            executionStates[stateIndex].LastAppliedValue);
                        break;
                    }
                }
            }
        }

        private string BuildModifierSource(string stateKey, int stack)
        {
            string challengeId = string.IsNullOrWhiteSpace(activeChallenge.Id)
                ? "challenge"
                : activeChallenge.Id;
            return $"Challenge:{challengeId}/{stateKey}/stack-{stack}";
        }

        private string BuildScaledModifierSource(string stateKey)
        {
            string challengeId = string.IsNullOrWhiteSpace(activeChallenge.Id)
                ? "challenge"
                : activeChallenge.Id;
            return $"Challenge:{challengeId}/{stateKey}/scaled";
        }

        private void UpdateDebugState(
            string stateKey,
            string ruleId,
            string effectId,
            int appliedStacks,
            int maxStacks,
            float lastAppliedValue)
        {
            for (int index = 0; index < executionStates.Count; index++)
            {
                if (executionStates[index].StateKey == stateKey)
                {
                    executionStates[index].Update(
                        appliedStacks,
                        lastAppliedValue);
                    return;
                }
            }

            executionStates.Add(new RuleExecutionDebugState(
                stateKey,
                ruleId,
                effectId,
                appliedStacks,
                maxStacks,
                lastAppliedValue));
        }

        private sealed class TimedModifierState
        {
            public TimedModifierState(
                string stateKey,
                string source,
                RuleEffect effect,
                float expiresAt)
            {
                StateKey = stateKey;
                Source = source;
                Effect = effect;
                ExpiresAt = expiresAt;
            }

            public string StateKey { get; }
            public string Source { get; }
            public RuleEffect Effect { get; }
            public float ExpiresAt { get; }
        }
    }

    public enum RuleStackMode
    {
        None,
        Stack
    }

    public enum RuntimeServiceTarget
    {
        Player,
        Enemy,
        Weapon
    }

    public enum RuleEffectKind
    {
        StatModifier,
        SpawnEnemy,
        GiveAmmo
    }

    public enum RuleConditionType
    {
        Always,
        RandomChance,
        EnemyType
    }

    public enum RuleComparison
    {
        Equals,
        NotEquals
    }

    public enum RuleScalingMode
    {
        Linear
    }

    public enum RuntimeValueSource
    {
        EventValue,
        PlayerMissingHPPercent
    }

    [Serializable]
    public sealed class RuleExecutionDebugState
    {
        [SerializeField] private string stateKey;
        [SerializeField] private string ruleId;
        [SerializeField] private string effectId;
        [SerializeField] private int appliedStacks;
        [SerializeField] private int maxStacks;
        [SerializeField] private float lastAppliedValue;

        public RuleExecutionDebugState(
            string newStateKey,
            string newRuleId,
            string newEffectId,
            int newAppliedStacks,
            int newMaxStacks,
            float newLastAppliedValue)
        {
            stateKey = newStateKey;
            ruleId = newRuleId;
            effectId = newEffectId;
            appliedStacks = newAppliedStacks;
            maxStacks = newMaxStacks;
            lastAppliedValue = newLastAppliedValue;
        }

        public string StateKey => stateKey;
        public float LastAppliedValue => lastAppliedValue;

        public void Update(int stacks, float value)
        {
            appliedStacks = stacks;
            lastAppliedValue = value;
        }
    }

    public sealed class ConditionEvaluator
    {
        private readonly Func<float> randomValueProvider;

        public ConditionEvaluator(Func<float> randomProvider = null)
        {
            randomValueProvider = randomProvider ?? (() => UnityEngine.Random.value);
        }

        public bool EvaluateAll(
            RuleCondition[] conditions,
            GameplayEvent gameplayEvent)
        {
            if (conditions == null || conditions.Length == 0)
            {
                return true;
            }

            for (int index = 0; index < conditions.Length; index++)
            {
                RuleCondition condition = conditions[index];
                if (!Evaluate(condition, gameplayEvent))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Evaluate(
            RuleCondition condition,
            GameplayEvent gameplayEvent)
        {
            if (condition == null ||
                !Enum.TryParse(
                    condition.Type,
                    true,
                    out RuleConditionType conditionType))
            {
                return false;
            }

            switch (conditionType)
            {
                case RuleConditionType.Always:
                    return true;
                case RuleConditionType.RandomChance:
                    return condition.Value >= 1f ||
                           (condition.Value > 0f &&
                            randomValueProvider() < condition.Value);
                case RuleConditionType.EnemyType:
                    return CompareText(
                        gameplayEvent.SubjectType,
                        condition.StringValue,
                        condition.Comparison);
                default:
                    return false;
            }
        }

        private static bool CompareText(
            string actual,
            string expected,
            string comparisonText)
        {
            RuleComparison comparison;
            if (string.IsNullOrWhiteSpace(comparisonText))
            {
                comparison = RuleComparison.Equals;
            }
            else if (!Enum.TryParse(comparisonText, true, out comparison))
            {
                return false;
            }

            bool equals = string.Equals(
                actual ?? string.Empty,
                expected ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
            return comparison == RuleComparison.Equals ? equals : !equals;
        }
    }

    public sealed class ScalingEvaluator
    {
        private readonly PlayerRuntimeService playerService;

        public ScalingEvaluator(PlayerRuntimeService player)
        {
            playerService = player;
        }

        public bool TryEvaluate(
            RuleScaling scaling,
            GameplayEvent gameplayEvent,
            out float effectValue)
        {
            effectValue = 0f;
            if (scaling == null ||
                !Enum.TryParse(
                    scaling.Mode,
                    true,
                    out RuleScalingMode scalingMode) ||
                scalingMode != RuleScalingMode.Linear ||
                !Enum.TryParse(
                    scaling.Source,
                    true,
                    out RuntimeValueSource source) ||
                scaling.SourceMax <= scaling.SourceMin)
            {
                return false;
            }

            float sourceValue;
            switch (source)
            {
                case RuntimeValueSource.EventValue:
                    sourceValue = gameplayEvent.Value;
                    break;
                case RuntimeValueSource.PlayerMissingHPPercent:
                    if (playerService == null)
                    {
                        return false;
                    }

                    sourceValue = playerService.MissingHealthPercent;
                    break;
                default:
                    return false;
            }

            float normalized = Mathf.InverseLerp(
                scaling.SourceMin,
                scaling.SourceMax,
                sourceValue);
            effectValue = Mathf.Lerp(
                scaling.EffectMin,
                scaling.EffectMax,
                normalized);
            return true;
        }
    }

    public sealed class EffectExecutor
    {
        private readonly PlayerRuntimeService playerService;
        private readonly EnemyRuntimeService enemyService;
        private readonly WeaponRuntimeService weaponService;
        private readonly SpawnRuntimeService spawnService;

        public EffectExecutor(
            PlayerRuntimeService player,
            EnemyRuntimeService enemy,
            WeaponRuntimeService weapon,
            SpawnRuntimeService spawn)
        {
            playerService = player;
            enemyService = enemy;
            weaponService = weapon;
            spawnService = spawn;
        }

        public bool IsInstantAction(RuleEffect effect)
        {
            return TryGetEffectKind(effect, out RuleEffectKind kind) &&
                   kind != RuleEffectKind.StatModifier;
        }

        public bool TryApply(RuleEffect effect, string source)
        {
            return TryApply(effect, source, effect != null ? effect.Value : 0f, false);
        }

        public bool TryApply(
            RuleEffect effect,
            string source,
            float appliedValue,
            bool replaceExisting)
        {
            if (!TryGetEffectKind(effect, out RuleEffectKind kind))
            {
                return false;
            }

            switch (kind)
            {
                case RuleEffectKind.StatModifier:
                    return ApplyStatModifier(
                        effect,
                        source,
                        appliedValue,
                        replaceExisting);
                case RuleEffectKind.SpawnEnemy:
                    return spawnService != null &&
                           spawnService.SpawnEnemy(effect.StringValue) != null;
                case RuleEffectKind.GiveAmmo:
                    return weaponService != null &&
                           weaponService.GiveAmmo(appliedValue);
                default:
                    return false;
            }
        }

        public void ClearRuleModifiers()
        {
            playerService?.ClearRuleModifiers();
            enemyService?.ClearRuleModifiers();
            weaponService?.ClearRuleModifiers();
        }

        public bool RemoveModifier(RuleEffect effect, string source)
        {
            if (!TryGetEffectKind(effect, out RuleEffectKind kind) ||
                kind != RuleEffectKind.StatModifier ||
                !Enum.TryParse(
                    effect.Target,
                    true,
                    out RuntimeServiceTarget target))
            {
                return false;
            }

            switch (target)
            {
                case RuntimeServiceTarget.Player:
                    if (playerService == null)
                    {
                        return false;
                    }

                    playerService.RemoveModifiersFromSource(source);
                    return true;
                case RuntimeServiceTarget.Enemy:
                    if (enemyService == null)
                    {
                        return false;
                    }

                    enemyService.RemoveGlobalModifiersFromSource(source);
                    return true;
                case RuntimeServiceTarget.Weapon:
                    if (weaponService == null)
                    {
                        return false;
                    }

                    weaponService.RemoveModifiersFromSource(source);
                    return true;
                default:
                    return false;
            }
        }

        private bool ApplyStatModifier(
            RuleEffect effect,
            string source,
            float value,
            bool replaceExisting)
        {
            if (!Enum.TryParse(
                    effect.Target,
                    true,
                    out RuntimeServiceTarget target) ||
                !Enum.TryParse(effect.StatId, true, out RuntimeStatId statId) ||
                !Enum.TryParse(
                    effect.Operation,
                    true,
                    out StatModifierOperation operation))
            {
                return false;
            }

            switch (target)
            {
                case RuntimeServiceTarget.Player:
                    return playerService != null && (replaceExisting
                        ? playerService.SetModifier(
                            statId,
                            source,
                            operation,
                            value)
                        : playerService.ApplyModifier(
                               statId,
                               source,
                               operation,
                               value));
                case RuntimeServiceTarget.Enemy:
                    return enemyService != null && (replaceExisting
                        ? enemyService.SetGlobalModifier(
                            statId,
                            source,
                            operation,
                            value)
                        : enemyService.ApplyGlobalModifier(
                               statId,
                               source,
                               operation,
                               value));
                case RuntimeServiceTarget.Weapon:
                    return weaponService != null && (replaceExisting
                        ? weaponService.SetModifier(
                            statId,
                            source,
                            operation,
                            value)
                        : weaponService.ApplyModifier(
                               statId,
                               source,
                               operation,
                               value));
                default:
                    return false;
            }
        }

        private static bool TryGetEffectKind(
            RuleEffect effect,
            out RuleEffectKind kind)
        {
            kind = RuleEffectKind.StatModifier;
            if (effect == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(effect.Kind))
            {
                return !string.IsNullOrWhiteSpace(effect.StatId);
            }

            return Enum.TryParse(effect.Kind, true, out kind);
        }
    }
}
