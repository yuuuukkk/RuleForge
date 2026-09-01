using System;
using System.Collections.Generic;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Rules;
using UnityEngine;

namespace RuleForge.Validation
{
    public enum BalanceResult
    {
        Balanced,
        RewardHeavy,
        RiskHeavy,
        NoSignal
    }

    public enum EstimatedDifficulty
    {
        Unknown,
        Relaxed,
        Moderate,
        Hard,
        Extreme
    }

    public enum GrowthSpeed
    {
        None,
        Slow,
        Medium,
        Fast
    }

    [Serializable]
    public sealed class BalanceEvaluation
    {
        [SerializeField] private float rewardScore;
        [SerializeField] private float penaltyScore;
        [SerializeField] private float rewardPenaltyRatio;
        [SerializeField] private BalanceResult result;
        [SerializeField] private EstimatedDifficulty estimatedDifficulty;
        [SerializeField] private float growthScore;
        [SerializeField] private GrowthSpeed growthSpeed;
        [SerializeField] private string[] gameplayTags = Array.Empty<string>();

        public BalanceEvaluation(
            float reward,
            float penalty,
            float ratio,
            BalanceResult balanceResult)
        {
            rewardScore = reward;
            penaltyScore = penalty;
            rewardPenaltyRatio = ratio;
            result = balanceResult;
            estimatedDifficulty = EstimatedDifficulty.Unknown;
            growthScore = 0f;
            growthSpeed = GrowthSpeed.None;
            gameplayTags = Array.Empty<string>();
        }

        public BalanceEvaluation(
            float reward,
            float penalty,
            float ratio,
            BalanceResult balanceResult,
            EstimatedDifficulty difficulty,
            float evaluatedGrowthScore,
            GrowthSpeed evaluatedGrowthSpeed,
            string[] tags)
        {
            rewardScore = reward;
            penaltyScore = penalty;
            rewardPenaltyRatio = ratio;
            result = balanceResult;
            estimatedDifficulty = difficulty;
            growthScore = evaluatedGrowthScore;
            growthSpeed = evaluatedGrowthSpeed;
            gameplayTags = tags ?? Array.Empty<string>();
        }

        public float RewardScore => rewardScore;
        public float PenaltyScore => penaltyScore;
        public float RewardPenaltyRatio => rewardPenaltyRatio;
        public BalanceResult Result => result;
        public EstimatedDifficulty Difficulty => estimatedDifficulty;
        public float GrowthScore => growthScore;
        public GrowthSpeed Growth => growthSpeed;
        public IReadOnlyList<string> GameplayTags =>
            gameplayTags ?? Array.Empty<string>();
    }

    public sealed class BalanceEvaluator
    {
        private readonly GameplayBalanceConfig balanceConfig;
        private readonly EffectCatalog effectCatalog;

        public BalanceEvaluator(
            GameplayBalanceConfig balance,
            EffectCatalog catalog)
        {
            balanceConfig = balance;
            effectCatalog = catalog;
        }

        public BalanceEvaluation Evaluate(ChallengeSpec challenge)
        {
            return Evaluate(challenge, 1f, 1f);
        }

        public BalanceEvaluation Evaluate(
            ChallengeSpec challenge,
            float rewardStrength,
            float penaltyStrength)
        {
            if (challenge == null ||
                balanceConfig == null ||
                effectCatalog == null)
            {
                return new BalanceEvaluation(0f, 0f, 1f, BalanceResult.NoSignal);
            }

            float rewardScore = 0f;
            float penaltyScore = 0f;
            float growthScore = 0f;
            bool hasRandom = false;
            bool hasStackGrowth = false;
            bool hasScaling = false;
            bool hasBurst = false;
            bool hasDamageReward = false;
            GameplayRule[] rules = challenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null)
                {
                    continue;
                }

