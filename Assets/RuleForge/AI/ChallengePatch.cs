using System;
using System.Collections.Generic;
using System.Globalization;
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
        ModifyGoal,
        ModifyWeapon,
        ModifyCondition,
        ModifyScaling,
        ModifyTrigger,
        AddCondition,
        RemoveCondition,
        AddEffect,
        RemoveEffect,
        ReplaceEffect
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
        [SerializeField] private string weapon;
        [SerializeField] private string trigger;
        [SerializeField] private RuleCondition condition;
        [SerializeField] private RuleEffect effect;
        [SerializeField] private RuleScaling scaling;
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
        public string Weapon => weapon ?? string.Empty;
        public string Trigger => trigger ?? string.Empty;
        public RuleCondition Condition => condition;
        public RuleEffect Effect => effect;
        public RuleScaling Scaling => scaling;
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
            string weapon = clone.Weapon;
            ChallengePatchOperation[] operations = patch.Operations;
            for (int index = 0; index < operations.Length; index++)
            {
                ChallengePatchOperation operation = operations[index];
                if (!TryApplyOperation(
                        operation,
                        rules,
                        ref goal,
                        ref goalTarget,
                        ref weapon,
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
                weapon,
                rules.ToArray());
            error = string.Empty;
            return true;
        }

        private static bool TryApplyOperation(
            ChallengePatchOperation operation,
            List<GameplayRule> rules,
            ref string goal,
            ref float goalTarget,
            ref string weapon,
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

            if (operationType == ChallengePatchOperationType.ModifyWeapon)
            {
                if (string.IsNullOrWhiteSpace(operation.Weapon))
                {
                    error = "ModifyWeapon requires a non-empty weapon.";
                    return false;
                }

                weapon = operation.Weapon.Trim();
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
            if (operationType == ChallengePatchOperationType.ModifyTrigger)
            {
                if (string.IsNullOrWhiteSpace(operation.Trigger))
                {
                    error = "ModifyTrigger requires a non-empty trigger.";
                    return false;
                }

                rule.SetTrigger(operation.Trigger.Trim());
                error = string.Empty;
                return true;
            }

            if (operationType == ChallengePatchOperationType.AddCondition)
            {
                if (operation.Condition == null)
                {
                    error = "AddCondition requires a complete condition.";
                    return false;
                }

                rule.AddCondition(operation.Condition);
                error = string.Empty;
                return true;
            }

            if (operationType == ChallengePatchOperationType.RemoveCondition)
            {
                if (!rule.RemoveConditionAt(operation.ConditionIndex))
                {
                    error = $"Condition index {operation.ConditionIndex} is out of range.";
                    return false;
                }

                error = string.Empty;
                return true;
            }

            if (operationType == ChallengePatchOperationType.ModifyCondition)
            {
                RuleCondition[] conditions = rule.Conditions;
                int conditionIndex = operation.ConditionIndex;
                if (conditionIndex < 0 || conditionIndex >= conditions.Length)
                {
                    error = $"Condition index {conditionIndex} is out of range.";
                    return false;
                }

                if (operation.Condition == null)
                {
                    error = "ModifyCondition requires a complete condition.";
                    return false;
                }

                conditions[conditionIndex] = operation.Condition;
                error = string.Empty;
                return true;
            }

            if (operationType == ChallengePatchOperationType.AddEffect)
            {
                if (operation.Effect == null)
                {
                    error = "AddEffect requires a complete effect.";
                    return false;
                }

                if (FindEffect(rule, operation.Effect.EffectId) != null)
                {
                    error = $"Effect '{operation.Effect.EffectId}' already exists.";
                    return false;
                }

                rule.AddEffect(operation.Effect);
                error = string.Empty;
                return true;
            }

            if (operationType == ChallengePatchOperationType.RemoveEffect)
            {
                if (!rule.RemoveEffect(operation.EffectId))
                {
                    error = $"Effect '{operation.EffectId}' was not found in rule '{operation.RuleId}'.";
                    return false;
                }

                error = string.Empty;
                return true;
            }

            if (operationType == ChallengePatchOperationType.ReplaceEffect)
            {
                if (operation.Effect == null)
                {
                    error = "ReplaceEffect requires a complete effect.";
                    return false;
                }

                if (!rule.ReplaceEffect(operation.EffectId, operation.Effect))
                {
                    error = $"Effect '{operation.EffectId}' was not found in rule '{operation.RuleId}'.";
                    return false;
                }

                error = string.Empty;
                return true;
            }

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
                case ChallengePatchOperationType.ModifyScaling:
                    effect.SetScaling(operation.Scaling);
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

    public static class ChallengeRepairScope
    {
        public static bool ContainsOnlyInitiallyChangedFields(
            ChallengeSpec baseline,
            ChallengeSpec initialCandidate,
            ChallengeSpec repairedCandidate,
            out string[] extraPaths)
        {
            HashSet<string> allowed = FindChangedPaths(
                baseline,
                initialCandidate);
            HashSet<string> repaired = FindChangedPaths(
                baseline,
                repairedCandidate);
            List<string> extras = new List<string>();
            foreach (string path in repaired)
            {
                if (!allowed.Contains(path))
                {
                    extras.Add(path);
                }
            }

            extras.Sort(StringComparer.Ordinal);
            extraPaths = extras.ToArray();
            return extraPaths.Length == 0;
        }

        public static HashSet<string> FindChangedPaths(
            ChallengeSpec before,
            ChallengeSpec after)
        {
            Dictionary<string, string> left = Flatten(before);
            Dictionary<string, string> right = Flatten(after);
            HashSet<string> paths = new HashSet<string>(left.Keys);
            paths.UnionWith(right.Keys);
            paths.RemoveWhere(path =>
                left.TryGetValue(path, out string oldValue) &&
                right.TryGetValue(path, out string newValue) &&
                string.Equals(oldValue, newValue, StringComparison.Ordinal));
            return paths;
        }

        private static Dictionary<string, string> Flatten(ChallengeSpec challenge)
        {
            Dictionary<string, string> values =
                new Dictionary<string, string>(StringComparer.Ordinal);
            if (challenge == null)
            {
                return values;
            }

            Add(values, "challenge/id", challenge.Id);
            Add(values, "challenge/displayName", challenge.DisplayName);
            Add(values, "challenge/goal", challenge.Goal);
            Add(values, "challenge/goalTarget", challenge.GoalTarget);
            Add(values, "challenge/weapon", challenge.Weapon);
            GameplayRule[] rules = challenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                string ruleKey = "rules/" + StableKey(
                    rule != null ? rule.Id : string.Empty,
                    ruleIndex);
                Add(values, ruleKey + "/present", rule != null ? "1" : "0");
                if (rule == null)
                {
                    continue;
                }

                Add(values, ruleKey + "/id", rule.Id);
                Add(values, ruleKey + "/trigger",
                    rule.Trigger != null ? rule.Trigger.Type : string.Empty);
                FlattenConditions(values, ruleKey, rule.Conditions);
                FlattenEffects(values, ruleKey, rule.Effects);
            }

            return values;
        }

        private static void FlattenConditions(
            Dictionary<string, string> values,
            string ruleKey,
            RuleCondition[] conditions)
        {
            for (int index = 0; index < conditions.Length; index++)
            {
                RuleCondition condition = conditions[index];
                string key = ruleKey + "/conditions/" + index;
                Add(values, key + "/present", condition != null ? "1" : "0");
                if (condition == null)
                {
                    continue;
                }
                Add(values, key + "/type", condition.Type);
                Add(values, key + "/comparison", condition.Comparison);
                Add(values, key + "/value", condition.Value);
                Add(values, key + "/stringValue", condition.StringValue);
            }
        }

        private static void FlattenEffects(
            Dictionary<string, string> values,
            string ruleKey,
            RuleEffect[] effects)
        {
            for (int index = 0; index < effects.Length; index++)
            {
                RuleEffect effect = effects[index];
                string key = ruleKey + "/effects/" + StableKey(
                    effect != null ? effect.EffectId : string.Empty,
                    index);
                Add(values, key + "/present", effect != null ? "1" : "0");
                if (effect == null)
                {
                    continue;
                }
                Add(values, key + "/effectId", effect.EffectId);
                Add(values, key + "/kind", effect.Kind);
                Add(values, key + "/target", effect.Target);
                Add(values, key + "/statId", effect.StatId);
                Add(values, key + "/operation", effect.Operation);
                Add(values, key + "/value", effect.Value);
                Add(values, key + "/stringValue", effect.StringValue);
                Add(values, key + "/stackMode", effect.StackMode);
                Add(values, key + "/maxStacks", effect.MaxStacks);
                Add(values, key + "/duration", effect.Duration);
                RuleScaling scaling = effect.Scaling;
                Add(values, key + "/scaling/present", scaling != null ? "1" : "0");
                if (scaling == null)
                {
                    continue;
                }
                Add(values, key + "/scaling/source", scaling.Source);
                Add(values, key + "/scaling/mode", scaling.Mode);
                Add(values, key + "/scaling/sourceMin", scaling.SourceMin);
                Add(values, key + "/scaling/sourceMax", scaling.SourceMax);
                Add(values, key + "/scaling/effectMin", scaling.EffectMin);
                Add(values, key + "/scaling/effectMax", scaling.EffectMax);
            }
        }

        private static string StableKey(string identifier, int index)
        {
            return string.IsNullOrWhiteSpace(identifier)
                ? "index-" + index
                : identifier + "@" + index;
        }

        private static void Add(
            Dictionary<string, string> values,
            string path,
            string value)
        {
            values[path] = value ?? string.Empty;
        }

        private static void Add(
            Dictionary<string, string> values,
            string path,
            float value)
        {
            values[path] = value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void Add(
            Dictionary<string, string> values,
            string path,
            int value)
        {
            values[path] = value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
