using System;
using System.Collections.Generic;
using System.Globalization;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Rules;
using RuleForge.Runtime.Stats;

namespace RuleForge.UI
{
    internal sealed class RuleDraft
    {
        public RuleDraft(GameplayRule rule, int fallbackNumber)
        {
            Id = string.IsNullOrWhiteSpace(rule.Id)
                ? $"manual_rule_{fallbackNumber:00}"
                : rule.Id;
            string[] triggerOptions =
                GameplayEventCapabilities.GetRuntimeEventNames();
            TriggerType = rule.Trigger != null &&
                          !string.IsNullOrWhiteSpace(rule.Trigger.Type)
                ? rule.Trigger.Type
                : triggerOptions.Length > 0
                    ? triggerOptions[0]
                    : string.Empty;
            RuleCondition[] conditions = rule.Conditions;
            for (int index = 0; index < conditions.Length; index++)
            {
                if (conditions[index] != null)
                {
                    Conditions.Add(new ConditionDraft(conditions[index]));
                }
            }

            RuleEffect[] effects = rule.Effects;
            for (int index = 0; index < effects.Length; index++)
            {
                if (effects[index] != null)
                {
                    Effects.Add(new EffectDraft(effects[index]));
                }
            }
        }

        public RuleDraft(string id, string triggerType)
        {
            Id = id;
            TriggerType = triggerType;
        }

        public string Id;
        public string TriggerType;
        public List<ConditionDraft> Conditions { get; } =
            new List<ConditionDraft>();
        public List<EffectDraft> Effects { get; } =
            new List<EffectDraft>();

        public bool TryBuild(out GameplayRule rule, out string error)
        {
            RuleCondition[] conditions = new RuleCondition[Conditions.Count];
            for (int index = 0; index < Conditions.Count; index++)
            {
                if (!Conditions[index].TryBuild(
                        out conditions[index],
                        out error))
                {
                    rule = null;
                    return false;
                }
            }

            RuleEffect[] effects = new RuleEffect[Effects.Count];
            for (int index = 0; index < Effects.Count; index++)
            {
                if (!Effects[index].TryBuild(out effects[index], out error))
                {
                    rule = null;
                    return false;
                }
            }

            rule = GameplayRule.Create(Id, TriggerType, conditions, effects);
            error = string.Empty;
            return true;
        }
    }

    internal sealed class ConditionDraft
    {
        public ConditionDraft()
        {
            Type = RuleConditionType.Always.ToString();
            Comparison = RuleComparison.Equals.ToString();
            ValueText = "0";
            StringValue = string.Empty;
        }

