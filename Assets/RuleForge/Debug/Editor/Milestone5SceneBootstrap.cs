using RuleForge.Config;
using RuleForge.Debugging;
using RuleForge.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone5SceneBootstrap
    {
        private const string BalanceConfigPath =
            "Assets/RuleForge/Config/Defaults/GameplayBalanceConfig.asset";
        private const string EffectCatalogPath =
            "Assets/RuleForge/Config/Defaults/EffectCatalog.asset";

        [MenuItem("RuleForge/Setup Milestone 5 Tuning Panel")]
        public static void SetupMilestone5TuningPanel()
        {
            Milestone4SceneBootstrap.SetupMilestone4RuleExpressions();

            Scene scene = SceneManager.GetActiveScene();
            Transform runtimeRoot = FindDescendant(scene, "RuleRuntime");
            GameplayBalanceConfig balanceConfig =
                AssetDatabase.LoadAssetAtPath<GameplayBalanceConfig>(BalanceConfigPath);
            EffectCatalog effectCatalog =
                AssetDatabase.LoadAssetAtPath<EffectCatalog>(EffectCatalogPath);
            if (runtimeRoot == null || balanceConfig == null || effectCatalog == null)
            {
                throw new MissingReferenceException(
                    "RuleRuntime, GameplayBalanceConfig, or EffectCatalog was not found.");
            }

            EnsureShowcaseDefinitions(effectCatalog);
            RuleEngine ruleEngine = runtimeRoot.GetComponent<RuleEngine>();
            if (ruleEngine == null)
            {
                throw new MissingReferenceException("RuleRuntime has no RuleEngine.");
            }

            RuntimeTuningPanel tuningPanel =
                runtimeRoot.GetComponent<RuntimeTuningPanel>();
            if (tuningPanel == null)
            {
                tuningPanel = runtimeRoot.gameObject.AddComponent<RuntimeTuningPanel>();
            }

            ruleEngine.ConfigureTuning(balanceConfig, effectCatalog);
            tuningPanel.Configure(ruleEngine);

            EditorUtility.SetDirty(effectCatalog);
            EditorUtility.SetDirty(ruleEngine);
            EditorUtility.SetDirty(tuningPanel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RuleForge Milestone 5 Tuning Panel setup complete.");
        }

        private static void EnsureShowcaseDefinitions(EffectCatalog catalog)
        {
            catalog.EnsureDefinition(
                "PlayerDamage",
                "Player Damage",
                EffectPolarity.Reward);
            catalog.EnsureDefinition(
                "EnemyMoveSpeed",
                "Enemy Move Speed",
                EffectPolarity.Penalty);
            catalog.EnsureDefinition(
                "PlayerDamageFromMissingHealth",
                "Missing-Health Damage",
                EffectPolarity.Reward);
            catalog.EnsureDefinition(
                "SpawnRunner",
                "Spawn Runner",
                EffectPolarity.Penalty);
            catalog.EnsureDefinition(
                "GiveAmmo",
                "Give Ammo",
                EffectPolarity.Reward);
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
