using System;
using System.Collections.Generic;
using System.Text;
using RuleForge.Config;
using RuleForge.Rules;
using RuleForge.Runtime.Stats;

namespace RuleForge.AI
{
    public static class GameplayVocabulary
    {
        private static readonly string[] SupportedEventNames =
            GameplayEventCapabilities.GetRuntimeEventNames();

        public static string BuildDescription(
            EffectCatalog catalog,
            GameplayBalanceConfig balance)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(
                "Allowed Events: " + string.Join(", ", SupportedEventNames));
            builder.AppendLine("Allowed Conditions: " + JoinEnum<RuleConditionType>());
            builder.AppendLine("Allowed Comparisons: " + JoinEnum<RuleComparison>());
            builder.AppendLine("Allowed Effect Kinds: " + JoinEnum<RuleEffectKind>());
            builder.AppendLine("Allowed Targets: " + JoinEnum<RuntimeServiceTarget>());
            builder.AppendLine("Allowed Stats: " + JoinEnum<RuntimeStatId>());
            builder.AppendLine("Allowed Operations: " + JoinEnum<StatModifierOperation>());
            builder.AppendLine("Allowed Stack Modes: " + JoinEnum<RuleStackMode>());
            builder.AppendLine("Allowed Scaling Modes: " + JoinEnum<RuleScalingMode>());
            builder.AppendLine("Allowed Scaling Sources: " + JoinEnum<RuntimeValueSource>());
            builder.AppendLine("Allowed enemy types: Grunt, Runner, Tank.");
            builder.AppendLine("Allowed Goals: Survive, KillCount, Score.");
            builder.AppendLine(
                "Allowed weapons: Assault Rifle, Shotgun, Sniper.");
            builder.AppendLine("Effect mappings (copy these identity fields exactly):");

            if (catalog != null)
            {
                IReadOnlyList<EffectDefinition> definitions = catalog.Effects;
                for (int index = 0; index < definitions.Count; index++)
                {
                    EffectDefinition definition = definitions[index];
                    if (definition == null)
                    {
                        continue;
                    }

                    builder.Append("- ").Append(definition.EffectId)
                        .Append(": kind=").Append(definition.CreatorKind)
                        .Append(", target=").Append(definition.CreatorTarget)
                        .Append(", statId=").Append(definition.CreatorStatId)
                        .Append(", operation=").Append(definition.CreatorOperation)
                        .Append(", stringValue=").Append(definition.CreatorStringValue)
                        .Append(", polarity=").Append(definition.Polarity)
                        .AppendLine();
                }
            }

            if (balance != null)
            {
                builder.Append("Complexity limits: maxRules=")
                    .Append(balance.MaxRules)
                    .Append(", maxEffectsPerRule=")
                    .Append(balance.MaxEffectsPerRule)
                    .Append(", maxConditionsPerRule=")
                    .Append(balance.MaxConditionsPerRule)
                    .Append(", maxStacks=")
                    .Append(balance.MaxStacks)
                    .AppendLine(".");
                builder.Append("Probability range: ")
                    .Append(balance.MinimumProbability)
                    .Append(" to ")
                    .Append(balance.MaximumProbability)
                    .Append(". Duration range: ")
                    .Append(balance.MinimumDuration)
                    .Append(" to ")
                    .Append(balance.MaximumDuration)
                    .AppendLine(" seconds.");

                IReadOnlyList<EffectValueLimit> limits =
                    balance.EffectValueLimits;
                builder.AppendLine("Effect value limits:");
                for (int index = 0; index < limits.Count; index++)
                {
                    EffectValueLimit limit = limits[index];
                    if (limit != null)
                    {
                        builder.Append("- ").Append(limit.EffectId)
                            .Append(": ").Append(limit.MinimumValue)
                            .Append(" to ").Append(limit.MaximumValue)
                            .AppendLine();
                    }
                }
            }

            builder.AppendLine(
                "RandomChance uses condition.value from 0 to 1. " +
                "EnemyType uses comparison Equals/NotEquals and stringValue " +
                "Grunt, Runner, or Tank.");
            builder.AppendLine(
                "Percent modifiers are decimal fractions: 0.05 means +5%. " +
                "Do not invent vocabulary or return natural-language rules.");
            builder.AppendLine(
                "Duration applies only to StatModifier effects. Use 0 for a " +
                "persistent modifier; positive seconds make it expire. " +
                "Scaled modifiers must use StackMode None.");
            return builder.ToString();
        }

