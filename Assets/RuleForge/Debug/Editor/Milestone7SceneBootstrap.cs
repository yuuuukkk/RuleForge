using RuleForge.Config;
using RuleForge.Rules;
using RuleForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone7SceneBootstrap
    {
        private const string BalanceConfigPath =
            "Assets/RuleForge/Config/Defaults/GameplayBalanceConfig.asset";
        private const string EffectCatalogPath =
            "Assets/RuleForge/Config/Defaults/EffectCatalog.asset";

        [MenuItem("RuleForge/Setup Milestone 7 Creator UI")]
        public static void SetupMilestone7CreatorUI()
        {
            Milestone6SceneBootstrap.SetupMilestone6Validator();

            Scene scene = SceneManager.GetActiveScene();
            Transform runtimeRoot = FindDescendant(scene, "RuleRuntime");
            GameplayBalanceConfig balanceConfig =
                AssetDatabase.LoadAssetAtPath<GameplayBalanceConfig>(
                    BalanceConfigPath);
            EffectCatalog effectCatalog =
                AssetDatabase.LoadAssetAtPath<EffectCatalog>(EffectCatalogPath);
            if (runtimeRoot == null ||
                balanceConfig == null ||
                effectCatalog == null)
            {
                throw new MissingReferenceException(
                    "RuleRuntime, GameplayBalanceConfig, or EffectCatalog was not found.");
            }

            ConfigureCreatorData(balanceConfig, effectCatalog);
            RuleEngine ruleEngine = runtimeRoot.GetComponent<RuleEngine>();
            if (ruleEngine == null)
            {
                throw new MissingReferenceException("RuleRuntime has no RuleEngine.");
            }

            ChallengeCreatorPanel creatorPanel =
                runtimeRoot.GetComponent<ChallengeCreatorPanel>();
            if (creatorPanel == null)
            {
                creatorPanel =
                    runtimeRoot.gameObject.AddComponent<ChallengeCreatorPanel>();
            }

            creatorPanel.Configure(ruleEngine);
            EditorUtility.SetDirty(balanceConfig);
            EditorUtility.SetDirty(effectCatalog);
            EditorUtility.SetDirty(creatorPanel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RuleForge Milestone 7 Creator UI setup complete.");
        }

        private static void ConfigureCreatorData(
            GameplayBalanceConfig balanceConfig,
            EffectCatalog effectCatalog)
        {
            balanceConfig.ConfigureStrengthRange(0f, 2f);

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
            effectCatalog.ConfigureCreatorTemplate(
                "EnemyMoveSpeed",
                true,
                "StatModifier",
                "Enemy",
                "EnemyMoveSpeed",
                "AddPercent",
                0.08f,
                string.Empty,
                "Stack",
                10);
            effectCatalog.ConfigureCreatorTemplate(
                "PlayerDamageFromMissingHealth",
                false,
                "StatModifier",
                "Weapon",
                "WeaponDamage",
                "AddPercent",
                0f,
                string.Empty,
                "None",
                1);
            effectCatalog.ConfigureCreatorTemplate(
                "SpawnRunner",
                true,
                "SpawnEnemy",
                "Enemy",
                string.Empty,
                string.Empty,
                0f,
                "Runner",
                "None",
                1);
            effectCatalog.ConfigureCreatorTemplate(
                "GiveAmmo",
                true,
                "GiveAmmo",
                "Weapon",
                string.Empty,
                string.Empty,
                10f,
                string.Empty,
                "None",
                1);
        }

        private static Transform FindDescendant(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Transform match = FindDescendant(roots[index].transform, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform match = FindDescendant(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
