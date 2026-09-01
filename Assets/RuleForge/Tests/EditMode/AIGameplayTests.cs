using NUnit.Framework;
using RuleForge.AI;
using RuleForge.DSL;
using UnityEngine;

namespace RuleForge.Tests.EditMode
{
    public sealed class AIGameplayTests
    {
        [Test]
        public void ChallengePatch_ModifiesOnlyRequestedEffectValue()
        {
            ChallengeSpec current = JsonUtility.FromJson<ChallengeSpec>(
                "{\"id\":\"test\",\"displayName\":\"Test\"," +
                "\"goal\":\"KillCount\",\"goalTarget\":10," +
                "\"weapon\":\"Assault Rifle\"," +
                "\"rules\":[{\"id\":\"rule\",\"trigger\":{" +
                "\"type\":\"EnemyKilled\"},\"conditions\":[],\"effects\":[" +
                "{\"effectId\":\"PlayerDamage\",\"kind\":\"StatModifier\"," +
                "\"target\":\"Weapon\",\"statId\":\"WeaponDamage\"," +
                "\"operation\":\"AddPercent\",\"value\":0.05," +
                "\"stringValue\":\"\",\"stackMode\":\"Stack\"," +
                "\"maxStacks\":10,\"duration\":0.0}]}]}");
            ChallengePatch patch = JsonUtility.FromJson<ChallengePatch>(
                "{\"operations\":[{\"operation\":\"ModifyValue\"," +
                "\"ruleId\":\"rule\",\"effectId\":\"PlayerDamage\"," +
                "\"conditionIndex\":0,\"value\":0.1,\"maxStacks\":0," +
                "\"duration\":0.0,\"probability\":0.0,\"goal\":\"\"," +
                "\"goalTarget\":0.0," +
                "\"rule\":null}]}");

            bool applied = ChallengePatchApplier.TryApply(
                current,
                patch,
                out ChallengeSpec candidate,
                out string error);

            Assert.That(applied, Is.True, error);
            Assert.That(candidate.Rules[0].Effects[0].Value, Is.EqualTo(0.1f));
            Assert.That(candidate.Rules[0].Effects[0].MaxStacks, Is.EqualTo(10));
            Assert.That(current.Rules[0].Effects[0].Value, Is.EqualTo(0.05f));
        }

        [Test]
        public void ChallengePatch_RejectsUnknownOperation()
        {
            ChallengeSpec current = ChallengeSpec.Create(
                "test",
                "Test",
                "KillCount",
                10f,
                "Assault Rifle",
                new[]
                {
                    GameplayRule.Create(
                        "rule",
                        "EnemyKilled",
                        new RuleCondition[0],
                        new[]
                        {
                            RuleEffect.Create(
                                "PlayerDamage",
                                "StatModifier",
                                "Weapon",
                                "WeaponDamage",
                                "AddPercent",
                                0.05f,
                                string.Empty,
                                "Stack",
                                10,
                                0f)
                        })
                });
            ChallengePatch patch = JsonUtility.FromJson<ChallengePatch>(
                "{\"operations\":[{\"operation\":\"RewriteEverything\"}]}");

            bool applied = ChallengePatchApplier.TryApply(
                current,
                patch,
                out _,
                out string error);

            Assert.That(applied, Is.False);
            StringAssert.Contains("Unknown operation", error);
        }

        [Test]
        public void ChallengePatch_ModifyScalingPreservesUnrequestedFields()
        {
            ChallengeSpec current = JsonUtility.FromJson<ChallengeSpec>(
                "{\"id\":\"scaling\",\"displayName\":\"Scaling\"," +
                "\"goal\":\"Survive\",\"goalTarget\":30," +
                "\"weapon\":\"Assault Rifle\",\"rules\":[{" +
                "\"id\":\"low_hp\",\"trigger\":{\"type\":\"PlayerHPChanged\"}," +
                "\"conditions\":[{\"type\":\"RandomChance\"," +
                "\"comparison\":\"\",\"value\":0.5,\"stringValue\":\"\"}]," +
                "\"effects\":[{\"effectId\":\"PlayerDamageFromMissingHealth\"," +
                "\"kind\":\"StatModifier\",\"target\":\"Weapon\"," +
                "\"statId\":\"WeaponDamage\",\"operation\":\"AddPercent\"," +
                "\"value\":0.0,\"stringValue\":\"\",\"stackMode\":\"None\"," +
                "\"maxStacks\":1,\"duration\":0.0,\"scaling\":{" +
                "\"source\":\"PlayerMissingHPPercent\",\"mode\":\"Linear\"," +
                "\"sourceMin\":0.0,\"sourceMax\":1.0," +
                "\"effectMin\":0.0,\"effectMax\":1.0}}]}]}" );
            ChallengePatch patch = JsonUtility.FromJson<ChallengePatch>(
                "{\"operations\":[{\"operation\":\"ModifyScaling\"," +
                "\"ruleId\":\"low_hp\",\"effectId\":\"PlayerDamageFromMissingHealth\"," +
                "\"conditionIndex\":0,\"value\":0.0,\"maxStacks\":0," +
                "\"duration\":0.0,\"probability\":0.0,\"goal\":\"\"," +
                "\"goalTarget\":0.0,\"weapon\":\"\",\"condition\":null," +
                "\"scaling\":{\"source\":\"PlayerMissingHPPercent\"," +
                "\"mode\":\"Linear\",\"sourceMin\":0.0,\"sourceMax\":1.0," +
                "\"effectMin\":0.0,\"effectMax\":1.5},\"rule\":null}]}" );

            bool applied = ChallengePatchApplier.TryApply(
                current,
                patch,
                out ChallengeSpec candidate,
                out string error);

            Assert.That(applied, Is.True, error);
            Assert.That(candidate.Weapon, Is.EqualTo("Assault Rifle"));
            Assert.That(candidate.Rules[0].Conditions[0].Value, Is.EqualTo(0.5f));
            Assert.That(
                candidate.Rules[0].Effects[0].Scaling.EffectMax,
                Is.EqualTo(1.5f));
            Assert.That(
                current.Rules[0].Effects[0].Scaling.EffectMax,
                Is.EqualTo(1f));
        }
    }
}
