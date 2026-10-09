using System;
using System.Collections.Generic;
using System.Text;
using RuleForge.Config;
using RuleForge.DSL;
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
            builder.AppendLine("Runtime stat names (NOT independently available as " +
                "effects): " + JoinEnum<RuntimeStatId>());
            builder.AppendLine("Operations (only when matched by an Effect " +
                "mapping below): " + JoinEnum<StatModifierOperation>());
            builder.AppendLine("Allowed Stack Modes: " + JoinEnum<RuleStackMode>());
            builder.AppendLine("Allowed Scaling Modes: " + JoinEnum<RuleScalingMode>());
            builder.AppendLine("Allowed Scaling Sources: " + JoinEnum<RuntimeValueSource>());
            builder.AppendLine("Allowed enemy types: Grunt, Runner, Tank.");
            builder.AppendLine("Allowed Goals: Survive, KillCount, Score, " +
                "TimeBankTarget, TimeBankSurvive, TimeBankEndless. " +
                "timeLimit=0 means no deadline. KillCount or Score may use " +
                "timeLimit from 1 to 300 seconds; reaching zero before the " +
                "goal is defeat. Survive must use timeLimit=0.");
            builder.AppendLine("TimeBank goals use timeLimit as starting time " +
                "and timeDamageScale (0.01 to 1 seconds per damage point) so " +
                "enemy hits remove time instead of HP. AddTime on EnemyKilled " +
                "extends the remaining time. TimeBankTarget wins when the " +
                "remaining bank reaches goalTarget; TimeBankSurvive wins after " +
                "goalTarget elapsed seconds; TimeBankEndless has goalTarget=0 " +
                "and ends only when time reaches zero. Do not invent a kill " +
                "target or extra risk for a time-as-health request. Ask the " +
                "player to choose one of these three endings if unclear.");
            builder.AppendLine(
                "Allowed weapons: Assault Rifle, Shotgun, Sniper.");
            builder.AppendLine("Effect mappings (copy these identity fields exactly):");
            builder.AppendLine("Only listed effectIds are playable. An enum " +
                "name alone does not authorize a new effect or identity. " +
                "Numbers, event, conditions, stacks, duration and scaling " +
                "are editable only within Validator limits.");

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
                        .Append(", defaultValue=")
                        .Append(definition.CreatorDefaultValue);
                    if (balance != null &&
                        balance.TryGetEffectValueLimit(
                            definition.EffectId, out EffectValueLimit valueLimit))
                    {
                        builder.Append(", allowedValue=")
                            .Append(valueLimit.MinimumValue)
                            .Append("..")
                            .Append(valueLimit.MaximumValue);
                    }
                    builder.AppendLine();
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
                            .Append(" to ").Append(limit.MaximumValue);
                        if (limit.MaximumStackedMagnitude > 0f)
                        {
                            builder.Append(", maximum absolute total at full " +
                                           "stacks=")
                                .Append(limit.MaximumStackedMagnitude);
                        }

                        builder.AppendLine();
                    }
                }
            }

            builder.AppendLine(
                "RandomChance uses condition.value from 0 to 1. " +
                "EnemyType uses comparison Equals/NotEquals and stringValue " +
                "Grunt, Runner, or Tank.");
            builder.AppendLine(
                "EventValue compares condition.value with the trigger's numeric " +
                "value using Equals, NotEquals, LessThan, LessOrEqual, " +
                "GreaterThan, or GreaterOrEqual. PlayerHPChanged and " +
                "PlayerAmmoChanged values are current 0-to-1 percentages; " +
                "EnemyHit, Headshot, PlayerHit, and WeaponFired values are damage. " +
                "EnemyKilled publishes no numeric value. Do not use EventValue " +
                "conditions or EventValue scaling on EnemyKilled. To grow on " +
                "every kill, use one EnemyKilled rule per effect with no numeric " +
                "condition, Stack mode, and the requested maxStacks. A stack " +
                "limit is not a kill-count milestone or victory target.");
            builder.AppendLine(
                "Percent modifiers are decimal fractions: 0.05 means +5%. " +
                "Do not invent vocabulary or return natural-language rules.");
            builder.AppendLine(
                "Duration applies only to StatModifier effects. Use 0 for a " +
                "persistent modifier; positive seconds make it expire. " +
                "Scaled modifiers must use StackMode None.");
            return builder.ToString();
        }

        public static string BuildDesignGuidance()
        {
            return
                "Translate the player's design intent into mechanics, not just a " +
                "theme or renamed copy of the current challenge. Select triggers, " +
                "conditions, effects, stacking, probability, duration, and scaling " +
                "because they express the requested play pattern. High-risk/high-" +
                "reward should contain both a meaningful reward and a clearly " +
                "stronger or faster-growing risk. Conservative designs should use " +
                "lower magnitudes, bounded stacks, or reliable conditions. Crazy " +
                "or random designs should use controlled probability and visible " +
                "consequences. Continuous growth should use Stack with a finite " +
                "maxStacks, and absolute value multiplied by maxStacks must stay " +
                "inside the catalog's full-stack safety limit. Short bursts " +
                "should use positive Duration instead of " +
                "permanent stacking. State-dependent designs should use Scaling " +
                "with the appropriate runtime source and StackMode None. Rule-loop " +
                "designs may use multiple generic rules whose events and effects " +
                "feed the player's next decision. Do not default every request to " +
                "EnemyKilled + PlayerDamage + EnemyMoveSpeed. Prefer the smallest " +
                "set of rules that makes the requested loop legible. When the user " +
                "specifies a numeric relationship, calculate final parameter values " +
                "so the requested ratio is actually represented. " +
                "Treat an explicit survival duration as Survive with that goalTarget " +
                "and timeLimit=0, not as a timed KillCount goal. Never add an " +
                "unrequested kill objective. For per-kill stacks, use the effect's " +
                "Stack/maxStacks; never interpret kill number as EventValue. " +
                "Before returning, silently verify all identity mappings, numeric ratios, limits, " +
                "stack/scaling compatibility, and that the output is materially " +
                "responsive to this prompt. Never use a fixed prompt template or " +
                "keyword-to-challenge lookup.";
        }

        public static string BuildStructureSummary(ChallengeSpec challenge)
        {
            if (challenge == null)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            GameplayRule[] rules = challenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(rule.Trigger != null
                        ? rule.Trigger.Type
                        : "NoTrigger")
                    .Append("[");
                RuleCondition[] conditions = rule.Conditions;
                for (int conditionIndex = 0;
                     conditionIndex < conditions.Length;
                     conditionIndex++)
                {
                    if (conditionIndex > 0)
                    {
                        builder.Append('+');
                    }

                    builder.Append(conditions[conditionIndex] != null
                        ? conditions[conditionIndex].Type
                        : "null");
                }

                builder.Append("]=>");
                RuleEffect[] effects = rule.Effects;
                for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                {
                    if (effectIndex > 0)
                    {
                        builder.Append('+');
                    }

                    RuleEffect effect = effects[effectIndex];
                    if (effect == null)
                    {
                        builder.Append("null");
                        continue;
                    }

                    builder.Append(effect.EffectId)
                        .Append('{')
                        .Append(effect.StackMode)
                        .Append(",p=")
                        .Append(HasRandomChance(conditions) ? "yes" : "no")
                        .Append(",scale=")
                        .Append(effect.Scaling != null ? "yes" : "no")
                        .Append(",duration=")
                        .Append(effect.Duration > 0f ? "yes" : "no")
                        .Append('}');
                }
            }

            return builder.Length > 0 ? builder.ToString() : "empty";
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
                   "\"goal\":{\"type\":\"string\",\"enum\":[\"Survive\",\"KillCount\",\"Score\",\"TimeBankTarget\",\"TimeBankSurvive\",\"TimeBankEndless\"]}," +
                   "\"goalTarget\":{\"type\":\"number\"}," +
                   "\"timeLimit\":{\"type\":\"number\"}," +
                   "\"timeDamageScale\":{\"type\":\"number\"}," +
                   "\"weapon\":{\"type\":\"string\",\"enum\":[\"Assault Rifle\",\"Shotgun\",\"Sniper\"]}," +
                   "\"rules\":{\"type\":\"array\",\"items\":" + ruleSchema + "}" +
                   "}," +
                   "\"required\":[\"id\",\"displayName\",\"goal\",\"goalTarget\",\"timeLimit\",\"timeDamageScale\",\"weapon\",\"rules\"]" +
                   "}";
        }

        public static string BuildProposalSchema()
        {
            return "{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"summary\":{\"type\":\"string\"}," +
                   "\"detectedIntent\":{\"type\":\"string\"}," +
                   "\"confidence\":{\"type\":\"string\",\"enum\":[\"Low\",\"Medium\",\"High\"]}," +
                   "\"goalExplicit\":{\"type\":\"boolean\"}," +
                   "\"triggerExplicit\":{\"type\":\"boolean\"}," +
                   "\"rewardExplicit\":{\"type\":\"boolean\"}," +
                   "\"riskExplicit\":{\"type\":\"boolean\"}," +
                   "\"scalingExplicit\":{\"type\":\"boolean\"}," +
                   "\"limitExplicit\":{\"type\":\"boolean\"}," +
                   "\"hasConflict\":{\"type\":\"boolean\"}," +
                   "\"withinVocabulary\":{\"type\":\"boolean\"}," +
                   "\"suggestedGoal\":{\"type\":\"string\"}," +
                   "\"suggestedRules\":{\"type\":\"array\",\"maxItems\":6,\"items\":{\"type\":\"string\"}}," +
                   "\"requiredGoalType\":{\"type\":\"string\",\"enum\":[\"\",\"Survive\",\"KillCount\",\"Score\",\"TimeBankTarget\",\"TimeBankSurvive\",\"TimeBankEndless\"]}," +
                   "\"requiredGoalTarget\":{\"type\":\"number\",\"minimum\":0}," +
                   "\"requiredTimeLimitSeconds\":{\"type\":\"number\",\"minimum\":0}," +
                   "\"requiredTimeDamageScale\":{\"type\":\"number\",\"minimum\":0}," +
                   "\"requiredMechanics\":{\"type\":\"array\",\"maxItems\":6,\"items\":{\"type\":\"object\",\"additionalProperties\":false,\"properties\":{\"trigger\":{\"type\":\"string\"},\"effectId\":{\"type\":\"string\"},\"value\":{\"type\":\"number\"},\"exactValue\":{\"type\":\"boolean\"},\"maxStacks\":{\"type\":\"integer\",\"minimum\":0},\"exactMaxStacks\":{\"type\":\"boolean\"}},\"required\":[\"trigger\",\"effectId\",\"value\",\"exactValue\",\"maxStacks\",\"exactMaxStacks\"]}}," +
                   "\"designReasoningSummary\":{\"type\":\"string\"}," +
                   "\"warnings\":{\"type\":\"array\",\"maxItems\":3,\"items\":{\"type\":\"string\"}}," +
                   "\"clarificationQuestion\":{\"type\":\"string\"}," +
                   "\"canGenerate\":{\"type\":\"boolean\"}," +
                   "\"penaltyRewardRatio\":{\"type\":\"number\",\"minimum\":0,\"maximum\":3}," +
                   "\"actionSuggestions\":{\"type\":\"array\",\"maxItems\":3,\"items\":{\"type\":\"string\"}}" +
                   "},\"required\":[\"summary\",\"detectedIntent\",\"confidence\",\"goalExplicit\",\"triggerExplicit\",\"rewardExplicit\",\"riskExplicit\",\"scalingExplicit\",\"limitExplicit\",\"hasConflict\",\"withinVocabulary\",\"suggestedGoal\",\"suggestedRules\",\"requiredGoalType\",\"requiredGoalTarget\",\"requiredTimeLimitSeconds\",\"requiredTimeDamageScale\",\"requiredMechanics\",\"designReasoningSummary\",\"warnings\",\"clarificationQuestion\",\"canGenerate\",\"penaltyRewardRatio\",\"actionSuggestions\"]}";
        }

        public static string BuildModificationProposalSchema()
        {
            return "{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"summary\":{\"type\":\"string\"}," +
                   "\"detectedIntent\":{\"type\":\"string\"}," +
                   "\"confidence\":{\"type\":\"string\",\"enum\":[\"Low\",\"Medium\",\"High\"]}," +
                   "\"parameterFocus\":{\"type\":\"array\",\"maxItems\":5,\"items\":{\"type\":\"string\"}}," +
                   "\"proposedChanges\":{\"type\":\"array\",\"maxItems\":5,\"items\":{\"type\":\"string\"}}," +
                   "\"designReasoningSummary\":{\"type\":\"string\"}," +
                   "\"warnings\":{\"type\":\"array\",\"maxItems\":3,\"items\":{\"type\":\"string\"}}," +
                   "\"clarificationQuestion\":{\"type\":\"string\"}," +
                   "\"canModify\":{\"type\":\"boolean\"}," +
                   "\"patchInstruction\":{\"type\":\"string\"}" +
                   "},\"required\":[\"summary\",\"detectedIntent\",\"confidence\",\"parameterFocus\",\"proposedChanges\",\"designReasoningSummary\",\"warnings\",\"clarificationQuestion\",\"canModify\",\"patchInstruction\"]}";
        }

        public static string BuildRepairSchema(EffectCatalog catalog)
        {
            return "{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"summary\":{\"type\":\"string\"}," +
                   "\"changes\":{\"type\":\"array\",\"maxItems\":8,\"items\":{\"type\":\"string\"}}," +
                   "\"designReasoningSummary\":{\"type\":\"string\"}," +
                   "\"repairedChallenge\":" + BuildChallengeSchema(catalog) +
                   "},\"required\":[\"summary\",\"changes\",\"designReasoningSummary\",\"repairedChallenge\"]}";
        }

        public static string BuildImprovementSchema()
        {
            return "{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"summary\":{\"type\":\"string\"}," +
                   "\"suggestions\":{\"type\":\"array\",\"minItems\":1,\"maxItems\":3,\"items\":{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"title\":{\"type\":\"string\"}," +
                   "\"intent\":{\"type\":\"string\"}," +
                   "\"reasoning\":{\"type\":\"string\"}" +
                   "},\"required\":[\"title\",\"intent\",\"reasoning\"]}}" +
                   "},\"required\":[\"summary\",\"suggestions\"]}";
        }

        public static string BuildPatchSchema(EffectCatalog catalog)
        {
            string operationEnum = JsonStringArray(
                Enum.GetNames(typeof(ChallengePatchOperationType)));
            string effectIds = BuildEffectIdArray(catalog, true);
            string nullableRule = "{\"anyOf\":[{\"type\":\"null\"}," +
                                  BuildRuleSchema(catalog) + "]}";
            string nullableCondition = "{\"anyOf\":[{\"type\":\"null\"}," +
                                       BuildConditionSchema() + "]}";
            string nullableScaling = "{\"anyOf\":[{\"type\":\"null\"}," +
                                     BuildScalingSchema() + "]}";
            string nullableEffect = "{\"anyOf\":[{\"type\":\"null\"}," +
                                    BuildEffectSchema(catalog) + "]}";
            string eventNamesWithEmpty = JsonStringArrayWithEmpty(
                SupportedEventNames);
            return "{" +
                   "\"type\":\"object\"," +
                   "\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"operations\":{\"type\":\"array\",\"minItems\":1,\"maxItems\":8,\"items\":{" +
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
                   "\"goal\":{\"type\":\"string\",\"enum\":[\"\",\"Survive\",\"KillCount\",\"Score\",\"TimeBankTarget\",\"TimeBankSurvive\",\"TimeBankEndless\"]}," +
                   "\"goalTarget\":{\"type\":\"number\"}," +
                   "\"timeLimit\":{\"type\":\"number\"}," +
                   "\"timeDamageScale\":{\"type\":\"number\"}," +
                   "\"weapon\":{\"type\":\"string\",\"enum\":[\"\",\"Assault Rifle\",\"Shotgun\",\"Sniper\"]}," +
                   "\"trigger\":{\"type\":\"string\",\"enum\":" + eventNamesWithEmpty + "}," +
                   "\"condition\":" + nullableCondition + "," +
                   "\"effect\":" + nullableEffect + "," +
                   "\"scaling\":" + nullableScaling + "," +
                   "\"rule\":" + nullableRule +
                   "}," +
                   "\"required\":[\"operation\",\"ruleId\",\"effectId\",\"conditionIndex\",\"value\",\"maxStacks\",\"duration\",\"probability\",\"goal\",\"goalTarget\",\"timeLimit\",\"timeDamageScale\",\"weapon\",\"trigger\",\"condition\",\"effect\",\"scaling\",\"rule\"]" +
                   "}}}," +
                   "\"required\":[\"operations\"]" +
                   "}";
        }

        private static string BuildRuleSchema(EffectCatalog catalog)
        {
            string eventNames = JsonStringArray(SupportedEventNames);
            string conditionSchema = BuildConditionSchema();
            string effectSchema = BuildEffectSchema(catalog);

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

        private static string BuildEffectSchema(EffectCatalog catalog)
        {
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
            string scalingSchema = BuildScalingSchema();
            return "{" +
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
        }

        private static string BuildConditionSchema()
        {
            string conditionNames = JsonStringArray(
                Enum.GetNames(typeof(RuleConditionType)));
            string comparisons = JsonStringArrayWithEmpty(
                Enum.GetNames(typeof(RuleComparison)));
            return "{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"type\":{\"type\":\"string\",\"enum\":" + conditionNames + "}," +
                   "\"comparison\":{\"type\":\"string\",\"enum\":" + comparisons + "}," +
                   "\"value\":{\"type\":\"number\"}," +
                   "\"stringValue\":{\"type\":\"string\"}" +
                   "},\"required\":[\"type\",\"comparison\",\"value\",\"stringValue\"]}";
        }

        private static string BuildScalingSchema()
        {
            string scalingModes = JsonStringArray(
                Enum.GetNames(typeof(RuleScalingMode)));
            string scalingSources = JsonStringArray(
                Enum.GetNames(typeof(RuntimeValueSource)));
            return "{" +
                   "\"type\":\"object\",\"additionalProperties\":false," +
                   "\"properties\":{" +
                   "\"source\":{\"type\":\"string\",\"enum\":" + scalingSources + "}," +
                   "\"mode\":{\"type\":\"string\",\"enum\":" + scalingModes + "}," +
                   "\"sourceMin\":{\"type\":\"number\"}," +
                   "\"sourceMax\":{\"type\":\"number\"}," +
                   "\"effectMin\":{\"type\":\"number\"}," +
                   "\"effectMax\":{\"type\":\"number\"}" +
                   "},\"required\":[\"source\",\"mode\",\"sourceMin\",\"sourceMax\",\"effectMin\",\"effectMax\"]}";
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

        private static bool HasRandomChance(RuleCondition[] conditions)
        {
            for (int index = 0; index < conditions.Length; index++)
            {
                if (conditions[index] != null &&
                    string.Equals(
                        conditions[index].Type,
                        RuleConditionType.RandomChance.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
