using System;
using System.Collections.Generic;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Rules;
using RuleForge.Runtime.Stats;

namespace RuleForge.Validation
{
    public sealed class ChallengeValidator
    {
        private readonly GameplayBalanceConfig balanceConfig;
        private readonly EffectCatalog effectCatalog;
        private readonly SchemaValidator schemaValidator = new SchemaValidator();
        private readonly SemanticValidator semanticValidator = new SemanticValidator();
        private readonly RangeValidator rangeValidator = new RangeValidator();
        private readonly ComplexityValidator complexityValidator =
            new ComplexityValidator();

        public ChallengeValidator(
            GameplayBalanceConfig balance,
            EffectCatalog catalog)
        {
            balanceConfig = balance;
            effectCatalog = catalog;
        }

        public ValidationResult Validate(ChallengeSpec challenge)
        {
            ValidationResult result = new ValidationResult();
            schemaValidator.Validate(challenge, result);

            if (balanceConfig == null)
            {
                result.AddError(
                    "Validation requires a trusted GameplayBalanceConfig.");
            }
            else
            {
                rangeValidator.Validate(challenge, balanceConfig, result);
                complexityValidator.Validate(challenge, balanceConfig, result);
            }

            if (effectCatalog == null)
            {
                result.AddError("Validation requires a trusted EffectCatalog.");
            }
            else
            {
                semanticValidator.Validate(challenge, effectCatalog, result);
            }

            return result;
        }
    }

