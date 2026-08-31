using RuleForge.Config;
using RuleForge.Enemies;
using RuleForge.Player;
using RuleForge.Rules;
using RuleForge.Runtime.Goals;
using RuleForge.Runtime.Services;
using RuleForge.UI;
using RuleForge.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone9SceneBootstrap
    {
        private const string Defaults = "Assets/RuleForge/Config/Defaults/";
        private const string EnemyPrefabPath =
            "Assets/RuleForge/Enemies/Enemy.prefab";

        [MenuItem("RuleForge/Setup Milestone 9 FPS Content")]
        public static void SetupMilestone9FPSContent()
        {
            Milestone8SceneBootstrap.SetupMilestone8AIGameplay();

            WeaponConfig assault = Load<WeaponConfig>(
                Defaults + "AssaultRifleConfig.asset");
            WeaponConfig shotgun = Load<WeaponConfig>(
                Defaults + "ShotgunConfig.asset");
            WeaponConfig sniper = Load<WeaponConfig>(
                Defaults + "SniperConfig.asset");
            EnemyConfig grunt = Load<EnemyConfig>(
                Defaults + "GruntConfig.asset");
            EnemyConfig runner = Load<EnemyConfig>(
                Defaults + "RunnerConfig.asset");
            EnemyConfig tank = Load<EnemyConfig>(
                Defaults + "TankConfig.asset");
            GameplayBalanceConfig balance = Load<GameplayBalanceConfig>(
                Defaults + "GameplayBalanceConfig.asset");
            EffectCatalog catalog = Load<EffectCatalog>(
                Defaults + "EffectCatalog.asset");

            ConfigureProfiles(assault, shotgun, sniper, grunt, runner, tank);
            ConfigureEnemyPrefab(grunt);
            ConfigureRuleVocabulary(balance, catalog);

            Scene scene = SceneManager.GetActiveScene();
            Transform runtimeRoot = FindDescendant(scene, "RuleRuntime");
            WeaponRuntimeStats weaponStats =
                Object.FindObjectOfType<WeaponRuntimeStats>();
            WeaponController weaponController = weaponStats != null
                ? weaponStats.GetComponent<WeaponController>()
                : null;
            EnemySpawner spawner = Object.FindObjectOfType<EnemySpawner>();
            PlayerHealth playerHealth = Object.FindObjectOfType<PlayerHealth>();
            EnemyHealth enemyPrefab =
                AssetDatabase.LoadAssetAtPath<EnemyHealth>(EnemyPrefabPath);
            if (runtimeRoot == null || weaponStats == null ||
                weaponController == null || spawner == null ||
                playerHealth == null || enemyPrefab == null)
            {
                throw new MissingReferenceException(
                    "Milestone 9 requires the completed Milestone 8 Arena setup.");
            }

            Transform weaponVisual = FindDescendant(
                weaponStats.transform,
                "WeaponVisual");
            WeaponLoadout loadout =
                GetOrAdd<WeaponLoadout>(weaponStats.gameObject);
            loadout.Configure(
                weaponStats,
                weaponController,
                weaponVisual,
                new[] { assault, shotgun, sniper });
            weaponStats.Configure(assault);

            Transform[] spawnPoints = GetSpawnPoints(scene);
            spawner.ConfigureRoster(
                enemyPrefab,
                spawnPoints,
                new[] { grunt, runner, tank });

            EnemyRuntimeService enemyService =
                runtimeRoot.GetComponent<EnemyRuntimeService>();
            SpawnRuntimeService spawnService =
                runtimeRoot.GetComponent<SpawnRuntimeService>();
            WeaponRuntimeService weaponService =
                runtimeRoot.GetComponent<WeaponRuntimeService>();
            RuleEngine ruleEngine = runtimeRoot.GetComponent<RuleEngine>();
            if (enemyService == null || spawnService == null ||
                weaponService == null || ruleEngine == null)
            {
                throw new MissingReferenceException(
                    "Milestone 9 runtime services were not found.");
            }

            enemyService.Configure(spawner);
            spawnService.Configure(spawner);
            weaponService.Configure(weaponStats, weaponController);

            ChallengeGoalController goals =
                GetOrAdd<ChallengeGoalController>(runtimeRoot.gameObject);
            goals.Configure(ruleEngine, playerHealth, loadout);
            GameplayHud hud = GetOrAdd<GameplayHud>(runtimeRoot.gameObject);
            hud.Configure(ruleEngine, goals, loadout, weaponController);

            EditorUtility.SetDirty(loadout);
            EditorUtility.SetDirty(weaponStats);
            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(enemyService);
            EditorUtility.SetDirty(spawnService);
            EditorUtility.SetDirty(weaponService);
            EditorUtility.SetDirty(goals);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "RuleForge Milestone 9 FPS Content setup complete: " +
                "3 weapons, 3 enemies, Goals, HUD, and Rule Feedback.");
        }

        private static void ConfigureProfiles(
            WeaponConfig assault,
            WeaponConfig shotgun,
            WeaponConfig sniper,
            EnemyConfig grunt,
            EnemyConfig runner,
            EnemyConfig tank)
        {
            assault.ConfigureProfile(
                "Assault Rifle", WeaponArchetype.AssaultRifle,
                20f, 100f, 8f, 30, 90, 1.8f, 1, 0.4f, true,
                new Color(0.15f, 0.55f, 1f),
                new Vector3(0.18f, 0.12f, 0.7f));
            shotgun.ConfigureProfile(
                "Shotgun", WeaponArchetype.Shotgun,
                12f, 45f, 1.2f, 8, 32, 2.2f, 8, 6f, false,
                new Color(1f, 0.55f, 0.12f),
                new Vector3(0.24f, 0.16f, 0.58f));
            sniper.ConfigureProfile(
                "Sniper", WeaponArchetype.Sniper,
                90f, 200f, 0.8f, 5, 20, 2.5f, 1, 0f, false,
                new Color(0.65f, 0.35f, 1f),
                new Vector3(0.14f, 0.1f, 0.95f));

            grunt.ConfigureProfile(
                "Grunt", 50f, 3f, 1.5f, 10f, 10f, 1f,
                3, 2f, new Color(0.85f, 0.25f, 0.2f), 1f);
            runner.ConfigureProfile(
                "Runner", 30f, 5f, 1.2f, 14f, 8f, 1.4f,
                3, 1.5f, new Color(1f, 0.75f, 0.1f), 0.8f);
            tank.ConfigureProfile(
                "Tank", 150f, 1.7f, 1.8f, 7f, 20f, 0.65f,
                3, 3f, new Color(0.25f, 0.8f, 0.35f), 1.35f);

            EditorUtility.SetDirty(assault);
            EditorUtility.SetDirty(shotgun);
            EditorUtility.SetDirty(sniper);
            EditorUtility.SetDirty(grunt);
            EditorUtility.SetDirty(runner);
            EditorUtility.SetDirty(tank);
        }

        private static void ConfigureRuleVocabulary(
            GameplayBalanceConfig balance,
            EffectCatalog catalog)
        {
            catalog.EnsureDefinition(
                "SpawnGrunt", "Spawn Grunt", EffectPolarity.Penalty);
            catalog.EnsureDefinition(
                "SpawnTank", "Spawn Tank", EffectPolarity.Penalty);
            catalog.ConfigureBalanceWeight("SpawnGrunt", 10f);
            catalog.ConfigureBalanceWeight("SpawnTank", 20f);
            catalog.ConfigureCreatorTemplate(
                "SpawnGrunt", true, "SpawnEnemy", "Enemy", string.Empty,
                string.Empty, 0f, "Grunt", "None", 1);
            catalog.ConfigureCreatorTemplate(
                "SpawnTank", true, "SpawnEnemy", "Enemy", string.Empty,
                string.Empty, 0f, "Tank", "None", 1);
            balance.EnsureEffectValueLimit("SpawnGrunt", 0f, 0f);
            balance.EnsureEffectValueLimit("SpawnTank", 0f, 0f);
            EditorUtility.SetDirty(balance);
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureEnemyPrefab(EnemyConfig grunt)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                EnemyRuntimeStats stats = GetOrAdd<EnemyRuntimeStats>(root);
                EnemyRuntimeIdentity identity = GetOrAdd<EnemyRuntimeIdentity>(root);
                stats.Configure(grunt);
                identity.Configure("Grunt");
                EditorUtility.SetDirty(stats);
                EditorUtility.SetDirty(identity);
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform[] GetSpawnPoints(Scene scene)
        {
            Transform root = FindDescendant(scene, "EnemySpawnPoints");
            if (root == null)
            {
                throw new MissingReferenceException(
                    "EnemySpawnPoints was not found.");
            }

            Transform[] points = new Transform[root.childCount];
            for (int index = 0; index < root.childCount; index++)
            {
                points[index] = root.GetChild(index);
            }

            return points;
        }

        private static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new MissingReferenceException($"Asset not found: {path}");
            }

            return asset;
        }

        private static T GetOrAdd<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
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