        public static string BuildChallengeSchema(EffectCatalog catalog)
        {
            string ruleSchema = BuildRuleSchema(catalog);
            return "{" +
                   "\"type\":\"object\"," +
                   "\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"id\":{\"type\":\"string\"}," +
                   "\"displayName\":{\"type\":\"string\"}," +
                   "\"goal\":{\"type\":\"string\",\"enum\":[\"Survive\",\"KillCount\",\"Score\"]}," +
                   "\"goalTarget\":{\"type\":\"number\"}," +
                   "\"weapon\":{\"type\":\"string\",\"enum\":[\"Assault Rifle\",\"Shotgun\",\"Sniper\"]}," +
                   "\"rules\":{\"type\":\"array\",\"items\":" + ruleSchema + "}" +
                   "}," +
                   "\"required\":[\"id\",\"displayName\",\"goal\",\"goalTarget\",\"weapon\",\"rules\"]" +
                   "}";
        }

        public static string BuildPatchSchema(EffectCatalog catalog)
        {
            string operationEnum = JsonStringArray(
                Enum.GetNames(typeof(ChallengePatchOperationType)));
            string effectIds = BuildEffectIdArray(catalog, true);
            string nullableRule = "{\"anyOf\":[{\"type\":\"null\"}," +
                                  BuildRuleSchema(catalog) + "]}";
            return "{" +
                   "\"type\":\"object\"," +
                   "\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"operations\":{\"type\":\"array\",\"minItems\":1,\"items\":{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"operation\":{\"type\":\"string\",\"enum\":" + operationEnum + "}," +
                   "\"ruleId\":{\"type\":\"string\"}," +
                   "\"effectId\":{\"type\":\"string\",\"enum\":" + effectIds + "}," +
                   "\"conditionIndex\":{\"type\":\"integer\",\"minimum\":0}," +
                   "\"value\":{\"type\":\"number\"}," +
                   "\"maxStacks\":{\"type\":\"integer\"}," +
                   "\"duration\":{\"type\":\"number\"}," +
                   "\"probability\":{\"type\":\"number\"}," +
                   "\"goal\":{\"type\":\"string\",\"enum\":[\"\",\"Survive\",\"KillCount\",\"Score\"]}," +
                   "\"goalTarget\":{\"type\":\"number\"}," +
                   "\"rule\":" + nullableRule +
                   "}," +
                   "\"required\":[\"operation\",\"ruleId\",\"effectId\",\"conditionIndex\",\"value\",\"maxStacks\",\"duration\",\"probability\",\"goal\",\"goalTarget\",\"rule\"]" +
                   "}}}," +
                   "\"required\":[\"operations\"]" +
                   "}";
        }

