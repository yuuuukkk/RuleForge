using RuleForge.Rules;
using RuleForge.Runtime.Services;
using RuleForge.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone4SceneBootstrap
    {
        private const string ShowcasePath =
            "Assets/RuleForge/Challenge/Examples/rule_expression_showcase.json";

        [MenuItem("RuleForge/Setup Milestone 4 Rule Expressions")]
        public static void SetupMilestone4RuleExpressions()
        {
            Milestone3SceneBootstrap.SetupMilestone3RuleEngine();

            Scene scene = SceneManager.GetActiveScene();
            Transform runtimeRoot = FindDescendant(scene, "RuleRuntime");
            TextAsset showcase =
                AssetDatabase.LoadAssetAtPath<TextAsset>(ShowcasePath);
            if (runtimeRoot == null || showcase == null)
            {
                throw new MissingReferenceException(
                    "RuleRuntime or the Milestone 4 showcase JSON was not found.");
            }

            PlayerRuntimeService playerService =
                runtimeRoot.GetComponent<PlayerRuntimeService>();
            EnemyRuntimeService enemyService =
                runtimeRoot.GetComponent<EnemyRuntimeService>();
            WeaponRuntimeService weaponService =
                runtimeRoot.GetComponent<WeaponRuntimeService>();
            SpawnRuntimeService spawnService =
                runtimeRoot.GetComponent<SpawnRuntimeService>();
            TimeRuntimeService timeService =
                runtimeRoot.GetComponent<TimeRuntimeService>();
            RuleEngine ruleEngine = runtimeRoot.GetComponent<RuleEngine>();
            WeaponRuntimeStats weaponStats =
                Object.FindObjectOfType<WeaponRuntimeStats>();
            WeaponController weaponController = weaponStats != null
                ? weaponStats.GetComponent<WeaponController>()
                : null;

            if (playerService == null || enemyService == null ||
                weaponService == null || spawnService == null ||
                timeService == null || ruleEngine == null ||
                weaponStats == null || weaponController == null)
            {
                throw new MissingReferenceException(
                    "Milestone 4 requires all Milestone 3 runtime services and the weapon.");
            }

            weaponStats.RefreshBaseValues();
            weaponService.Configure(weaponStats, weaponController);
            ruleEngine.Configure(
                showcase,
                playerService,
                enemyService,
                weaponService,
                spawnService,
                timeService);

            EditorUtility.SetDirty(weaponStats);
            EditorUtility.SetDirty(weaponService);
            EditorUtility.SetDirty(ruleEngine);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RuleForge Milestone 4 Rule Expressions setup complete.");
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