        public ConditionDraft(RuleCondition condition)
        {
            Type = string.IsNullOrWhiteSpace(condition.Type)
                ? RuleConditionType.Always.ToString()
                : condition.Type;
            Comparison = condition.Comparison;
            ValueText = string.Equals(
                    Type,
                    RuleConditionType.RandomChance.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                ? (condition.Value * 100f).ToString(
                    "0.###", CultureInfo.InvariantCulture)
                : condition.Value.ToString(
                    "0.###", CultureInfo.InvariantCulture);
            StringValue = condition.StringValue;
        }

        public string Type;
        public string Comparison;
        public string ValueText;
        public string StringValue;

        public void SetType(string type)
        {
            Type = type;
            bool usesComparison =
                string.Equals(
                    Type,
                    RuleConditionType.EnemyType.ToString(),
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    Type,
                    RuleConditionType.EventValue.ToString(),
                    StringComparison.OrdinalIgnoreCase);
            if (!usesComparison)
            {
                Comparison = string.Empty;
                StringValue = string.Empty;
                return;
            }

            if (string.IsNullOrWhiteSpace(Comparison))
            {
                Comparison = RuleComparison.Equals.ToString();
            }

            if (string.Equals(
                    Type,
                    RuleConditionType.EnemyType.ToString(),
                    StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(StringValue))
            {
                StringValue = "Grunt";
            }
            else if (string.Equals(
                         Type,
                         RuleConditionType.EventValue.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                StringValue = string.Empty;
            }
        }

        public bool TryBuild(out RuleCondition condition, out string error)
        {
            float value = 0f;
            if (!string.Equals(
                    Type,
                    RuleConditionType.Always.ToString(),
                    StringComparison.OrdinalIgnoreCase) &&
                !ChallengeCreatorDraftParser.TryParseFloat(
                    ValueText, out value))
            {
                condition = null;
                error = RuleForgeLocalization.T(
                    $"Invalid condition value in {Type}.",
                    $"条件 {RuleForgeLocalization.DataValue(Type)} 的数值无效。");
                return false;
            }

            if (string.Equals(
                    Type,
                    RuleConditionType.RandomChance.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                value /= 100f;
            }

            condition = RuleCondition.Create(
                Type,
                string.Equals(
                        Type,
                        RuleConditionType.Always.ToString(),
                        StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : Comparison,
                value,
                StringValue);
            error = string.Empty;
            return true;
        }
    }

    internal sealed class EffectDraft
    {
        private string kind;
        private string target;
        private string statId;
        private string operation;
        private string stringValue;
        private string stackMode;
        private float duration;
        private bool hasScaling;
        private string scalingSource;
        private string scalingMode;
        private float scalingSourceMin;
        private float scalingSourceMax;

        public EffectDraft(RuleEffect effect)
        {
            EffectId = effect.EffectId;
            kind = effect.Kind;
            target = effect.Target;
            statId = effect.StatId;
            operation = effect.Operation;
            stringValue = effect.StringValue;
            stackMode = effect.StackMode;
            duration = effect.Duration;
            ValueText = FormatValue(effect.Value, IsPercent);
            MaxStacksText = effect.MaxStacks.ToString(
                CultureInfo.InvariantCulture);
            DurationText = duration.ToString(
                "0.###",
                CultureInfo.InvariantCulture);

            RuleScaling scaling = effect.Scaling;
            hasScaling = scaling != null &&
                         !string.IsNullOrWhiteSpace(scaling.Source);
            if (hasScaling)
            {
                scalingSource = scaling.Source;
                scalingMode = scaling.Mode;
                scalingSourceMin = scaling.SourceMin;
                scalingSourceMax = scaling.SourceMax;
                ScalingSourceMinText = FormatValue(scaling.SourceMin, false);
                ScalingSourceMaxText = FormatValue(scaling.SourceMax, false);
                ScalingMinText = FormatValue(scaling.EffectMin, IsPercent);
                ScalingMaxText = FormatValue(scaling.EffectMax, IsPercent);
            }
        }

        public EffectDraft(EffectDefinition definition)
        {
            ApplyTemplate(definition);
        }

        public string EffectId { get; private set; }
        public bool IsPercent => string.Equals(
            operation,
            StatModifierOperation.AddPercent.ToString(),
            StringComparison.OrdinalIgnoreCase);
        public bool HasScaling => hasScaling;
        public bool IsStatModifier => string.Equals(
            kind, RuleEffectKind.StatModifier.ToString(),
            StringComparison.OrdinalIgnoreCase);
        public bool IsSpawnEnemy => string.Equals(
            kind, RuleEffectKind.SpawnEnemy.ToString(),
            StringComparison.OrdinalIgnoreCase);
        public string StackMode => stackMode;
        public string ScalingSource => scalingSource;
        public string ValueText = "0";
        public string MaxStacksText = "1";
        public string DurationText = "0";
        public string ScalingMinText = "0";
        public string ScalingMaxText = "0";
        public string ScalingSourceMinText = "0";
        public string ScalingSourceMaxText = "1";

        public void SetStackMode(string mode)
        {
            stackMode = hasScaling ? RuleStackMode.None.ToString() : mode;
            if (string.Equals(stackMode, RuleStackMode.None.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                MaxStacksText = "1";
            }
        }

        public void SetScalingEnabled(bool enabled)
        {
            if (!IsStatModifier)
            {
                return;
            }

            hasScaling = enabled;
            if (enabled)
            {
                stackMode = RuleStackMode.None.ToString();
                MaxStacksText = "1";
                scalingSource = RuntimeValueSource.PlayerMissingHPPercent.ToString();
                scalingMode = RuleScalingMode.Linear.ToString();
                ScalingSourceMinText = "0";
                ScalingSourceMaxText = "1";
                ScalingMinText = ValueText;
                ScalingMaxText = ValueText;
            }
        }

        public void SetScalingSource(string source)
        {
            scalingSource = source;
        }

        public void ApplyTemplate(EffectDefinition definition)
        {
            EffectId = definition.EffectId;
            kind = definition.CreatorKind;
            target = definition.CreatorTarget;
            statId = definition.CreatorStatId;
            operation = definition.CreatorOperation;
            stringValue = definition.CreatorStringValue;
            stackMode = definition.CreatorStackMode;
            duration = 0f;
            hasScaling = false;
            scalingSource = string.Empty;
            scalingMode = string.Empty;
            scalingSourceMin = 0f;
            scalingSourceMax = 1f;
            ScalingMinText = "0";
            ScalingMaxText = "0";
            ScalingSourceMinText = "0";
            ScalingSourceMaxText = "1";
            ValueText = FormatValue(
                definition.CreatorDefaultValue,
                IsPercent);
            MaxStacksText = definition.CreatorMaxStacks.ToString(
                CultureInfo.InvariantCulture);
            DurationText = "0";
        }

        public bool TryBuild(out RuleEffect effect, out string error)
        {
            if (!ChallengeCreatorDraftParser.TryParseFloat(
                    ValueText, out float value) ||
                !ChallengeCreatorDraftParser.TryParseFloat(
                    DurationText, out float parsedDuration) ||
                !int.TryParse(
                    MaxStacksText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int maxStacks))
            {
                effect = null;
                error = RuleForgeLocalization.T(
                    $"Invalid Value, Max Stack, or Duration in '{EffectId}'.",
                    $"效果“{RuleForgeLocalization.EffectName(EffectId, EffectId)}”的数值、最大层数或持续时间无效。");
                return false;
            }

            if (IsPercent)
            {
                value /= 100f;
            }

            RuleScaling scaling = null;
            if (hasScaling)
            {
                if (!ChallengeCreatorDraftParser.TryParseFloat(
                        ScalingMinText, out float scalingMin) ||
                    !ChallengeCreatorDraftParser.TryParseFloat(
                        ScalingMaxText, out float scalingMax) ||
                    !ChallengeCreatorDraftParser.TryParseFloat(
                        ScalingSourceMinText, out scalingSourceMin) ||
                    !ChallengeCreatorDraftParser.TryParseFloat(
                        ScalingSourceMaxText, out scalingSourceMax))
                {
                    effect = null;
                    error = RuleForgeLocalization.T(
                        $"Invalid scaling values in '{EffectId}'.",
                        $"效果“{RuleForgeLocalization.EffectName(EffectId, EffectId)}”的缩放数值无效。");
                    return false;
                }

                scaling = RuleScaling.Create(
                    scalingSource,
                    scalingMode,
                    scalingSourceMin,
                    scalingSourceMax,
                    IsPercent ? scalingMin / 100f : scalingMin,
                    IsPercent ? scalingMax / 100f : scalingMax);
            }

            effect = RuleEffect.Create(
                EffectId,
                kind,
                target,
                statId,
                operation,
                value,
                stringValue,
                stackMode,
                maxStacks,
                parsedDuration,
                scaling);
            error = string.Empty;
            return true;
        }

        private static string FormatValue(float value, bool percent)
        {
            float displayed = percent ? value * 100f : value;
            return displayed.ToString(
                "0.###", CultureInfo.InvariantCulture);
        }
    }

    internal static class ChallengeCreatorDraftParser
    {
        public static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(
                       text,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out value) ||
                   float.TryParse(
                       text,
                       NumberStyles.Float,
                       CultureInfo.CurrentCulture,
                       out value);
        }
    }
}