                float probabilityFactor = GetProbabilityFactor(rule.Conditions);
                hasRandom |= probabilityFactor < 0.999f;
                float triggerFrequency = GetTriggerFrequency(rule);
                RuleEffect[] effects = rule.Effects;
                for (int effectIndex = 0;
                     effectIndex < effects.Length;
                     effectIndex++)
                {
                    RuleEffect effect = effects[effectIndex];
                    if (effect == null ||
                        !effectCatalog.TryGetDefinition(
                            effect.EffectId,
                            out EffectDefinition definition))
                    {
                        continue;
                    }

                    float score = EvaluateEffect(
                        effect,
                        definition,
                        probabilityFactor);
                    if (definition.Polarity == EffectPolarity.Reward)
                    {
                        rewardScore += score * Mathf.Max(0f, rewardStrength);
                    }
                    else if (definition.Polarity == EffectPolarity.Penalty)
                    {
                        penaltyScore += score * Mathf.Max(0f, penaltyStrength);
                    }

                    if (Enum.TryParse(
                            effect.StackMode,
                            true,
                            out RuleStackMode stackMode) &&
                        stackMode == RuleStackMode.Stack &&
                        effect.MaxStacks > 1)
                    {
                        hasStackGrowth = true;
                        growthScore += Mathf.Sqrt(effect.MaxStacks) *
                                       probabilityFactor * triggerFrequency;
                    }

                    if (effect.Scaling != null &&
                        !string.IsNullOrWhiteSpace(effect.Scaling.Source))
                    {
                        hasScaling = true;
                        growthScore += 1.5f * triggerFrequency;
                    }

                    hasBurst |= effect.Duration > 0f;
                    hasDamageReward |=
                        definition.Polarity == EffectPolarity.Reward &&
                        string.Equals(
                            effect.StatId,
                            RuleForge.Runtime.Stats.RuntimeStatId.WeaponDamage.ToString(),
                            StringComparison.OrdinalIgnoreCase);
                }
            }

