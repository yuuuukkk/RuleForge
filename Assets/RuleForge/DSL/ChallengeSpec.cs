using System;
using UnityEngine;

namespace RuleForge.DSL
{
    public enum ChallengeGoalType
    {
        Survive,
        KillCount,
        Score
    }

    [Serializable]
    public sealed class ChallengeSpec
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string goal;
        [SerializeField] private float goalTarget = 10f;
        [SerializeField] private string weapon;
        [SerializeField] private GameplayRule[] rules = Array.Empty<GameplayRule>();

        public string Id => id ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string Goal => goal ?? string.Empty;
        public float GoalTarget => goalTarget;
        public string Weapon => weapon ?? string.Empty;
        public GameplayRule[] Rules => rules ?? Array.Empty<GameplayRule>();

        public static ChallengeSpec Create(
            string challengeId,
            string challengeName,
            string challengeGoal,
            string challengeWeapon,
            GameplayRule[] challengeRules)
        {
            return Create(
                challengeId,
                challengeName,
                challengeGoal,
                10f,
                challengeWeapon,
                challengeRules);
        }

        public static ChallengeSpec Create(
            string challengeId,
            string challengeName,
            string challengeGoal,
            float challengeGoalTarget,
            string challengeWeapon,
            GameplayRule[] challengeRules)
        {
            return new ChallengeSpec
            {
                id = challengeId ?? string.Empty,
                displayName = challengeName ?? string.Empty,
                goal = challengeGoal ?? string.Empty,
                goalTarget = challengeGoalTarget,
                weapon = challengeWeapon ?? string.Empty,
                rules = challengeRules ?? Array.Empty<GameplayRule>()
            };
        }
    }

    [Serializable]
    public sealed class GameplayRule
    {
        [SerializeField] private string id;
        [SerializeField] private RuleTrigger trigger = new RuleTrigger();
        [SerializeField] private RuleCondition[] conditions = Array.Empty<RuleCondition>();
        [SerializeField] private RuleEffect[] effects = Array.Empty<RuleEffect>();

        public string Id => id ?? string.Empty;
        public RuleTrigger Trigger => trigger;
        public RuleCondition[] Conditions => conditions ?? Array.Empty<RuleCondition>();
        public RuleEffect[] Effects => effects ?? Array.Empty<RuleEffect>();

        public static GameplayRule Create(
            string ruleId,
            string triggerType,
            RuleCondition[] ruleConditions,
            RuleEffect[] ruleEffects)
        {
            return new GameplayRule
            {
                id = ruleId ?? string.Empty,
                trigger = RuleTrigger.Create(triggerType),
                conditions = ruleConditions ?? Array.Empty<RuleCondition>(),
                effects = ruleEffects ?? Array.Empty<RuleEffect>()
            };
        }

        public void SetTrigger(string triggerType)
        {
            trigger = RuleTrigger.Create(triggerType);
        }

        public void AddCondition(RuleCondition condition)
        {
            if (condition == null)
            {
                return;
            }

            RuleCondition[] current = Conditions;
            RuleCondition[] expanded = new RuleCondition[current.Length + 1];
            Array.Copy(current, expanded, current.Length);
            expanded[current.Length] = condition;
            conditions = expanded;
        }

        public bool RemoveConditionAt(int index)
        {
            RuleCondition[] current = Conditions;
            if (index < 0 || index >= current.Length)
            {
                return false;
            }

            RuleCondition[] reduced = new RuleCondition[current.Length - 1];
            if (index > 0)
            {
                Array.Copy(current, 0, reduced, 0, index);
            }

            if (index < current.Length - 1)
            {
                Array.Copy(
                    current,
                    index + 1,
                    reduced,
                    index,
                    current.Length - index - 1);
            }

            conditions = reduced;
            return true;
        }

        public void AddEffect(RuleEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            RuleEffect[] current = Effects;
            RuleEffect[] expanded = new RuleEffect[current.Length + 1];
            Array.Copy(current, expanded, current.Length);
            expanded[current.Length] = effect;
            effects = expanded;
        }

        public bool RemoveEffect(string effectId)
        {
            int index = FindEffectIndex(effectId);
            if (index < 0)
            {
                return false;
            }

            RuleEffect[] current = Effects;
            RuleEffect[] reduced = new RuleEffect[current.Length - 1];
            if (index > 0)
            {
                Array.Copy(current, 0, reduced, 0, index);
            }

            if (index < current.Length - 1)
            {
                Array.Copy(
                    current,
                    index + 1,
                    reduced,
                    index,
                    current.Length - index - 1);
            }

            effects = reduced;
            return true;
        }

        public bool ReplaceEffect(string effectId, RuleEffect replacement)
        {
            int index = FindEffectIndex(effectId);
            if (index < 0 || replacement == null)
            {
                return false;
            }

            effects[index] = replacement;
            return true;
        }

        private int FindEffectIndex(string effectId)
        {
            RuleEffect[] current = Effects;
            for (int index = 0; index < current.Length; index++)
            {
                if (current[index] != null && string.Equals(
                        current[index].EffectId,
                        effectId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }
    }

    [Serializable]
    public sealed class RuleTrigger
    {
        [SerializeField] private string type;

        public string Type => type ?? string.Empty;

        public static RuleTrigger Create(string triggerType)
        {
            return new RuleTrigger { type = triggerType ?? string.Empty };
        }
    }

    [Serializable]
    public sealed class RuleCondition
    {
        [SerializeField] private string type;
        [SerializeField] private string comparison;
        [SerializeField] private float value;
        [SerializeField] private string stringValue;

        public string Type => type ?? string.Empty;
        public string Comparison => comparison ?? string.Empty;
        public float Value => value;
        public string StringValue => stringValue ?? string.Empty;

        public static RuleCondition Create(
            string conditionType,
            string conditionComparison,
            float conditionValue,
            string conditionStringValue)
        {
            return new RuleCondition
            {
                type = conditionType ?? string.Empty,
                comparison = conditionComparison ?? string.Empty,
                value = conditionValue,
                stringValue = conditionStringValue ?? string.Empty
            };
        }

        public void SetValue(float newValue)
        {
            value = newValue;
        }
    }

    [Serializable]
    public sealed class RuleEffect
    {
        [SerializeField] private string effectId;
        [SerializeField] private string kind;
        [SerializeField] private string target;
        [SerializeField] private string statId;
        [SerializeField] private string operation;
        [SerializeField] private float value;
        [SerializeField] private string stringValue;
        [SerializeField] private string stackMode;
        [SerializeField] private int maxStacks;
        [SerializeField] private float duration;
        [SerializeField] private RuleScaling scaling;

        public string EffectId => effectId ?? string.Empty;
        public string Kind => kind ?? string.Empty;
        public string Target => target ?? string.Empty;
        public string StatId => statId ?? string.Empty;
        public string Operation => operation ?? string.Empty;
        public float Value => value;
        public string StringValue => stringValue ?? string.Empty;
        public string StackMode => stackMode ?? string.Empty;
        public int MaxStacks => maxStacks;
        public float Duration => duration;
        public RuleScaling Scaling => scaling;

        public static RuleEffect Create(
            string id,
            string effectKind,
            string effectTarget,
            string effectStatId,
            string effectOperation,
            float effectValue,
            string effectStringValue,
            string effectStackMode,
            int effectMaxStacks,
            float effectDuration,
            RuleScaling effectScaling = null)
        {
            return new RuleEffect
            {
                effectId = id ?? string.Empty,
                kind = effectKind ?? string.Empty,
                target = effectTarget ?? string.Empty,
                statId = effectStatId ?? string.Empty,
                operation = effectOperation ?? string.Empty,
                value = effectValue,
                stringValue = effectStringValue ?? string.Empty,
                stackMode = effectStackMode ?? string.Empty,
                maxStacks = effectMaxStacks,
                duration = effectDuration,
                scaling = effectScaling
            };
        }

        public void SetValue(float newValue)
        {
            value = newValue;
        }

        public void SetMaxStacks(int newMaxStacks)
        {
            maxStacks = newMaxStacks;
        }

        public void SetDuration(float newDuration)
        {
            duration = newDuration;
        }

        public void SetScaling(RuleScaling newScaling)
        {
            scaling = newScaling;
        }
    }

    [Serializable]
    public sealed class RuleScaling
    {
        [SerializeField] private string source;
        [SerializeField] private string mode;
        [SerializeField] private float sourceMin;
        [SerializeField] private float sourceMax = 1f;
        [SerializeField] private float effectMin;
        [SerializeField] private float effectMax = 1f;

        public string Source => source ?? string.Empty;
        public string Mode => mode ?? string.Empty;
        public float SourceMin => sourceMin;
        public float SourceMax => sourceMax;
        public float EffectMin => effectMin;
        public float EffectMax => effectMax;

        public static RuleScaling Create(
            string scalingSource,
            string scalingMode,
            float scalingSourceMin,
            float scalingSourceMax,
            float scalingEffectMin,
            float scalingEffectMax)
        {
            return new RuleScaling
            {
                source = scalingSource ?? string.Empty,
                mode = scalingMode ?? string.Empty,
                sourceMin = scalingSourceMin,
                sourceMax = scalingSourceMax,
                effectMin = scalingEffectMin,
                effectMax = scalingEffectMax
            };
        }

        public void SetEffectRange(float newEffectMin, float newEffectMax)
        {
            effectMin = newEffectMin;
            effectMax = newEffectMax;
        }
    }
}
