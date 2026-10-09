using System.Reflection;
using NUnit.Framework;
using RuleForge.AI;
using RuleForge.DSL;
using UnityEngine;

namespace RuleForge.Tests.EditMode
{
    public sealed class AIGameplayTests
    {
        [Test]
        public void ResponsesParser_ReadsStructuredTextAfterReasoningItem()
        {
            object[] arguments = ReadResponses(
                "{\"status\":\"completed\",\"output\":[" +
                "{\"type\":\"reasoning\",\"content\":[]}," +
                "{\"type\":\"message\",\"content\":[" +
                "{\"type\":\"output_text\",\"text\":\"{\\\"ok\\\":true}\"}]}]}");

            Assert.That(arguments[0], Is.True);
            Assert.That(arguments[1], Is.EqualTo("{\"ok\":true}"));
        }

        [Test]
        public void ResponsesParser_ExplainsTokenLimitInsteadOfGenericMissingText()
        {
            object[] arguments = ReadResponses(
                "{\"status\":\"incomplete\"," +
                "\"incomplete_details\":{\"reason\":\"max_output_tokens\"}," +
                "\"output\":[{\"type\":\"reasoning\",\"content\":[]}]}");

            Assert.That(arguments[0], Is.False);
            StringAssert.Contains("maxOutputTokens", (string)arguments[2]);
        }

        [Test]
        public void ResponsesParser_AcceptsTopLevelOutputTextWhenProvided()
        {
            object[] arguments = ReadResponses(
                "{\"status\":\"completed\"," +
                "\"output_text\":\"{\\\"ok\\\":true}\",\"output\":[]}");

            Assert.That(arguments[0], Is.True);
            Assert.That(arguments[1], Is.EqualTo("{\"ok\":true}"));
        }

        [Test]
        public void ResponsesParser_SurfacesProviderFailureMessage()
        {
            object[] arguments = ReadResponses(
                "{\"status\":\"failed\"," +
                "\"error\":{\"message\":\"provider failure\"}," +
                "\"output\":[]}");

            Assert.That(arguments[0], Is.False);
            Assert.That(arguments[2], Is.EqualTo("provider failure"));
        }

        private static object[] ReadResponses(string json)
        {
            MethodInfo reader = typeof(OpenAIResponsesGameplayService)
                .GetMethod("TryReadOutputText",
                    BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reader, Is.Not.Null);
            object[] parameters =
            {
                json,
                RuntimeAIProtocol.Responses,
                null,
                null
            };
            bool success = (bool)reader.Invoke(null, parameters);
            return new object[]
            {
                success,
                parameters[2],
                parameters[3]
            };
        }

        [Test]
        public void GameplayProposal_ParsesCompletenessAndPlayerFacingAdvice()
        {
            GameplayProposal proposal = JsonUtility.FromJson<GameplayProposal>(
                "{\"summary\":\"Kill to grow\",\"detectedIntent\":\"Snowball\"," +
                "\"confidence\":\"Medium\",\"goalExplicit\":false," +
                "\"triggerExplicit\":true,\"rewardExplicit\":true," +
                "\"riskExplicit\":false,\"scalingExplicit\":false," +
                "\"limitExplicit\":false,\"hasConflict\":false," +
                "\"withinVocabulary\":true,\"suggestedGoal\":\"Survive 60s\"," +
                "\"suggestedRules\":[\"Damage +6% per kill\"]," +
                "\"designReasoningSummary\":\"Adds controlled growth\"," +
                "\"warnings\":[\"No risk requested\"]," +
                "\"clarificationQuestion\":\"\",\"canGenerate\":true," +
                "\"penaltyRewardRatio\":1.5," +
                "\"actionSuggestions\":[\"Add a faster-growing risk\"]}");

            Assert.That(proposal.ConfidenceLevel,
                Is.EqualTo(GameplayProposalConfidence.Medium));
            Assert.That(proposal.GoalExplicit, Is.False);
            Assert.That(proposal.CanGenerate, Is.True);
            Assert.That(proposal.PenaltyRewardRatio, Is.EqualTo(1.5f));
            Assert.That(proposal.Warnings, Has.Length.EqualTo(1));
        }

        [Test]
        public void GameplayDesignerSchemas_AreStrictAndSeparateFromChallengeSpec()
        {
            string proposal = GameplayVocabulary.BuildProposalSchema();
            string modification =
                GameplayVocabulary.BuildModificationProposalSchema();
            string improvements = GameplayVocabulary.BuildImprovementSchema();

            StringAssert.Contains("\"additionalProperties\":false", proposal);
            StringAssert.Contains("\"clarificationQuestion\"", proposal);
            StringAssert.Contains("\"canGenerate\"", proposal);
            StringAssert.Contains("\"penaltyRewardRatio\"", proposal);
            StringAssert.Contains("\"exactMaxStacks\"", proposal);
            StringAssert.Contains("\"patchInstruction\"", modification);
            StringAssert.DoesNotContain("\"operations\"", modification);
            StringAssert.Contains("\"maxItems\":3", improvements);
            StringAssert.Contains("\"intent\"", improvements);
        }

