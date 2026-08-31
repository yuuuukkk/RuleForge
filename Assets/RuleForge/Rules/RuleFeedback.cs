using System;
using System.Collections.Generic;

namespace RuleForge.Rules
{
    public sealed class RuleTriggerFeedback
    {
        public RuleTriggerFeedback(
            string ruleId,
            IReadOnlyList<RuleEffectFeedback> effects)
        {
            RuleId = ruleId ?? string.Empty;
            Effects = effects ?? Array.Empty<RuleEffectFeedback>();
        }

        public string RuleId { get; }
        public IReadOnlyList<RuleEffectFeedback> Effects { get; }
    }

    public sealed class RuleEffectFeedback
    {
        public RuleEffectFeedback(
            string effectId,
            string displayName,
            string kind,
            string operation,
            float value,
            string stringValue,
            int appliedStacks,
            int maxStacks)
        {
            EffectId = effectId ?? string.Empty;
            DisplayName = displayName ?? EffectId;
            Kind = kind ?? string.Empty;
            Operation = operation ?? string.Empty;
            Value = value;
            StringValue = stringValue ?? string.Empty;
            AppliedStacks = appliedStacks;
            MaxStacks = maxStacks;
        }

        public string EffectId { get; }
        public string DisplayName { get; }
        public string Kind { get; }
        public string Operation { get; }
        public float Value { get; }
        public string StringValue { get; }
        public int AppliedStacks { get; }
        public int MaxStacks { get; }
    }
}
