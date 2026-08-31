using System;
using System.Collections.Generic;
using RuleForge.DSL;
using RuleForge.Rules;
using UnityEngine;

namespace RuleForge.AI
{
    public enum ChallengePatchOperationType
    {
        AddRule,
        RemoveRule,
        ModifyValue,
        ModifyMaxStack,
        ModifyDuration,
        ModifyProbability,
        ModifyGoal
    }

    [Serializable]
    public sealed class ChallengePatch
    {
        [SerializeField] private ChallengePatchOperation[] operations =
            Array.Empty<ChallengePatchOperation>();

        public ChallengePatchOperation[] Operations =>
            operations ?? Array.Empty<ChallengePatchOperation>();
    }

    [Serializable]
    public sealed class ChallengePatchOperation
    {
        [SerializeField] private string operation;
        [SerializeField] private string ruleId;
        [SerializeField] private string effectId;
        [SerializeField] private int conditionIndex;
        [SerializeField] private float value;
        [SerializeField] private int maxStacks;
        [SerializeField] private float duration;
        [SerializeField] private float probability;
        [SerializeField] private string goal;
        [SerializeField] private float goalTarget;
        [SerializeField] private GameplayRule rule;

        public string Operation => operation ?? string.Empty;
        public string RuleId => ruleId ?? string.Empty;
        public string EffectId => effectId ?? string.Empty;
        public int ConditionIndex => conditionIndex;
        public float Value => value;
        public int MaxStacks => maxStacks;
        public float Duration => duration;
        public float Probability => probability;
        public string Goal => goal ?? string.Empty;
        public float GoalTarget => goalTarget;
        public GameplayRule Rule => rule;
    }

    public static class ChallengePatchApplier
    {
        public static bool TryApply(
            ChallengeSpec current,
            ChallengePatch patch,
            out ChallengeSpec candidate,
            out string error)
        {
            candidate = null;
            if (current == null)
            {
                error = "Current ChallengeSpec is required.";
                return false;
            }

            if (patch == null || patch.Operations.Length == 0)
            {
                error = "ChallengePatch must contain at least one operation.";
                return false;
            }

            ChallengeSpec clone = JsonUtility.FromJson<ChallengeSpec>(
                JsonUtility.ToJson(current));
            if (clone == null)
            {
                error = "Current ChallengeSpec could not be cloned.";
                return false;
            }

            List<GameplayRule> rules =
                new List<GameplayRule>(clone.Rules);
            string goal = clone.Goal;
            float goalTarget = clone.GoalTarget;
            ChallengePatchOperation[] operations = patch.Operations;
            for (int index = 0; index < operations.Length; index++)
            {
                ChallengePatchOperation operation = operations[index];
                if (!TryApplyOperation(
                        operation,
                        rules,
                        ref goal,
                        ref goalTarget,
                        out error))
                {
                    error = $"operations[{index}]: {error}";
                    return false;
                }
            }

            candidate = ChallengeSpec.Create(
                clone.Id,
                clone.DisplayName,
                goal,
                goalTarget,
                clone.Weapon,
                rules.ToArray());
            error = string.Empty;
            return true;
        }

        private static bool TryApplyOperation(
            ChallengePatchOperation operation,
            List<GameplayRule> rules,
            ref string goal,
            ref float goalTarget,
            out string error)
        {
            if (operation == null ||
                !Enum.TryParse(
                    operation.Operation,
                    true,
                    out ChallengePatchOperationType operationType))
            {
                error = $"Unknown operation '{operation?.Operation}'.";
                return false;
            }

            if (operationType == ChallengePatchOperationType.ModifyGoal)
            {
                if (string.IsNullOrWhiteSpace(operation.Goal))
                {
                    error = "ModifyGoal requires a non-empty goal.";
                    return false;
                }

                goal = operation.Goal.Trim();
                goalTarget = operation.GoalTarget;
                error = string.Empty;
                return true;
            }

            if (operationType == ChallengePatchOperationType.AddRule)
            {
                if (operation.Rule == null)
                {
                    error = "AddRule requires a complete rule.";
                    return false;
                }

                if (FindRule(rules, operation.Rule.Id) != null)
                {
                    error = $"Rule '{operation.Rule.Id}' already exists.";
                    return false;
                }

                rules.Add(operation.Rule);
                error = string.Empty;
                return true;
            }

            int ruleIndex = FindRuleIndex(rules, operation.RuleId);
            if (ruleIndex < 0)
            {
                error = $"Rule '{operation.RuleId}' was not found.";
                return false;
            }

            if (operationType == ChallengePatchOperationType.RemoveRule)
            {
                rules.RemoveAt(ruleIndex);
                error = string.Empty;
                return true;
            }

            GameplayRule rule = rules[ruleIndex];
            if (operationType == ChallengePatchOperationType.ModifyProbability)
            {
                RuleCondition[] conditions = rule.Conditions;
                int conditionIndex = operation.ConditionIndex;
                if (conditionIndex < 0 || conditionIndex >= conditions.Length)
                {
                    error = $"Condition index {conditionIndex} is out of range.";
                    return false;
                }

                RuleCondition condition = conditions[conditionIndex];
                if (condition == null ||
                    !string.Equals(
                        condition.Type,
                        RuleConditionType.RandomChance.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    error = "ModifyProbability target is not RandomChance.";
                    return false;
                }

                condition.SetValue(operation.Probability);
                error = string.Empty;
                return true;
            }

            RuleEffect effect = FindEffect(rule, operation.EffectId);
            if (effect == null)
            {
                error =
                    $"Effect '{operation.EffectId}' was not found in " +
                    $"rule '{operation.RuleId}'.";
                return false;
            }

            switch (operationType)
            {
                case ChallengePatchOperationType.ModifyValue:
                    effect.SetValue(operation.Value);
                    break;
                case ChallengePatchOperationType.ModifyMaxStack:
                    effect.SetMaxStacks(operation.MaxStacks);
                    break;
                case ChallengePatchOperationType.ModifyDuration:
                    effect.SetDuration(operation.Duration);
                    break;
                default:
                    error = $"Operation '{operation.Operation}' is unsupported.";
                    return false;
            }

            error = string.Empty;
            return true;
        }

        private static GameplayRule FindRule(
            List<GameplayRule> rules,
            string ruleId)
        {
            int index = FindRuleIndex(rules, ruleId);
            return index >= 0 ? rules[index] : null;
        }

        private static int FindRuleIndex(
            List<GameplayRule> rules,
            string ruleId)
        {
            for (int index = 0; index < rules.Count; index++)
            {
                GameplayRule rule = rules[index];
                if (rule != null &&
                    string.Equals(
                        rule.Id,
                        ruleId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static RuleEffect FindEffect(
            GameplayRule rule,
            string effectId)
        {
            RuleEffect[] effects = rule.Effects;
            for (int index = 0; index < effects.Length; index++)
            {
                RuleEffect effect = effects[index];
                if (effect != null &&
                    string.Equals(
                        effect.EffectId,
                        effectId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return effect;
                }
            }

            return null;
        }
    }
}
