using System.Linq;
using System.Text;
using NUnit.Framework;
using RuleForge.Config;
using RuleForge.DSL;
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
            balanceConfig.EnsureEffectValueLimit("PlayerDamage", 0f, 1.5f);

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
