using System.Linq;
using System.Text;
using NUnit.Framework;
using RuleForge.AI;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Rules;
using RuleForge.Validation;
using UnityEngine;

namespace RuleForge.Tests
{
    public sealed class ChallengeValidatorTests
    {
        private GameplayBalanceConfig balanceConfig;
        private EffectCatalog effectCatalog;
        private ChallengeValidator validator;

        [SetUp]
        public void SetUp()
        {
            balanceConfig = ScriptableObject.CreateInstance<GameplayBalanceConfig>();
            balanceConfig.ConfigureComplexityLimits(6, 4, 3, 20);
            balanceConfig.EnsureEffectValueLimit(
                "PlayerDamage", 0f, 1.5f, 1.5f);

            effectCatalog = ScriptableObject.CreateInstance<EffectCatalog>();
            effectCatalog.EnsureDefinition(
                "PlayerDamage",
                "Player Damage",
                EffectPolarity.Reward);
            effectCatalog.ConfigureCreatorTemplate(
                "PlayerDamage",
                true,
                "StatModifier",
                "Weapon",
                "WeaponDamage",
                "AddPercent",
                0.05f,
                string.Empty,
                "Stack",
                10);
            validator = new ChallengeValidator(balanceConfig, effectCatalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(balanceConfig);
            Object.DestroyImmediate(effectCatalog);
        }

        [Test]
        public void Validate_RejectsPlayerDamageAtFiveHundredPercent()
        {
            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(
                BuildChallengeJson(BuildRuleJson("damage_500", 5f)));

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Errors.Any(error => error.Contains("allowed range")),
                Is.True);
        }

        [Test]
        public void Validate_RejectsTwentyRules()
        {
            StringBuilder rules = new StringBuilder();
            for (int index = 0; index < 20; index++)
            {
                if (index > 0)
                {
                    rules.Append(',');
                }

                rules.Append(BuildRuleJson($"rule_{index}", 0.05f));
            }

            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(
                BuildChallengeJson(rules.ToString()));

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Errors.Any(error => error.Contains("20 rules")),
                Is.True);
        }