        [Test]
        public void IntentContract_RejectsAmmoSubstitutedForKillToAddTime()
        {
            GameplayProposal proposal = CreateTimedKillProposal();
            ChallengeSpec wrong = CreateTimedKillChallenge("GiveAmmo", 10f);

            bool matches = ChallengeIntentContract.TryValidate(
                proposal, wrong, out string error);

            Assert.That(matches, Is.False);
            StringAssert.Contains("AddTime", error);
        }

        [Test]
        public void IntentContract_RejectsTimeRewardOnWrongTrigger()
        {
            GameplayProposal proposal = CreateTimedKillProposal();
            ChallengeSpec wrongTrigger = ChallengeSpec.Create(
                "timed-kill", "Timed Kill", "KillCount", 10f,
                "Assault Rifle",
                new[]
                {
                    GameplayRule.Create("reload-time", "WeaponReloaded",
                        System.Array.Empty<RuleCondition>(),
                        new[]
                        {
                            RuleEffect.Create("AddTime", "AddTime", "Time",
                                "", "AddFlat", 1f, "", "None", 0, 0f)
                        })
                }, 10f);

            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, wrongTrigger, out _), Is.False);
        }

        [Test]
        public void IntentContract_RequiresExactTimeRewardAndStartingClock()
        {
            GameplayProposal proposal = CreateTimedKillProposal();
            ChallengeSpec wrongReward =
                CreateTimedKillChallenge("AddTime", 2f);
            ChallengeSpec wrongClock =
                CreateTimedKillChallenge("AddTime", 1f, 20f);

            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, wrongReward, out _), Is.False);
            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, wrongClock, out _), Is.False);
            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, CreateTimedKillChallenge("AddTime", 1f), out _),
                Is.True);
        }

        [Test]
        public void IntentContract_RejectsInventedVictoryTargetAndExtraRisk()
        {
            GameplayProposal proposal = CreateTimedKillProposal();
            ChallengeSpec wrongTarget = ChallengeSpec.Create(
                "timed-kill", "Timed Kill", "KillCount", 30f,
                "Assault Rifle",
                CreateTimedKillChallenge("AddTime", 1f).Rules,
                10f);
            ChallengeSpec extraRisk = ChallengeSpec.Create(
                "timed-kill", "Timed Kill", "KillCount", 10f,
                "Assault Rifle",
                new[]
                {
                    CreateTimedKillChallenge("AddTime", 1f).Rules[0],
                    GameplayRule.Create("extra-risk", "EnemyKilled",
                        System.Array.Empty<RuleCondition>(),
                        new[]
                        {
                            RuleEffect.Create("EnemySpeed", "StatModifier",
                                "Enemy", "EnemyMoveSpeed", "AddPercent",
                                0.05f, "", "Stack", 20, 0f)
                        })
                }, 10f);

            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, wrongTarget, out _), Is.False);
            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, extraRisk, out _), Is.False);
        }

        [Test]
        public void IntentContract_TimeBankEndlessRequiresTimeDamageConversion()
        {
            GameplayProposal proposal = JsonUtility.FromJson<GameplayProposal>(
                "{\"requiredGoalType\":\"TimeBankEndless\"," +
                "\"requiredGoalTarget\":0," +
                "\"requiredTimeLimitSeconds\":10," +
                "\"requiredTimeDamageScale\":0.1," +
                "\"suggestedRules\":[\"每次击杀加一秒\"]," +
                "\"requiredMechanics\":[{\"trigger\":\"EnemyKilled\"," +
                "\"effectId\":\"AddTime\",\"value\":1," +
                "\"exactValue\":true}]}" );
            GameplayRule[] rules = CreateTimedKillChallenge("AddTime", 1f).Rules;
            ChallengeSpec correct = ChallengeSpec.Create(
                "time_trial", "Time Trial", "TimeBankEndless", 0f,
                "Assault Rifle", rules, 10f, 0.1f);
            ChallengeSpec wrong = ChallengeSpec.Create(
                "time_trial", "Time Trial", "TimeBankEndless", 0f,
                "Assault Rifle", rules, 10f, 0.2f);

            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, correct, out _), Is.True);
            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, wrong, out _), Is.False);
        }

        [Test]
        public void IntentContract_RejectsChangingExplicitTenStackLimit()
        {
            GameplayProposal proposal = JsonUtility.FromJson<GameplayProposal>(
                "{\"requiredGoalType\":\"Survive\"," +
                "\"requiredGoalTarget\":60," +
                "\"requiredTimeLimitSeconds\":0," +
                "\"requiredMechanics\":[{\"trigger\":\"EnemyKilled\"," +
                "\"effectId\":\"PlayerDamage\",\"value\":0.05," +
                "\"exactValue\":true,\"maxStacks\":10," +
                "\"exactMaxStacks\":true}]}" );
            GameplayRule tenStacks = GameplayRule.Create(
                "damage", "EnemyKilled", System.Array.Empty<RuleCondition>(),
                new[]
                {
                    RuleEffect.Create("PlayerDamage", "StatModifier",
                        "Weapon", "WeaponDamage", "AddPercent", 0.05f,
                        "", "Stack", 10, 0f)
                });
            GameplayRule threeStacks = GameplayRule.Create(
                "damage", "EnemyKilled", System.Array.Empty<RuleCondition>(),
                new[]
                {
                    RuleEffect.Create("PlayerDamage", "StatModifier",
                        "Weapon", "WeaponDamage", "AddPercent", 0.05f,
                        "", "Stack", 3, 0f)
                });
            ChallengeSpec correct = ChallengeSpec.Create(
                "survive", "Survive", "Survive", 60f,
                "Assault Rifle", new[] { tenStacks });
            ChallengeSpec wrong = ChallengeSpec.Create(
                "survive", "Survive", "Survive", 60f,
                "Assault Rifle", new[] { threeStacks });

            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, correct, out _), Is.True);
            Assert.That(ChallengeIntentContract.TryValidate(
                proposal, wrong, out _), Is.False);
        }

        private static GameplayProposal CreateTimedKillProposal()
        {
            return JsonUtility.FromJson<GameplayProposal>(
                "{\"suggestedRules\":[\"每击杀一名敌人加一秒\"]," +
                "\"requiredGoalType\":\"KillCount\"," +
                "\"requiredGoalTarget\":10," +
                "\"requiredTimeLimitSeconds\":10," +
                "\"requiredMechanics\":[{" +
                "\"trigger\":\"EnemyKilled\"," +
                "\"effectId\":\"AddTime\"," +
                "\"value\":1,\"exactValue\":true}]}" );
        }

        private static ChallengeSpec CreateTimedKillChallenge(
            string effectId,
            float effectValue,
            float timeLimit = 10f)
        {
            return ChallengeSpec.Create(
                "timed-kill", "Timed Kill", "KillCount", 10f,
                "Assault Rifle",
                new[]
                {
                    GameplayRule.Create("kill-time", "EnemyKilled",
                        System.Array.Empty<RuleCondition>(),
                        new[]
                        {
                            RuleEffect.Create(effectId, "AddTime", "Time",
                                "", "AddFlat", effectValue, "", "None",
                                0, 0f)
                        })
                },
                timeLimit);
        }

        [Test]
        public void ChallengeRepairScope_AllowsOnlyTheInitiallyChangedField()
        {
            ChallengeSpec baseline = CreateDamageChallenge(0.05f, 10);
            ChallengeSpec invalidCandidate = CreateDamageChallenge(3f, 10);
            ChallengeSpec repairedCandidate = CreateDamageChallenge(0.25f, 10);

            bool isWithinScope =
                ChallengeRepairScope.ContainsOnlyInitiallyChangedFields(
                    baseline,
                    invalidCandidate,
                    repairedCandidate,
                    out string[] extras);

            Assert.That(isWithinScope, Is.True);
            Assert.That(extras, Is.Empty);
        }

        [Test]
        public void ChallengeRepairScope_RejectsAnUnrelatedFieldChange()
        {
            ChallengeSpec baseline = CreateDamageChallenge(0.05f, 10);
            ChallengeSpec invalidCandidate = CreateDamageChallenge(3f, 10);
            ChallengeSpec repairedCandidate = CreateDamageChallenge(0.25f, 4);

            bool isWithinScope =
                ChallengeRepairScope.ContainsOnlyInitiallyChangedFields(
                    baseline,
                    invalidCandidate,
                    repairedCandidate,
                    out string[] extras);

            Assert.That(isWithinScope, Is.False);
            Assert.That(extras, Has.Some.Contains("maxStacks"));
        }

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

        private static ChallengeSpec CreateDamageChallenge(
            float value,
            int maxStacks)
        {
            return ChallengeSpec.Create(
                "repair_scope",
                "Repair Scope",
                "KillCount",
                10f,
                "Assault Rifle",
                new[]
                {
                    GameplayRule.Create(
                        "damage_growth",
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
                                value,
                                string.Empty,
                                "Stack",
                                maxStacks,
                                0f)
                        })
                });
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