            float ratio = penaltyScore > 0f
                ? rewardScore / penaltyScore
                : rewardScore > 0f
                    ? float.PositiveInfinity
                    : 1f;
            BalanceResult result = ResolveResult(rewardScore, penaltyScore, ratio);
            EstimatedDifficulty difficulty = ResolveDifficulty(
                challenge,
                penaltyScore,
                result);
            GrowthSpeed growthSpeed = ResolveGrowthSpeed(growthScore);
            string[] tags = BuildTags(
                challenge,
                rewardScore,
                penaltyScore,
                hasRandom,
                hasStackGrowth,
                hasScaling,
                hasBurst,
                hasDamageReward);
            return new BalanceEvaluation(
                rewardScore,
                penaltyScore,
                ratio,
                result,
                difficulty,
                growthScore,
                growthSpeed,
                tags);
        }

        private float EvaluateEffect(
            RuleEffect effect,
            EffectDefinition definition,
            float probabilityFactor)
        {
            float magnitude = GetEffectMagnitude(effect);
            float normalizedMagnitude = 1f;
            if (balanceConfig.TryGetEffectValueLimit(
                    effect.EffectId,
                    out EffectValueLimit limit))
            {
                float maximumMagnitude = Mathf.Max(
                    Mathf.Abs(limit.MinimumValue),
                    Mathf.Abs(limit.MaximumValue));
                normalizedMagnitude = maximumMagnitude > 0f
                    ? Mathf.Clamp01(Mathf.Abs(magnitude) / maximumMagnitude)
                    : 1f;
            }

            float repetitionFactor = 1f;
            if (Enum.TryParse(
                    effect.StackMode,
                    true,
                    out RuleStackMode stackMode) &&
                stackMode == RuleStackMode.Stack)
            {
                repetitionFactor = Mathf.Sqrt(Mathf.Max(1, effect.MaxStacks));
            }

            float durationFactor = effect.Duration > 0f
                ? Mathf.Clamp(Mathf.Sqrt(effect.Duration / 10f), 0.35f, 1.25f)
                : 1f;

            return definition.BalanceWeight *
                   normalizedMagnitude *
                   repetitionFactor *
                   probabilityFactor *
                   durationFactor;
        }

        private static EstimatedDifficulty ResolveDifficulty(
            ChallengeSpec challenge,
            float penaltyScore,
            BalanceResult result)
        {
            float score = penaltyScore;
            if (result == BalanceResult.RiskHeavy)
            {
                score *= 1.2f;
            }

            if (string.Equals(
                    challenge.Goal,
                    ChallengeGoalType.Survive.ToString(),
                    StringComparison.OrdinalIgnoreCase) &&
                challenge.GoalTarget >= 45f)
            {
                score += 0.75f;
            }

            if (score <= 0f)
            {
                return EstimatedDifficulty.Relaxed;
            }

            if (score < 0.75f)
            {
                return EstimatedDifficulty.Relaxed;
            }

            if (score < 2.5f)
            {
                return EstimatedDifficulty.Moderate;
            }

            if (score < 6f)
            {
                return EstimatedDifficulty.Hard;
            }

            return EstimatedDifficulty.Extreme;
        }

        private static GrowthSpeed ResolveGrowthSpeed(float score)
        {
            if (score <= 0f)
            {
                return GrowthSpeed.None;
            }

            if (score < 1.5f)
            {
                return GrowthSpeed.Slow;
            }

            return score < 4f ? GrowthSpeed.Medium : GrowthSpeed.Fast;
        }

        private static string[] BuildTags(
            ChallengeSpec challenge,
            float rewardScore,
            float penaltyScore,
            bool hasRandom,
            bool hasStackGrowth,
            bool hasScaling,
            bool hasBurst,
            bool hasDamageReward)
        {
            System.Collections.Generic.List<string> tags =
                new System.Collections.Generic.List<string>();
            if (rewardScore >= 0.75f && penaltyScore >= 0.75f)
            {
                tags.Add("High Risk / High Reward");
            }

            if (hasStackGrowth)
            {
                tags.Add("Snowball");
            }

            if (hasDamageReward && penaltyScore > 0f)
            {
                tags.Add("Glass Cannon");
            }

            if (hasRandom)
            {
                tags.Add("Random");
            }

            if (string.Equals(
                    challenge.Goal,
                    ChallengeGoalType.Survive.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("Survival");
            }

            if (hasScaling)
            {
                tags.Add("Scaling");
            }

            if (hasBurst)
            {
                tags.Add("Burst");
            }

            return tags.ToArray();
        }

        private static float GetTriggerFrequency(GameplayRule rule)
        {
            if (rule?.Trigger == null || !Enum.TryParse(
                    rule.Trigger.Type,
                    true,
                    out GameplayEventType trigger))
            {
                return 1f;
            }

            switch (trigger)
            {
                case GameplayEventType.WeaponFired:
                case GameplayEventType.EnemyHit:
                case GameplayEventType.PlayerHPChanged:
                case GameplayEventType.PlayerAmmoChanged:
                    return 1.5f;
                case GameplayEventType.PlayerReload:
                    return 0.65f;
                case GameplayEventType.GameStarted:
                    return 0.1f;
                default:
                    return 1f;
            }
        }

        private BalanceResult ResolveResult(
            float rewardScore,
            float penaltyScore,
            float ratio)
        {
            if (rewardScore <= 0f && penaltyScore <= 0f)
            {
                return BalanceResult.NoSignal;
            }

            if (ratio < balanceConfig.BalancedRatioMinimum)
            {
                return BalanceResult.RiskHeavy;
            }

            if (ratio > balanceConfig.BalancedRatioMaximum)
            {
                return BalanceResult.RewardHeavy;
            }

            return BalanceResult.Balanced;
        }

        private static float GetEffectMagnitude(RuleEffect effect)
        {
            RuleScaling scaling = effect.Scaling;
            if (scaling == null || string.IsNullOrWhiteSpace(scaling.Source))
            {
                return effect.Value;
            }

            return Mathf.Max(
                Mathf.Abs(scaling.EffectMin),
                Mathf.Abs(scaling.EffectMax));
        }

        private static float GetProbabilityFactor(RuleCondition[] conditions)
        {
            float probability = 1f;
            if (conditions == null)
            {
                return probability;
            }

            for (int index = 0; index < conditions.Length; index++)
            {
                RuleCondition condition = conditions[index];
                if (condition != null &&
                    string.Equals(
                        condition.Type,
                        RuleConditionType.RandomChance.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    probability *= Mathf.Clamp01(condition.Value);
                }
            }

            return probability;
        }
    }
}
