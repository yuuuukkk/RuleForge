using System.Linq;
using RuleForge.Config;
using RuleForge.Enemies;
using RuleForge.Player;
using RuleForge.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone2SceneBootstrap
    {
        private const string ArenaScenePath = "Assets/Scenes/Arena.unity";
        private const string EnemyPrefabPath = "Assets/RuleForge/Enemies/Enemy.prefab";
        private const string DefaultsFolder = "Assets/RuleForge/Config/Defaults";
        private const string PlayerConfigPath = DefaultsFolder + "/PlayerConfig.asset";
        private const string WeaponConfigPath = DefaultsFolder + "/WeaponConfig.asset";
        private const string EnemyConfigPath = DefaultsFolder + "/EnemyConfig.asset";
        private const string BalanceConfigPath =
            DefaultsFolder + "/GameplayBalanceConfig.asset";

        [MenuItem("RuleForge/Setup Milestone 2 Runtime Stats")]
        public static void SetupMilestone2RuntimeStats()
        {
            EnsureDefaultsFolder();
            PlayerConfig playerConfig = GetOrCreateAsset<PlayerConfig>(PlayerConfigPath);
            WeaponConfig weaponConfig = GetOrCreateAsset<WeaponConfig>(WeaponConfigPath);
            EnemyConfig enemyConfig = GetOrCreateAsset<EnemyConfig>(EnemyConfigPath);
            GetOrCreateAsset<GameplayBalanceConfig>(BalanceConfigPath);

            EnemyHealth enemyPrefab = ConfigureEnemyPrefab(enemyConfig);
            ConfigureArena(playerConfig, weaponConfig, enemyPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("RuleForge Milestone 2 Runtime Stats setup complete.");
        }

        private static EnemyHealth ConfigureEnemyPrefab(EnemyConfig enemyConfig)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                EnemyRuntimeStats runtimeStats =
                    GetOrAddComponent<EnemyRuntimeStats>(prefabRoot);
                runtimeStats.Configure(enemyConfig);
                EditorUtility.SetDirty(runtimeStats);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            return AssetDatabase.LoadAssetAtPath<EnemyHealth>(EnemyPrefabPath);
        }

        private static void ConfigureArena(
            PlayerConfig playerConfig,
            WeaponConfig weaponConfig,
            EnemyHealth enemyPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
            GameObject arena = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == "Arena");
            if (arena == null)
            {
                throw new MissingReferenceException("Arena root was not found.");
            }

            Transform player = FindDescendant(arena.transform, "Player");
            Transform weapon = FindDescendant(arena.transform, "Weapon");
            Transform spawnerTransform = FindDescendant(arena.transform, "EnemySpawner");
            Transform enemySpawnRoot = FindDescendant(arena.transform, "EnemySpawnPoints");
            if (player == null || weapon == null || spawnerTransform == null ||
                enemySpawnRoot == null)
            {
                throw new MissingReferenceException(
                    "Arena must be initialized with the Milestone 1 sandbox first.");
            }

            PlayerRuntimeStats playerStats =
                GetOrAddComponent<PlayerRuntimeStats>(player.gameObject);
            playerStats.Configure(playerConfig);

            WeaponRuntimeStats weaponStats =
                GetOrAddComponent<WeaponRuntimeStats>(weapon.gameObject);
            weaponStats.Configure(weaponConfig);

            Transform[] spawnPoints = enemySpawnRoot
                .Cast<Transform>()
                .Where(point => point.name.StartsWith("Spawn_"))
                .OrderBy(point => point.name)
                .ToArray();
            EnemySpawner spawner = spawnerTransform.GetComponent<EnemySpawner>();
            spawner.Configure(enemyPrefab, spawnPoints);

            EditorUtility.SetDirty(playerStats);
            EditorUtility.SetDirty(weaponStats);
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDefaultsFolder()
        {
            if (!AssetDatabase.IsValidFolder(DefaultsFolder))
            {
                AssetDatabase.CreateFolder("Assets/RuleForge/Config", "Defaults");
            }
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

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