        private static string BuildRuleSchema(EffectCatalog catalog)
        {
            string eventNames = JsonStringArray(SupportedEventNames);
            string conditionNames = JsonStringArray(
                Enum.GetNames(typeof(RuleConditionType)));
            string comparisons = JsonStringArrayWithEmpty(
                Enum.GetNames(typeof(RuleComparison)));
            string effectIds = BuildEffectIdArray(catalog, false);
            string effectKinds = JsonStringArray(
                Enum.GetNames(typeof(RuleEffectKind)));
            string targets = JsonStringArray(
                Enum.GetNames(typeof(RuntimeServiceTarget)));
            string stats = JsonStringArrayWithEmpty(
                Enum.GetNames(typeof(RuntimeStatId)));
            string operations = JsonStringArrayWithEmpty(
                Enum.GetNames(typeof(StatModifierOperation)));
            string stackModes = JsonStringArray(
                Enum.GetNames(typeof(RuleStackMode)));
            string scalingModes = JsonStringArray(
                Enum.GetNames(typeof(RuleScalingMode)));
            string scalingSources = JsonStringArray(
                Enum.GetNames(typeof(RuntimeValueSource)));

            string conditionSchema = "{" +
                "\"type\":\"object\",\"additionalProperties\":false," +
                "\"properties\":{" +
                "\"type\":{\"type\":\"string\",\"enum\":" + conditionNames + "}," +
                "\"comparison\":{\"type\":\"string\",\"enum\":" + comparisons + "}," +
                "\"value\":{\"type\":\"number\"}," +
                "\"stringValue\":{\"type\":\"string\"}" +
                "},\"required\":[\"type\",\"comparison\",\"value\",\"stringValue\"]}";

            string scalingSchema = "{" +
                "\"type\":\"object\",\"additionalProperties\":false," +
                "\"properties\":{" +
                "\"source\":{\"type\":\"string\",\"enum\":" + scalingSources + "}," +
                "\"mode\":{\"type\":\"string\",\"enum\":" + scalingModes + "}," +
                "\"sourceMin\":{\"type\":\"number\"}," +
                "\"sourceMax\":{\"type\":\"number\"}," +
                "\"effectMin\":{\"type\":\"number\"}," +
                "\"effectMax\":{\"type\":\"number\"}" +
                "},\"required\":[\"source\",\"mode\",\"sourceMin\",\"sourceMax\",\"effectMin\",\"effectMax\"]}";

            string effectSchema = "{" +
                "\"type\":\"object\",\"additionalProperties\":false," +
                "\"properties\":{" +
                "\"effectId\":{\"type\":\"string\",\"enum\":" + effectIds + "}," +
                "\"kind\":{\"type\":\"string\",\"enum\":" + effectKinds + "}," +
                "\"target\":{\"type\":\"string\",\"enum\":" + targets + "}," +
                "\"statId\":{\"type\":\"string\",\"enum\":" + stats + "}," +
                "\"operation\":{\"type\":\"string\",\"enum\":" + operations + "}," +
                "\"value\":{\"type\":\"number\"}," +
                "\"stringValue\":{\"type\":\"string\"}," +
                "\"stackMode\":{\"type\":\"string\",\"enum\":" + stackModes + "}," +
                "\"maxStacks\":{\"type\":\"integer\"}," +
                "\"duration\":{\"type\":\"number\"}," +
                "\"scaling\":{\"anyOf\":[{\"type\":\"null\"}," + scalingSchema + "]}" +
                "},\"required\":[\"effectId\",\"kind\",\"target\",\"statId\",\"operation\",\"value\",\"stringValue\",\"stackMode\",\"maxStacks\",\"duration\",\"scaling\"]}";

            return "{" +
                "\"type\":\"object\",\"additionalProperties\":false," +
                "\"properties\":{" +
                "\"id\":{\"type\":\"string\"}," +
                "\"trigger\":{\"type\":\"object\",\"additionalProperties\":false,\"properties\":{" +
                "\"type\":{\"type\":\"string\",\"enum\":" + eventNames + "}},\"required\":[\"type\"]}," +
                "\"conditions\":{\"type\":\"array\",\"items\":" + conditionSchema + "}," +
                "\"effects\":{\"type\":\"array\",\"items\":" + effectSchema + "}" +
                "},\"required\":[\"id\",\"trigger\",\"conditions\",\"effects\"]}";
        }

        private static string BuildEffectIdArray(
            EffectCatalog catalog,
            bool includeEmpty)
        {
            List<string> identifiers = new List<string>();
            if (includeEmpty)
            {
                identifiers.Add(string.Empty);
            }
            if (catalog != null)
            {
                IReadOnlyList<EffectDefinition> effects = catalog.Effects;
                for (int index = 0; index < effects.Count; index++)
                {
                    EffectDefinition definition = effects[index];
                    if (definition != null &&
                        !string.IsNullOrWhiteSpace(definition.EffectId))
                    {
                        identifiers.Add(definition.EffectId);
                    }
                }
            }

            if (identifiers.Count == 0)
            {
                identifiers.Add("NoAllowedEffectConfigured");
            }

            return JsonStringArray(identifiers.ToArray());
        }

        private static string JoinEnum<T>() where T : struct
        {
            return string.Join(", ", Enum.GetNames(typeof(T)));
        }

        private static string JsonStringArrayWithEmpty(string[] values)
        {
            string[] combined = new string[values.Length + 1];
            combined[0] = string.Empty;
            Array.Copy(values, 0, combined, 1, values.Length);
            return JsonStringArray(combined);
        }

        private static string JsonStringArray(string[] values)
        {
            StringBuilder builder = new StringBuilder("[");
            for (int index = 0; index < values.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append('"')
                    .Append(EscapeJson(values[index]))
                    .Append('"');
            }

            return builder.Append(']').ToString();
        }

        public static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length + 16);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 32)
                        {
                            builder.Append("\\u")
                                .Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            return builder.ToString();
        }
    }
}
