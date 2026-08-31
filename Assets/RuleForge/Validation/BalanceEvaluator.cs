using System;
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

    [Serializable]
    public sealed class BalanceEvaluation
    {
        [SerializeField] private float rewardScore;
        [SerializeField] private float penaltyScore;
        [SerializeField] private float rewardPenaltyRatio;
        [SerializeField] private BalanceResult result;

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
        }

        public float RewardScore => rewardScore;
        public float PenaltyScore => penaltyScore;
        public float RewardPenaltyRatio => rewardPenaltyRatio;
        public BalanceResult Result => result;
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
            GameplayRule[] rules = challenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null)
                {
                    continue;
                }

                float probabilityFactor = GetProbabilityFactor(rule.Conditions);
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
                }
            }

            float ratio = penaltyScore > 0f
                ? rewardScore / penaltyScore
                : rewardScore > 0f
                    ? float.PositiveInfinity
                    : 1f;
            BalanceResult result = ResolveResult(rewardScore, penaltyScore, ratio);
            return new BalanceEvaluation(
                rewardScore,
                penaltyScore,
                ratio,
                result);
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

            return definition.BalanceWeight *
                   normalizedMagnitude *
                   repetitionFactor *
                   probabilityFactor;
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
