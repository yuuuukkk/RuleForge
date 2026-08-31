using System.Collections.Generic;
using NUnit.Framework;
using RuleForge.Analytics;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Validation;
using UnityEngine;

namespace RuleForge.Tests.EditMode
{
    public sealed class AnalyticsTests
    {
        [Test]
        public void CalculateSummary_UsesOnlyRealProviderBenchmarkSamples()
        {
            List<AIGenerationRecord> ai = new List<AIGenerationRecord>
            {
                AIRecord(true, true, false, true, 100f),
                AIRecord(true, false, false, true, 200f),
                AIRecord(true, true, true, true, 300f),
                AIRecord(true, true, false, false, 1f)
            };
            List<GameplaySessionRecord> gameplay =
                new List<GameplaySessionRecord>
                {
                    GameplayRecord(true, 10f, 3, 1, 2),
                    GameplayRecord(false, 20f, 2, 0, 1)
                };

            AnalyticsSummary summary =
                AnalyticsStorage.CalculateSummary(ai, gameplay);

            Assert.AreEqual(4, summary.totalAIRequests);
            Assert.AreEqual(3, summary.benchmarkAIRequests);
            Assert.AreEqual(3, summary.generationBenchmarkRequests);
            Assert.AreEqual(0.5f, summary.firstPassGenerationSuccessRate);
            Assert.AreEqual(1f / 3f, summary.validationRejectionRate, 0.0001f);
            Assert.AreEqual(1f, summary.retrySuccessRate);
            Assert.AreEqual(200f, summary.averageGenerationLatencyMs);
            Assert.AreEqual(2, summary.gameplaySessions);
            Assert.AreEqual(0.5f, summary.victoryRate);
            Assert.AreEqual(5, summary.totalKills);
            Assert.AreEqual(1, summary.totalHeadshots);
            Assert.AreEqual(3, summary.totalRulesTriggered);
        }

        [Test]
        public void Validator_AcceptsDescendingLastStandScaling()
        {
            GameplayBalanceConfig balance =
                ScriptableObject.CreateInstance<GameplayBalanceConfig>();
            EffectCatalog catalog = ScriptableObject.CreateInstance<EffectCatalog>();
            try
            {
                const string effectId = "PlayerMoveSpeedFromMissingHealth";
                balance.EnsureEffectValueLimit(effectId, -0.75f, 0f);
                catalog.EnsureDefinition(
                    effectId,
                    "Missing-Health Move Speed",
                    EffectPolarity.Penalty);
                catalog.ConfigureCreatorTemplate(
                    effectId,
                    false,
                    "StatModifier",
                    "Player",
                    "PlayerMoveSpeed",
                    "AddPercent",
                    0f,
                    string.Empty,
                    "None",
                    1);

                const string json =
                    "{\"id\":\"last_stand_test\"," +
                    "\"displayName\":\"Last Stand Test\"," +
                    "\"goal\":\"Survive\",\"goalTarget\":10," +
                    "\"weapon\":\"Assault Rifle\",\"rules\":[{" +
                    "\"id\":\"slow\",\"trigger\":{\"type\":\"PlayerHPChanged\"}," +
                    "\"conditions\":[],\"effects\":[{" +
                    "\"effectId\":\"PlayerMoveSpeedFromMissingHealth\"," +
                    "\"kind\":\"StatModifier\",\"target\":\"Player\"," +
                    "\"statId\":\"PlayerMoveSpeed\"," +
                    "\"operation\":\"AddPercent\",\"value\":0," +
                    "\"stringValue\":\"\",\"stackMode\":\"None\"," +
                    "\"maxStacks\":1,\"duration\":0," +
                    "\"scaling\":{\"source\":\"PlayerMissingHPPercent\"," +
                    "\"mode\":\"Linear\",\"sourceMin\":0,\"sourceMax\":1," +
                    "\"effectMin\":0,\"effectMax\":-0.5}}]}]}";
                ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(json);

                ValidationResult result =
                    new ChallengeValidator(balance, catalog).Validate(challenge);

                Assert.IsTrue(result.IsValid, result.BuildSummary());
            }
            finally
            {
                Object.DestroyImmediate(balance);
                Object.DestroyImmediate(catalog);
            }
        }

        private static AIGenerationRecord AIRecord(
            bool parse,
            bool validation,
            bool retry,
            bool benchmark,
            float latency)
        {
            return new AIGenerationRecord
            {
                requestType = "Generate",
                parseSuccess = parse,
                validationPassed = validation,
                isRetry = retry,
                benchmarkEligible = benchmark,
                generationTimeMs = latency
            };
        }

        private static GameplaySessionRecord GameplayRecord(
            bool victory,
            float duration,
            int kills,
            int headshots,
            int rulesTriggered)
        {
            return new GameplaySessionRecord
            {
                victory = victory,
                playDurationSeconds = duration,
                kills = kills,
                headshots = headshots,
                rulesTriggered = rulesTriggered
            };
        }
    }
}
