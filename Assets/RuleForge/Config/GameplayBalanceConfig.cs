using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.Config
{
    [Serializable]
    public sealed class EffectValueLimit
    {
        [SerializeField] private string effectId;
        [SerializeField] private float minimumValue;
        [SerializeField] private float maximumValue = 1f;
        [SerializeField, Min(0f)] private float maximumStackedMagnitude;

        public EffectValueLimit(
            string id,
            float minimum,
            float maximum)
        {
            effectId = id ?? string.Empty;
            minimumValue = minimum;
            maximumValue = Mathf.Max(minimum, maximum);
        }

        public string EffectId => effectId;
        public float MinimumValue => minimumValue;
        public float MaximumValue => maximumValue;
        public float MaximumStackedMagnitude => maximumStackedMagnitude;

        public void Configure(float minimum, float maximum)
        {
            minimumValue = minimum;
            maximumValue = Mathf.Max(minimum, maximum);
        }

        public void Configure(
            float minimum,
            float maximum,
            float stackedMagnitude)
        {
            Configure(minimum, maximum);
            maximumStackedMagnitude = Mathf.Max(0f, stackedMagnitude);
        }
    }

    [CreateAssetMenu(
        fileName = "GameplayBalanceConfig",
        menuName = "RuleForge/Config/Gameplay Balance")]
    public sealed class GameplayBalanceConfig : ScriptableObject
    {
        [Header("Complexity")]
        [SerializeField, Min(1)] private int maxRules = 6;
        [SerializeField, Min(1)] private int maxEffectsPerRule = 4;
        [SerializeField, Min(0)] private int maxConditionsPerRule = 3;

        [Header("Global Strength")]
        [SerializeField, Min(0f)] private float rewardMultiplier = 1f;
        [SerializeField, Min(0f)] private float penaltyMultiplier = 1f;
        [SerializeField, Min(0f)] private float minimumStrengthMultiplier;
        [SerializeField, Min(0f)] private float maximumStrengthMultiplier = 2f;

        [Header("Rule Limits")]
        [SerializeField, Min(1)] private int maxStacks = 20;
        [SerializeField, Min(0f)] private float minimumDuration;
        [SerializeField, Min(0f)] private float maximumDuration = 300f;
        [SerializeField, Range(0f, 1f)] private float minimumProbability;
        [SerializeField, Range(0f, 1f)] private float maximumProbability = 1f;

        [Header("Effect Limits")]
        [SerializeField] private List<EffectValueLimit> effectValueLimits =
            new List<EffectValueLimit>();

        [Header("Balance Feedback")]
        [SerializeField, Min(0f)] private float balancedRatioMinimum = 0.75f;
        [SerializeField, Min(0f)] private float balancedRatioMaximum = 1.25f;

        public int MaxRules => maxRules;
        public int MaxEffectsPerRule => maxEffectsPerRule;
        public int MaxConditionsPerRule => maxConditionsPerRule;
        public float RewardMultiplier => rewardMultiplier;
        public float PenaltyMultiplier => penaltyMultiplier;
        public float MinimumStrengthMultiplier => minimumStrengthMultiplier;
        public float MaximumStrengthMultiplier => maximumStrengthMultiplier;
        public int MaxStacks => maxStacks;
        public float MinimumDuration => minimumDuration;
        public float MaximumDuration => maximumDuration;
        public float MinimumProbability => minimumProbability;
        public float MaximumProbability => maximumProbability;
        public IReadOnlyList<EffectValueLimit> EffectValueLimits => effectValueLimits;
        public float BalancedRatioMinimum => balancedRatioMinimum;
        public float BalancedRatioMaximum => balancedRatioMaximum;

        public bool TryGetEffectValueLimit(
            string effectId,
            out EffectValueLimit limit)
        {
            EnsureEffectValueLimitList();
            string normalizedId = effectId ?? string.Empty;
            for (int index = 0; index < effectValueLimits.Count; index++)
            {
                EffectValueLimit candidate = effectValueLimits[index];
                if (candidate != null &&
                    string.Equals(
                        candidate.EffectId,
                        normalizedId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    limit = candidate;
                    return true;
                }
            }

            limit = null;
            return false;
        }

        public void ConfigureComplexityLimits(
            int rules,
            int effectsPerRule,
            int conditionsPerRule,
            int stacks)
        {
            maxRules = Mathf.Max(1, rules);
            maxEffectsPerRule = Mathf.Max(1, effectsPerRule);
            maxConditionsPerRule = Mathf.Max(0, conditionsPerRule);
            maxStacks = Mathf.Max(1, stacks);
        }

        public void ConfigureBalanceThresholds(float minimum, float maximum)
        {
            balancedRatioMinimum = Mathf.Max(0f, minimum);
            balancedRatioMaximum = Mathf.Max(
                balancedRatioMinimum,
                maximum);
        }

        public void ConfigureStrengthRange(float minimum, float maximum)
        {
            minimumStrengthMultiplier = Mathf.Max(0f, minimum);
            maximumStrengthMultiplier = Mathf.Max(
                minimumStrengthMultiplier,
                maximum);
            rewardMultiplier = Mathf.Clamp(
                rewardMultiplier,
                minimumStrengthMultiplier,
                maximumStrengthMultiplier);
            penaltyMultiplier = Mathf.Clamp(
                penaltyMultiplier,
                minimumStrengthMultiplier,
                maximumStrengthMultiplier);
        }

        public void EnsureEffectValueLimit(
            string effectId,
            float minimum,
            float maximum)
        {
            EnsureEffectValueLimitList();
            if (TryGetEffectValueLimit(effectId, out EffectValueLimit existing))
            {
                existing.Configure(minimum, maximum);
                return;
            }

            effectValueLimits.Add(
                new EffectValueLimit(effectId, minimum, maximum));
        }

        public void EnsureEffectValueLimit(
            string effectId,
            float minimum,
            float maximum,
            float maximumStackedMagnitude)
        {
            EnsureEffectValueLimitList();
            if (TryGetEffectValueLimit(effectId, out EffectValueLimit existing))
            {
                existing.Configure(
                    minimum,
                    maximum,
                    maximumStackedMagnitude);
                return;
            }

            EffectValueLimit limit =
                new EffectValueLimit(effectId, minimum, maximum);
            limit.Configure(minimum, maximum, maximumStackedMagnitude);
            effectValueLimits.Add(limit);
        }

        private void OnValidate()
        {
            maxRules = Mathf.Max(1, maxRules);
            maxEffectsPerRule = Mathf.Max(1, maxEffectsPerRule);
            maxConditionsPerRule = Mathf.Max(0, maxConditionsPerRule);
            minimumStrengthMultiplier = Mathf.Max(0f, minimumStrengthMultiplier);
            maximumStrengthMultiplier = Mathf.Max(
                minimumStrengthMultiplier,
                maximumStrengthMultiplier);
            rewardMultiplier = Mathf.Clamp(
                rewardMultiplier,
                minimumStrengthMultiplier,
                maximumStrengthMultiplier);
            penaltyMultiplier = Mathf.Clamp(
                penaltyMultiplier,
                minimumStrengthMultiplier,
                maximumStrengthMultiplier);
            maxStacks = Mathf.Max(1, maxStacks);
            minimumDuration = Mathf.Max(0f, minimumDuration);
            maximumDuration = Mathf.Max(minimumDuration, maximumDuration);
            minimumProbability = Mathf.Clamp01(minimumProbability);
            maximumProbability = Mathf.Clamp(maximumProbability, minimumProbability, 1f);
            balancedRatioMinimum = Mathf.Max(0f, balancedRatioMinimum);
            balancedRatioMaximum = Mathf.Max(
                balancedRatioMinimum,
                balancedRatioMaximum);
            EnsureEffectValueLimitList();
        }

        private void EnsureEffectValueLimitList()
        {
            if (effectValueLimits == null)
            {
                effectValueLimits = new List<EffectValueLimit>();
            }
        }
    }
}