        [Test]
        public void Validate_RejectsTriggerNotPublishedByRuntime()
        {
            string rule = BuildRuleJson("timer_rule", 0.05f)
                .Replace("EnemyKilled", "TimerInterval");
            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(
                BuildChallengeJson(rule));

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Errors.Any(error => error.Contains("not published")),
                Is.True);
        }

        [Test]
        public void Validate_AcceptsFiniteDurationOnStatModifier()
        {
            string rule = BuildRuleJson("timed_damage", 0.05f)
                .Replace(
                    "\"maxStacks\":1}",
                    "\"maxStacks\":1,\"duration\":3}");
            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(
                BuildChallengeJson(rule));

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.True, result.BuildSummary());
        }

        [Test]
        public void EventValueCondition_ComparesPublishedNumericContext()
        {
            ConditionEvaluator evaluator = new ConditionEvaluator(() => 0f);
            RuleCondition[] conditions =
            {
                RuleCondition.Create(
                    RuleConditionType.EventValue.ToString(),
                    RuleComparison.LessOrEqual.ToString(),
                    0.25f,
                    string.Empty)
            };

            Assert.That(
                evaluator.EvaluateAll(
                    conditions,
                    new GameplayEvent(
                        GameplayEventType.PlayerAmmoChanged,
                        value: 0.2f)),
                Is.True);
            Assert.That(
                evaluator.EvaluateAll(
                    conditions,
                    new GameplayEvent(
                        GameplayEventType.PlayerAmmoChanged,
                        value: 0.5f)),
                Is.False);
        }

        [Test]
        public void BalanceEvaluation_DerivesGrowthAndSnowballTagFromStacking()
        {
            string rule = BuildRuleJson("stacking_damage", 0.05f)
                .Replace(
                    "\"stackMode\":\"None\",\"maxStacks\":1",
                    "\"stackMode\":\"Stack\",\"maxStacks\":10");
            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(
                BuildChallengeJson(rule));

            BalanceEvaluation evaluation =
                new BalanceEvaluator(balanceConfig, effectCatalog)
                    .Evaluate(challenge);

            Assert.That(evaluation.Growth, Is.Not.EqualTo(GrowthSpeed.None));
            Assert.That(evaluation.GameplayTags, Does.Contain("Snowball"));
        }

        [Test]
        public void Validate_RejectsUnsafeFullyStackedMagnitude()
        {
            string rule = BuildRuleJson("unsafe_stack", 0.2f)
                .Replace(
                    "\"stackMode\":\"None\",\"maxStacks\":1",
                    "\"stackMode\":\"Stack\",\"maxStacks\":10");
            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(
                BuildChallengeJson(rule));

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Errors.Any(error => error.Contains("safe total")),
                Is.True);
        }

        [Test]
        public void Validate_WarnsWhenStackedMagnitudeApproachesLimit()
        {
            string rule = BuildRuleJson("high_stack", 0.12f)
                .Replace(
                    "\"stackMode\":\"None\",\"maxStacks\":1",
                    "\"stackMode\":\"Stack\",\"maxStacks\":10");
            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(
                BuildChallengeJson(rule));

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.True, result.BuildSummary());
            Assert.That(result.Warnings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void TimedKillGoal_AcceptsKillToAddTime()
        {
            RegisterAddTime();
            ChallengeSpec challenge = BuildTimedKillChallenge(10f);

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.True, result.BuildSummary());
        }

        [Test]
        public void SurviveSixtySeconds_WithTwoTenStackKillEffects_IsValid()
        {
            balanceConfig.EnsureEffectValueLimit(
                "EnemyMoveSpeed", 0.05f, 1.5f, 1f);
            effectCatalog.EnsureDefinition(
                "EnemyMoveSpeed", "Enemy Move Speed", EffectPolarity.Penalty);
            effectCatalog.ConfigureCreatorTemplate(
                "EnemyMoveSpeed", true, "StatModifier", "Enemy",
                "EnemyMoveSpeed", "AddPercent", 0.08f,
                string.Empty, "Stack", 10);
            ChallengeSpec challenge = ChallengeSpec.Create(
                "survive_growth", "Survive Growth", "Survive", 60f,
                "Assault Rifle", new[]
                {
                    GameplayRule.Create("damage_growth", "EnemyKilled",
                        new RuleCondition[0], new[]
                        {
                            RuleEffect.Create("PlayerDamage", "StatModifier",
                                "Weapon", "WeaponDamage", "AddPercent",
                                0.05f, string.Empty, "Stack", 10, 0f)
                        }),
                    GameplayRule.Create("enemy_growth", "EnemyKilled",
                        new RuleCondition[0], new[]
                        {
                            RuleEffect.Create("EnemyMoveSpeed", "StatModifier",
                                "Enemy", "EnemyMoveSpeed", "AddPercent",
                                0.08f, string.Empty, "Stack", 10, 0f)
                        })
                });

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.True, result.BuildSummary());
        }

        [Test]
        public void EnemyKilled_EventValueMilestone_IsRejected()
        {
            ChallengeSpec challenge = ChallengeSpec.Create(
                "invalid_milestone", "Invalid Milestone", "Survive", 60f,
                "Assault Rifle", new[]
                {
                    GameplayRule.Create("third_kill", "EnemyKilled",
                        new[]
                        {
                            RuleCondition.Create("EventValue", "Equals", 3f,
                                string.Empty)
                        },
                        new[]
                        {
                            RuleEffect.Create("PlayerDamage", "StatModifier",
                                "Weapon", "WeaponDamage", "AddPercent",
                                0.05f, string.Empty, "Stack", 10, 0f)
                        })
                });

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.Errors.Any(error =>
                error.Contains("no documented numeric value")), Is.True);
        }

        [TestCase("TimeBankTarget", 100f)]
        [TestCase("TimeBankSurvive", 60f)]
        [TestCase("TimeBankEndless", 0f)]
        public void TimeBankGoals_AllowKillToExtendTimeWithoutKillObjective(
            string goal, float target)
        {
            RegisterAddTime();
            ChallengeSpec challenge = ChallengeSpec.Create(
                "time_trial", "Time Trial", goal, target,
                "Assault Rifle", BuildTimedKillChallenge(10f).Rules,
                10f, 0.1f);

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.IsValid, Is.True, result.BuildSummary());
        }

        [Test]
        public void TimeBank_RejectsImmediateVictoryAndMissingDamageConversion()
        {
            RegisterAddTime();
            GameplayRule[] rules = BuildTimedKillChallenge(10f).Rules;
            ChallengeSpec immediate = ChallengeSpec.Create(
                "time_trial", "Time Trial", "TimeBankTarget", 10f,
                "Assault Rifle", rules, 10f, 0.1f);
            ChallengeSpec noConversion = ChallengeSpec.Create(
                "time_trial", "Time Trial", "TimeBankEndless", 0f,
                "Assault Rifle", rules, 10f, 0f);

            Assert.That(validator.Validate(immediate).Errors.Any(
                error => error.Contains("victory is immediate")), Is.True);
            Assert.That(validator.Validate(noConversion).Errors.Any(
                error => error.Contains("timeDamageScale")), Is.True);
        }

        [Test]
        public void AddTime_RequiresTimedGoalAndBoundedValue()
        {
            RegisterAddTime();
            ChallengeSpec noTimer = BuildTimedKillChallenge(0f);
            ChallengeSpec tooMuchTime = BuildTimedKillChallenge(10f, 100f);

            Assert.That(validator.Validate(noTimer).Errors.Any(
                error => error.Contains("requires a timed")), Is.True);
            Assert.That(validator.Validate(tooMuchTime).Errors.Any(
                error => error.Contains("allowed range")), Is.True);
        }

        [Test]
        public void ModifyTimeLimitPatch_PreservesGoalAndRules()
        {
            RegisterAddTime();
            ChallengeSpec current = BuildTimedKillChallenge(10f);
            ChallengePatch patch = JsonUtility.FromJson<ChallengePatch>(
                "{\"operations\":[{\"operation\":\"ModifyTimeLimit\"," +
                "\"timeLimit\":15}]}");

            bool applied = ChallengePatchApplier.TryApply(
                current, patch, out ChallengeSpec candidate,
                out string error);

            Assert.That(applied, Is.True, error);
            Assert.That(candidate.TimeLimit, Is.EqualTo(15f));
            Assert.That(candidate.Goal, Is.EqualTo(current.Goal));
            Assert.That(candidate.Rules[0].Id, Is.EqualTo(current.Rules[0].Id));
            Assert.That(validator.Validate(candidate).IsValid, Is.True);
        }

        [Test]
        public void EventValueScaling_RejectsTriggerWithoutNumericValue()
        {
            RuleEffect effect = RuleEffect.Create(
                "PlayerDamage", "StatModifier", "Weapon", "WeaponDamage",
                "AddPercent", 0.05f, string.Empty, "None", 1, 0f,
                RuleScaling.Create("EventValue", "Linear",
                    0f, 1f, 0.05f, 0.1f));
            ChallengeSpec challenge = ChallengeSpec.Create(
                "invalid_scaling", "Invalid Scaling", "KillCount", 10f,
                "Assault Rifle",
                new[]
                {
                    GameplayRule.Create("non_numeric", "GameStarted",
                        new RuleCondition[0], new[] { effect })
                });

            ValidationResult result = validator.Validate(challenge);

            Assert.That(result.Errors.Any(
                error => error.Contains("requires a numeric trigger")),
                Is.True);
        }

        private void RegisterAddTime()
        {
            balanceConfig.EnsureEffectValueLimit("AddTime", 0.1f, 10f, 0f);
            effectCatalog.EnsureDefinition(
                "AddTime", "Extra Time", EffectPolarity.Reward);
            effectCatalog.ConfigureCreatorTemplate(
                "AddTime", true, "AddTime", "Time", string.Empty,
                string.Empty, 1f, string.Empty, "None", 1);
        }

        private static ChallengeSpec BuildTimedKillChallenge(
            float timeLimit, float bonusSeconds = 1f)
        {
            return ChallengeSpec.Create(
                "timed_kills", "Timed Kills", "KillCount", 10f,
                "Assault Rifle",
                new[]
                {
                    GameplayRule.Create("extra_second", "EnemyKilled",
                        new RuleCondition[0],
                        new[]
                        {
                            RuleEffect.Create("AddTime", "AddTime", "Time",
                                string.Empty, string.Empty, bonusSeconds,
                                string.Empty, "None", 1, 0f)
                        })
                },
                timeLimit);
        }

        private static string BuildChallengeJson(string rules)
        {
            return
                "{\"id\":\"validator_test\",\"displayName\":\"Validator Test\"," +
                "\"goal\":\"KillCount\",\"goalTarget\":10," +
                "\"weapon\":\"Assault Rifle\"," +
                $"\"rules\":[{rules}]}}";
        }

        private static string BuildRuleJson(string id, float value)
        {
            return
                $"{{\"id\":\"{id}\",\"trigger\":{{\"type\":\"EnemyKilled\"}}," +
                "\"conditions\":[],\"effects\":[{" +
                "\"effectId\":\"PlayerDamage\"," +
                "\"kind\":\"StatModifier\"," +
                "\"target\":\"Weapon\"," +
                "\"statId\":\"WeaponDamage\"," +
                "\"operation\":\"AddPercent\"," +
                $"\"value\":{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                "\"stackMode\":\"None\",\"maxStacks\":1}]}";
        }
    }
}
