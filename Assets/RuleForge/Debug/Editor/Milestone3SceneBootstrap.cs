using RuleForge.Enemies;
using RuleForge.Player;
using RuleForge.Rules;
using RuleForge.Runtime.Services;
using RuleForge.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone3SceneBootstrap
    {
        private const string ArenaScenePath = "Assets/Scenes/Arena.unity";
        private const string BloodPactPath =
            "Assets/RuleForge/Challenge/Examples/blood_pact.json";

        [MenuItem("RuleForge/Setup Milestone 3 Rule Engine")]
        public static void SetupMilestone3RuleEngine()
        {
            Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
            GameObject arena = FindRoot(scene, "Arena");
            if (arena == null)
            {
                throw new MissingReferenceException("Arena root was not found.");
            }

            Transform gameSystems = FindDescendant(arena.transform, "GameSystems");
            PlayerRuntimeStats playerStats =
                FindDescendant(arena.transform, "Player")
                    ?.GetComponent<PlayerRuntimeStats>();
            PlayerHealth playerHealth = playerStats != null
                ? playerStats.GetComponent<PlayerHealth>()
                : null;
            WeaponRuntimeStats weaponStats =
                FindDescendant(arena.transform, "Weapon")
                    ?.GetComponent<WeaponRuntimeStats>();
            EnemySpawner enemySpawner =
                FindDescendant(arena.transform, "EnemySpawner")
                    ?.GetComponent<EnemySpawner>();
            TextAsset bloodPact = AssetDatabase.LoadAssetAtPath<TextAsset>(BloodPactPath);

            if (gameSystems == null || playerStats == null || playerHealth == null ||
                weaponStats == null || enemySpawner == null || bloodPact == null)
            {
                throw new MissingReferenceException(
                    "Milestone 2 setup and blood_pact.json are required before Milestone 3 setup.");
            }

            Transform runtimeRoot = FindDirectChild(gameSystems, "RuleRuntime");
            if (runtimeRoot == null)
            {
                GameObject runtimeObject = new GameObject("RuleRuntime");
                runtimeObject.transform.SetParent(gameSystems, false);
                runtimeRoot = runtimeObject.transform;
            }

            PlayerRuntimeService playerService =
                GetOrAddComponent<PlayerRuntimeService>(runtimeRoot.gameObject);
            EnemyRuntimeService enemyService =
                GetOrAddComponent<EnemyRuntimeService>(runtimeRoot.gameObject);
            WeaponRuntimeService weaponService =
                GetOrAddComponent<WeaponRuntimeService>(runtimeRoot.gameObject);
            SpawnRuntimeService spawnService =
                GetOrAddComponent<SpawnRuntimeService>(runtimeRoot.gameObject);
            TimeRuntimeService timeService =
                GetOrAddComponent<TimeRuntimeService>(runtimeRoot.gameObject);
            RuleEngine ruleEngine =
                GetOrAddComponent<RuleEngine>(runtimeRoot.gameObject);

            playerService.Configure(playerStats, playerHealth);
            enemyService.Configure(enemySpawner);
            weaponService.Configure(weaponStats);
            spawnService.Configure(enemySpawner);
            ruleEngine.Configure(
                bloodPact,
                playerService,
                enemyService,
                weaponService,
                spawnService,
                timeService);

            EditorUtility.SetDirty(playerService);
            EditorUtility.SetDirty(enemyService);
            EditorUtility.SetDirty(weaponService);
            EditorUtility.SetDirty(spawnService);
            EditorUtility.SetDirty(timeService);
            EditorUtility.SetDirty(ruleEngine);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RuleForge Milestone 3 Rule Engine setup complete.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == name)
                {
                    return roots[index];
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

        private static Transform FindDirectChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