    public sealed class SchemaValidator
    {
        public void Validate(ChallengeSpec challenge, ValidationResult result)
        {
            if (challenge == null)
            {
                result.AddError("ChallengeSpec is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(challenge.Id))
            {
                result.AddError("ChallengeSpec.id is required.");
            }

            if (string.IsNullOrWhiteSpace(challenge.DisplayName))
            {
                result.AddError("ChallengeSpec.displayName is required.");
            }

            bool recognizedGoal = Enum.TryParse(
                challenge.Goal, true, out ChallengeGoalType goalType);
            bool timeBankGoal = recognizedGoal &&
                (goalType == ChallengeGoalType.TimeBankTarget ||
                 goalType == ChallengeGoalType.TimeBankSurvive ||
                 goalType == ChallengeGoalType.TimeBankEndless);
            if (string.IsNullOrWhiteSpace(challenge.Goal))
            {
                result.AddError("ChallengeSpec.goal is required.");
            }
            else if (!recognizedGoal)
            {
                result.AddError(
                    $"ChallengeSpec.goal '{challenge.Goal}' is unknown.");
            }

            if (float.IsNaN(challenge.GoalTarget) ||
                float.IsInfinity(challenge.GoalTarget) ||
                (goalType == ChallengeGoalType.TimeBankEndless
                    ? challenge.GoalTarget != 0f
                    : challenge.GoalTarget <= 0f))
            {
                result.AddError(
                    "ChallengeSpec.goalTarget must be positive, or 0 for TimeBankEndless.");
            }
            if (goalType == ChallengeGoalType.TimeBankTarget &&
                challenge.GoalTarget > 300f)
            {
                result.AddError(
                    "TimeBankTarget cannot exceed the 300-second time-bank cap.");
            }
            if (goalType == ChallengeGoalType.TimeBankTarget &&
                challenge.GoalTarget <= challenge.TimeLimit)
            {
                result.AddError(
                    "TimeBankTarget must exceed the starting time bank, or victory is immediate.");
            }

            if (float.IsNaN(challenge.TimeLimit) ||
                float.IsInfinity(challenge.TimeLimit) ||
                challenge.TimeLimit < 0f ||
                challenge.TimeLimit > 300f)
            {
                result.AddError(
                    "ChallengeSpec.timeLimit must be 0 or between 1 and 300 seconds.");
            }
            else if (challenge.TimeLimit > 0f &&
                     (challenge.TimeLimit < 1f ||
                      goalType == ChallengeGoalType.Survive))
            {
                result.AddError(
                    "ChallengeSpec.timeLimit requires a timed goal and at least 1 second.");
            }
            if (timeBankGoal && challenge.TimeLimit < 1f)
            {
                result.AddError(
                    "TimeBank goals require at least 1 second of starting time.");
            }
            if (float.IsNaN(challenge.TimeDamageScale) ||
                float.IsInfinity(challenge.TimeDamageScale) ||
                (timeBankGoal &&
                 (challenge.TimeDamageScale < 0.01f ||
                  challenge.TimeDamageScale > 1f)) ||
                (!timeBankGoal && challenge.TimeDamageScale != 0f))
            {
                result.AddError(
                    "ChallengeSpec.timeDamageScale must be 0.01–1 for TimeBank, or 0 for other goals.");
            }

            if (string.IsNullOrWhiteSpace(challenge.Weapon))
            {
                result.AddError("ChallengeSpec.weapon is required.");
            }
            else if (!IsSupportedWeapon(challenge.Weapon))
            {
                result.AddError(
                    $"ChallengeSpec.weapon '{challenge.Weapon}' is unknown.");
            }

            GameplayRule[] rules = challenge.Rules;
            if (rules.Length == 0)
            {
                result.AddError("ChallengeSpec must contain at least one rule.");
                return;
            }

            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                string rulePath = $"rules[{ruleIndex}]";
                if (rule == null)
                {
                    result.AddError($"{rulePath} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rule.Id))
                {
                    result.AddError($"{rulePath}.id is required.");
                }

                if (rule.Trigger == null ||
                    string.IsNullOrWhiteSpace(rule.Trigger.Type))
                {
                    result.AddError($"{rulePath}.trigger.type is required.");
                }

                RuleEffect[] effects = rule.Effects;
                if (effects.Length == 0)
                {
                    result.AddError($"{rulePath} must contain at least one effect.");
                    continue;
                }

                for (int effectIndex = 0;
                     effectIndex < effects.Length;
                     effectIndex++)
                {
                    RuleEffect effect = effects[effectIndex];
                    string effectPath =
                        $"{rulePath}.effects[{effectIndex}]";
                    if (effect == null)
                    {
                        result.AddError($"{effectPath} is null.");
                    }
                    else if (string.IsNullOrWhiteSpace(effect.EffectId))
                    {
                        result.AddError($"{effectPath}.effectId is required.");
                    }
                }
            }
        }

        private static bool IsSupportedWeapon(string weapon)
        {
            return string.Equals(
                       weapon,
                       "Assault Rifle",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       weapon,
                       "Shotgun",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       weapon,
                       "Sniper",
                       StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class SemanticValidator
    {
        public void Validate(
            ChallengeSpec challenge,
            EffectCatalog catalog,
            ValidationResult result)
        {
            if (challenge == null)
            {
                return;
            }

            HashSet<string> ruleIds =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            GameplayRule[] rules = challenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null)
                {
                    continue;
                }

                string rulePath = $"rules[{ruleIndex}]";
                if (!string.IsNullOrWhiteSpace(rule.Id) &&
                    !ruleIds.Add(rule.Id))
                {
                    result.AddError($"{rulePath}.id '{rule.Id}' is duplicated.");
                }

                bool hasKnownTrigger = TryParseTrigger(rule, rulePath, result,
                    out GameplayEventType triggerType);
                ValidateConditions(
                    rule,
                    rulePath,
                    hasKnownTrigger,
                    triggerType,
                    result);
                ValidateEffects(rule, rulePath, catalog, challenge, result);
                if (hasKnownTrigger && !ProvidesNumericValue(triggerType))
                {
                    RuleEffect[] effects = rule.Effects;
                    for (int effectIndex = 0;
                         effectIndex < effects.Length;
                         effectIndex++)
                    {
                        RuleScaling scaling = effects[effectIndex]?.Scaling;
                        if (scaling != null && string.Equals(
                                scaling.Source, "EventValue",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            result.AddError(
                                $"{rulePath}.effects[{effectIndex}].scaling.source EventValue requires a numeric trigger.");
                        }
                    }
                }
            }
        }

        private static bool TryParseTrigger(
            GameplayRule rule,
            string rulePath,
            ValidationResult result,
            out GameplayEventType triggerType)
        {
            triggerType = GameplayEventType.GameStarted;
            if (rule.Trigger == null ||
                string.IsNullOrWhiteSpace(rule.Trigger.Type))
            {
                return false;
            }

            if (Enum.TryParse(rule.Trigger.Type, true, out triggerType) &&
                GameplayEventCapabilities.IsRuntimeSupported(triggerType))
            {
                return true;
            }

            if (Enum.TryParse(rule.Trigger.Type, true, out triggerType))
            {
                result.AddError(
                    $"{rulePath}.trigger.type '{rule.Trigger.Type}' is not " +
                    "published by the current runtime.");
                return false;
            }

            result.AddError(
                $"{rulePath}.trigger.type '{rule.Trigger.Type}' is unknown.");
            return false;
        }

        private static void ValidateConditions(
            GameplayRule rule,
            string rulePath,
            bool hasKnownTrigger,
            GameplayEventType triggerType,
            ValidationResult result)
        {
            RuleCondition[] conditions = rule.Conditions;
            for (int index = 0; index < conditions.Length; index++)
            {
                RuleCondition condition = conditions[index];
                string path = $"{rulePath}.conditions[{index}]";
                if (condition == null)
                {
                    result.AddError($"{path} is null.");
                    continue;
                }

                if (!Enum.TryParse(
                        condition.Type,
                        true,
                        out RuleConditionType conditionType))
                {
                    result.AddError(
                        $"{path}.type '{condition.Type}' is unknown.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(condition.Comparison) &&
                    !Enum.TryParse(
                        condition.Comparison,
                        true,
                        out RuleComparison _))
                {
                    result.AddError(
                        $"{path}.comparison '{condition.Comparison}' is unknown.");
                }

                if (conditionType == RuleConditionType.EventValue)
                {
                    if (!Enum.TryParse(
                            condition.Comparison,
                            true,
                            out RuleComparison numericComparison) ||
                        !IsNumericComparison(numericComparison))
                    {
                        result.AddError(
                            $"{path}.comparison requires a numeric comparison.");
                    }

                    if (hasKnownTrigger && !ProvidesNumericValue(triggerType))
                    {
                        result.AddError(
                            $"{path} is incompatible with trigger '{triggerType}', which has no documented numeric value.");
                    }

                    continue;
                }

                if (conditionType != RuleConditionType.EnemyType)
                {
                    continue;
                }

                if (!Enum.TryParse(
                        condition.Comparison,
                        true,
                        out RuleComparison textComparison) ||
                    (textComparison != RuleComparison.Equals &&
                     textComparison != RuleComparison.NotEquals))
                {
                    result.AddError(
                        $"{path}.comparison requires Equals or NotEquals for EnemyType.");
                }

                if (string.IsNullOrWhiteSpace(condition.StringValue))
                {
                    result.AddError($"{path}.stringValue requires an enemy type.");
                }
                else if (!IsSupportedEnemyType(condition.StringValue))
                {
                    result.AddError(
                        $"{path}.stringValue '{condition.StringValue}' is unknown.");
                }

                if (hasKnownTrigger && !ProvidesEnemyContext(triggerType))
                {
                    result.AddError(
                        $"{path} is incompatible with trigger '{triggerType}'.");
                }
            }
        }

        private static bool IsSupportedEnemyType(string enemyType)
        {
            return string.Equals(
                       enemyType,
                       "Grunt",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       enemyType,
                       "Runner",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       enemyType,
                       "Tank",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateEffects(
            GameplayRule rule,
            string rulePath,
            EffectCatalog catalog,
            ChallengeSpec challenge,
            ValidationResult result)
        {
            RuleEffect[] effects = rule.Effects;
            for (int index = 0; index < effects.Length; index++)
            {
                RuleEffect effect = effects[index];
                string path = $"{rulePath}.effects[{index}]";
                if (effect == null)
                {
                    continue;
                }

                if (!catalog.TryGetDefinition(
                        effect.EffectId,
                        out EffectDefinition definition))
                {
                    result.AddError(
                        $"{path}.effectId '{effect.EffectId}' is not allowed.");
                }
                else
                {
                    ValidateTrustedEffectIdentity(
                        effect,
                        definition,
                        path,
                        result);
                }

                if (!TryGetEffectKind(effect, out RuleEffectKind kind))
                {
                    result.AddError($"{path}.kind '{effect.Kind}' is unknown.");
                    continue;
                }

                ValidateEffectKind(effect, kind, path, result);
                if (kind == RuleEffectKind.AddTime &&
                    challenge.TimeLimit <= 0f)
                {
                    result.AddError(
                        $"{path} AddTime requires a timed goal.");
                }
                if (kind == RuleEffectKind.AddTime &&
                    (!string.Equals(effect.StackMode, "None",
                         StringComparison.OrdinalIgnoreCase) ||
                     effect.MaxStacks != 1))
                {
                    result.AddError(
                        $"{path} AddTime requires stackMode None and maxStacks 1.");
                }

                bool hasScaling = effect.Scaling != null &&
                                  !string.IsNullOrWhiteSpace(
                                      effect.Scaling.Source);
                if (kind != RuleEffectKind.StatModifier)
                {
                    if (effect.Duration != 0f)
                    {
                        result.AddError(
                            $"{path}.duration is only supported for StatModifier effects.");
                    }

                    if (hasScaling)
                    {
                        result.AddError(
                            $"{path}.scaling is only supported for StatModifier effects.");
                    }
                }
                else if (hasScaling &&
                         Enum.TryParse(
                             effect.StackMode,
                             true,
                             out RuleStackMode scaledStackMode) &&
                         scaledStackMode != RuleStackMode.None)
                {
                    result.AddError(
                        $"{path} scaled modifiers must use stackMode None.");
                }

                if (!Enum.TryParse(
                        effect.StackMode,
                        true,
                        out RuleStackMode _))
                {
                    result.AddError(
                        $"{path}.stackMode '{effect.StackMode}' is unknown.");
                }

                ValidateScaling(effect.Scaling, path, result);
            }
        }

        private static void ValidateTrustedEffectIdentity(
            RuleEffect effect,
            EffectDefinition definition,
            string path,
            ValidationResult result)
        {
            if (!Same(effect.Kind, definition.CreatorKind) ||
                !Same(effect.Target, definition.CreatorTarget) ||
                !Same(effect.StatId, definition.CreatorStatId) ||
                !Same(effect.Operation, definition.CreatorOperation) ||
                !Same(effect.StringValue, definition.CreatorStringValue))
            {
                result.AddError(
                    $"{path} identity does not match trusted EffectCatalog " +
                    $"definition '{definition.EffectId}'.");
            }
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(
                left ?? string.Empty,
                right ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateEffectKind(
            RuleEffect effect,
            RuleEffectKind kind,
            string path,
            ValidationResult result)
        {
            switch (kind)
            {
                case RuleEffectKind.StatModifier:
                    if (!Enum.TryParse(
                            effect.Target,
                            true,
                            out RuntimeServiceTarget target))
                    {
                        result.AddError(
                            $"{path}.target '{effect.Target}' is unknown.");
                        return;
                    }

                    if (!Enum.TryParse(
                            effect.StatId,
                            true,
                            out RuntimeStatId statId))
                    {
                        result.AddError(
                            $"{path}.statId '{effect.StatId}' is unknown.");
                    }
                    else if (!TargetSupportsStat(target, statId))
                    {
                        result.AddError(
                            $"{path} target '{target}' cannot modify '{statId}'.");
                    }

                    if (!Enum.TryParse(
                            effect.Operation,
                            true,
                            out StatModifierOperation _))
                    {
                        result.AddError(
                            $"{path}.operation '{effect.Operation}' is unknown.");
                    }

                    break;
                case RuleEffectKind.SpawnEnemy:
                    if (!string.Equals(
                            effect.Target,
                            RuntimeServiceTarget.Enemy.ToString(),
                            StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(effect.StringValue))
                    {
                        result.AddError(
                            $"{path} SpawnEnemy requires target Enemy and an enemy type.");
                    }

                    break;
                case RuleEffectKind.GiveAmmo:
                    if (!string.Equals(
                            effect.Target,
                            RuntimeServiceTarget.Weapon.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddError(
                            $"{path} GiveAmmo requires target Weapon.");
                    }

                    break;
                case RuleEffectKind.AddTime:
                    if (!string.Equals(
                            effect.Target,
                            RuntimeServiceTarget.Time.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddError(
                            $"{path} AddTime requires target Time.");
                    }
                    break;
            }
        }

        private static void ValidateScaling(
            RuleScaling scaling,
            string effectPath,
            ValidationResult result)
        {
            if (scaling == null || string.IsNullOrWhiteSpace(scaling.Source))
            {
                return;
            }

            if (!Enum.TryParse(
                    scaling.Source,
                    true,
                    out RuntimeValueSource _))
            {
                result.AddError(
                    $"{effectPath}.scaling.source '{scaling.Source}' is unknown.");
            }

            if (!Enum.TryParse(
                    scaling.Mode,
                    true,
                    out RuleScalingMode _))
            {
                result.AddError(
                    $"{effectPath}.scaling.mode '{scaling.Mode}' is unknown.");
            }
        }

        private static bool TryGetEffectKind(
            RuleEffect effect,
            out RuleEffectKind kind)
        {
            kind = RuleEffectKind.StatModifier;
            if (string.IsNullOrWhiteSpace(effect.Kind))
            {
                return !string.IsNullOrWhiteSpace(effect.StatId);
            }

            return Enum.TryParse(effect.Kind, true, out kind);
        }

        private static bool ProvidesEnemyContext(GameplayEventType triggerType)
        {
            return triggerType == GameplayEventType.EnemyKilled ||
                   triggerType == GameplayEventType.EnemyHit ||
                   triggerType == GameplayEventType.Headshot;
        }

        private static bool ProvidesNumericValue(GameplayEventType triggerType)
        {
            return triggerType == GameplayEventType.EnemyHit ||
                   triggerType == GameplayEventType.Headshot ||
                   triggerType == GameplayEventType.PlayerHit ||
                   triggerType == GameplayEventType.WeaponFired ||
                   triggerType == GameplayEventType.PlayerHPChanged ||
                   triggerType == GameplayEventType.PlayerAmmoChanged;
        }

        private static bool IsNumericComparison(RuleComparison comparison)
        {
            return comparison == RuleComparison.Equals ||
                   comparison == RuleComparison.NotEquals ||
                   comparison == RuleComparison.LessThan ||
                   comparison == RuleComparison.LessOrEqual ||
                   comparison == RuleComparison.GreaterThan ||
                   comparison == RuleComparison.GreaterOrEqual;
        }

        private static bool TargetSupportsStat(
            RuntimeServiceTarget target,
            RuntimeStatId statId)
        {
            string statName = statId.ToString();
            switch (target)
            {
                case RuntimeServiceTarget.Player:
                    return statName.StartsWith("Player", StringComparison.Ordinal);
                case RuntimeServiceTarget.Enemy:
                    return statName.StartsWith("Enemy", StringComparison.Ordinal);
                case RuntimeServiceTarget.Weapon:
                    return statName.StartsWith("Weapon", StringComparison.Ordinal);
                default:
                    return false;
            }
        }
    }

    public sealed class RangeValidator
    {
        public void Validate(
            ChallengeSpec challenge,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            if (challenge == null)
            {
                return;
            }

            GameplayRule[] rules = challenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null)
                {
                    continue;
                }

                ValidateConditions(rule, ruleIndex, config, result);
                ValidateEffects(rule, ruleIndex, config, result);
            }
        }

        private static void ValidateConditions(
            GameplayRule rule,
            int ruleIndex,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            RuleCondition[] conditions = rule.Conditions;
            for (int index = 0; index < conditions.Length; index++)
            {
                RuleCondition condition = conditions[index];
                if (condition == null ||
                    !string.Equals(
                        condition.Type,
                        RuleConditionType.RandomChance.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string path = $"rules[{ruleIndex}].conditions[{index}].value";
                if (!IsFinite(condition.Value) ||
                    condition.Value < config.MinimumProbability ||
                    condition.Value > config.MaximumProbability)
                {
                    result.AddError(
                        $"{path} {condition.Value} is outside probability range " +
                        $"[{config.MinimumProbability}, {config.MaximumProbability}].");
                }
            }
        }

        private static void ValidateEffects(
            GameplayRule rule,
            int ruleIndex,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            RuleEffect[] effects = rule.Effects;
            for (int index = 0; index < effects.Length; index++)
            {
                RuleEffect effect = effects[index];
                if (effect == null)
                {
                    continue;
                }

                string path = $"rules[{ruleIndex}].effects[{index}]";
                ValidateEffectValue(effect, path, config, result);
                ValidateStacksAndDuration(effect, path, config, result);
                ValidateStackedMagnitude(effect, path, config, result);
                ValidateScaling(effect, path, config, result);
            }
        }

        private static void ValidateStackedMagnitude(
            RuleEffect effect,
            string path,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            if (!Enum.TryParse(
                    effect.StackMode,
                    true,
                    out RuleStackMode stackMode) ||
                stackMode != RuleStackMode.Stack ||
                effect.MaxStacks <= 0 ||
                !config.TryGetEffectValueLimit(
                    effect.EffectId,
                    out EffectValueLimit limit) ||
                limit.MaximumStackedMagnitude <= 0f)
            {
                return;
            }

            float stackedMagnitude =
                Math.Abs(effect.Value) * effect.MaxStacks;
            if (stackedMagnitude > limit.MaximumStackedMagnitude + 0.0001f)
            {
                result.AddError(
                    $"{path} reaches stacked magnitude {stackedMagnitude:0.###}, " +
                    $"above the safe total {limit.MaximumStackedMagnitude:0.###} " +
                    $"for '{effect.EffectId}'. Reduce value or maxStacks.");
                return;
            }

            if (stackedMagnitude >= limit.MaximumStackedMagnitude * 0.8f)
            {
                result.AddWarning(
                    $"{path} reaches {stackedMagnitude:0.###} when fully " +
                    "stacked and is close to its gameplay safety limit.");
            }
        }

        private static void ValidateEffectValue(
            RuleEffect effect,
            string path,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            if (!IsFinite(effect.Value))
            {
                result.AddError($"{path}.value must be a finite number.");
                return;
            }

            if (!config.TryGetEffectValueLimit(
                    effect.EffectId,
                    out EffectValueLimit limit))
            {
                result.AddError(
                    $"{path}.effectId '{effect.EffectId}' has no trusted value range.");
                return;
            }

            if (effect.Value < limit.MinimumValue ||
                effect.Value > limit.MaximumValue)
            {
                result.AddError(
                    $"{path}.value {effect.Value} is outside the allowed range " +
                    $"[{limit.MinimumValue}, {limit.MaximumValue}] for " +
                    $"'{effect.EffectId}'.");
            }
        }

        private static void ValidateStacksAndDuration(
            RuleEffect effect,
            string path,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            bool isStacking = Enum.TryParse(
                                  effect.StackMode,
                                  true,
                                  out RuleStackMode stackMode) &&
                              stackMode == RuleStackMode.Stack;
            int minimumStacks = isStacking ? 1 : 0;
            if (effect.MaxStacks < minimumStacks ||
                effect.MaxStacks > config.MaxStacks)
            {
                result.AddError(
                    $"{path}.maxStacks {effect.MaxStacks} is outside range " +
                    $"[{minimumStacks}, {config.MaxStacks}].");
            }

            if (!IsFinite(effect.Duration) ||
                effect.Duration < config.MinimumDuration ||
                effect.Duration > config.MaximumDuration)
            {
                result.AddError(
                    $"{path}.duration {effect.Duration} is outside range " +
                    $"[{config.MinimumDuration}, {config.MaximumDuration}].");
            }
        }

        private static void ValidateScaling(
            RuleEffect effect,
            string path,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            RuleScaling scaling = effect.Scaling;
            if (scaling == null || string.IsNullOrWhiteSpace(scaling.Source))
            {
                return;
            }

            if (!IsFinite(scaling.SourceMin) ||
                !IsFinite(scaling.SourceMax) ||
                scaling.SourceMax <= scaling.SourceMin)
            {
                result.AddError(
                    $"{path}.scaling source range must be finite and increasing.");
            }

            if (!config.TryGetEffectValueLimit(
                    effect.EffectId,
                    out EffectValueLimit limit))
            {
                return;
            }

            if (!IsFinite(scaling.EffectMin) ||
                !IsFinite(scaling.EffectMax) ||
                scaling.EffectMin < limit.MinimumValue ||
                scaling.EffectMin > limit.MaximumValue ||
                scaling.EffectMax < limit.MinimumValue ||
                scaling.EffectMax > limit.MaximumValue)
            {
                result.AddError(
                    $"{path}.scaling effect endpoints " +
                    $"[{scaling.EffectMin}, {scaling.EffectMax}] exceeds " +
                    $"[{limit.MinimumValue}, {limit.MaximumValue}].");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class ComplexityValidator
    {
        public void Validate(
            ChallengeSpec challenge,
            GameplayBalanceConfig config,
            ValidationResult result)
        {
            if (challenge == null)
            {
                return;
            }

            GameplayRule[] rules = challenge.Rules;
            if (rules.Length > config.MaxRules)
            {
                result.AddError(
                    $"Challenge has {rules.Length} rules; maximum is " +
                    $"{config.MaxRules}.");
            }

            for (int index = 0; index < rules.Length; index++)
            {
                GameplayRule rule = rules[index];
                if (rule == null)
                {
                    continue;
                }

                if (rule.Effects.Length > config.MaxEffectsPerRule)
                {
                    result.AddError(
                        $"rules[{index}] has {rule.Effects.Length} effects; " +
                        $"maximum is {config.MaxEffectsPerRule}.");
                }

                if (rule.Conditions.Length > config.MaxConditionsPerRule)
                {
                    result.AddError(
                        $"rules[{index}] has {rule.Conditions.Length} conditions; " +
                        $"maximum is {config.MaxConditionsPerRule}.");
                }
            }
        }
    }
}
