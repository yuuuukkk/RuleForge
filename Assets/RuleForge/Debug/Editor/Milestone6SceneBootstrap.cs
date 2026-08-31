using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Validation;
using UnityEditor;
using UnityEngine;

namespace RuleForge.Editor
{
    public static class Milestone6SceneBootstrap
    {
        private const string BalanceConfigPath =
            "Assets/RuleForge/Config/Defaults/GameplayBalanceConfig.asset";
        private const string EffectCatalogPath =
            "Assets/RuleForge/Config/Defaults/EffectCatalog.asset";
        private const string ShowcasePath =
            "Assets/RuleForge/Challenge/Examples/rule_expression_showcase.json";
        private const string InvalidDamagePath =
            "Assets/RuleForge/Challenge/Examples/invalid_player_damage_500.json";
        private const string InvalidComplexityPath =
            "Assets/RuleForge/Challenge/Examples/invalid_twenty_rules.json";

        [MenuItem("RuleForge/Setup Milestone 6 Validator")]
        public static void SetupMilestone6Validator()
        {
            Milestone5SceneBootstrap.SetupMilestone5TuningPanel();

            GameplayBalanceConfig balanceConfig =
                AssetDatabase.LoadAssetAtPath<GameplayBalanceConfig>(
                    BalanceConfigPath);
            EffectCatalog effectCatalog =
                AssetDatabase.LoadAssetAtPath<EffectCatalog>(EffectCatalogPath);
            if (balanceConfig == null || effectCatalog == null)
            {
                throw new MissingReferenceException(
                    "GameplayBalanceConfig or EffectCatalog was not found.");
            }

            balanceConfig.ConfigureComplexityLimits(6, 4, 3, 20);
            balanceConfig.ConfigureBalanceThresholds(0.75f, 1.25f);
            balanceConfig.EnsureEffectValueLimit("PlayerDamage", 0f, 1.5f);
            balanceConfig.EnsureEffectValueLimit("EnemyMoveSpeed", 0.05f, 1.5f);
            balanceConfig.EnsureEffectValueLimit(
                "PlayerDamageFromMissingHealth",
                0f,
                1.5f);
            balanceConfig.EnsureEffectValueLimit("SpawnRunner", 0f, 0f);
            balanceConfig.EnsureEffectValueLimit("GiveAmmo", 1f, 100f);

            effectCatalog.ConfigureBalanceWeight("PlayerDamage", 10f);
            effectCatalog.ConfigureBalanceWeight("EnemyMoveSpeed", 10f);
            effectCatalog.ConfigureBalanceWeight(
                "PlayerDamageFromMissingHealth",
                12f);
            effectCatalog.ConfigureBalanceWeight("SpawnRunner", 15f);
            effectCatalog.ConfigureBalanceWeight("GiveAmmo", 8f);

            EditorUtility.SetDirty(balanceConfig);
            EditorUtility.SetDirty(effectCatalog);
            AssetDatabase.SaveAssets();
            Debug.Log("RuleForge Milestone 6 Validator setup complete.");
        }

        [MenuItem("RuleForge/Validate Milestone 6 Acceptance Samples")]
        public static void ValidateMilestone6AcceptanceSamples()
        {
            GameplayBalanceConfig balanceConfig =
                AssetDatabase.LoadAssetAtPath<GameplayBalanceConfig>(
                    BalanceConfigPath);
            EffectCatalog effectCatalog =
                AssetDatabase.LoadAssetAtPath<EffectCatalog>(EffectCatalogPath);
            if (balanceConfig == null || effectCatalog == null)
            {
                throw new MissingReferenceException(
                    "Run Setup Milestone 6 Validator before validating samples.");
            }

            ChallengeValidator validator = new ChallengeValidator(
                balanceConfig,
                effectCatalog);
            ValidateSample(ShowcasePath, validator);
            ValidateSample(InvalidDamagePath, validator);
            ValidateSample(InvalidComplexityPath, validator);
        }

        private static void ValidateSample(
            string assetPath,
            ChallengeValidator validator)
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (json == null)
            {
                Debug.LogError($"Validation sample was not found: {assetPath}");
                return;
            }

            ChallengeSpec challenge = JsonUtility.FromJson<ChallengeSpec>(json.text);
            ValidationResult result = validator.Validate(challenge);
            string details = result.Errors.Count == 0
                ? string.Empty
                : "\n- " + string.Join("\n- ", result.Errors);
            Debug.Log(
                $"{json.name}: {result.BuildSummary()}{details}",
                json);
        }
    }
}
