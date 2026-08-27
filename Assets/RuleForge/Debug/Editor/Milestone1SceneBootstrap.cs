using System.Linq;
using RuleForge.Enemies;
using RuleForge.Player;
using RuleForge.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone1SceneBootstrap
    {
        private const string ArenaScenePath = "Assets/Scenes/Arena.unity";
        private const string EnemyPrefabPath = "Assets/RuleForge/Enemies/Enemy.prefab";

        [MenuItem("RuleForge/Setup Milestone 1 Sandbox")]
        public static void SetupMilestone1Sandbox()
        {
            Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
            GameObject arena = FindRoot(scene, "Arena");
            if (arena == null)
            {
                throw new MissingReferenceException("Arena root was not found.");
            }

            Transform playerSpawn = FindDescendant(arena.transform, "PlayerSpawn");
            Transform enemySpawnRoot = FindDescendant(arena.transform, "EnemySpawnPoints");
            Transform gameSystems = FindDescendant(arena.transform, "GameSystems");

            if (playerSpawn == null || enemySpawnRoot == null || gameSystems == null)
            {
                throw new MissingReferenceException(
                    "Arena requires PlayerSpawn, EnemySpawnPoints, and GameSystems.");
            }

            SetupLighting(arena.transform);
            SetupPlayer(arena.transform, playerSpawn);
            EnemyHealth enemyPrefab = GetOrCreateEnemyPrefab();
            SetupSpawner(gameSystems, enemySpawnRoot, enemyPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RuleForge Milestone 1 Arena setup complete.");
        }

        private static void SetupLighting(Transform arena)
        {
            Light existingLight = Object.FindObjectOfType<Light>();
            if (existingLight != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(arena, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
        }

        private static void SetupPlayer(Transform arena, Transform playerSpawn)
        {
            Transform existingPlayer = FindDescendant(arena, "Player");
            GameObject player = existingPlayer != null
                ? existingPlayer.gameObject
                : new GameObject("Player");

            player.transform.SetParent(arena, true);
            player.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            player.tag = "Player";

            CharacterController characterController =
                GetOrAddComponent<CharacterController>(player);
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            PlayerHealth playerHealth = GetOrAddComponent<PlayerHealth>(player);
            PlayerController playerController = GetOrAddComponent<PlayerController>(player);

            Transform cameraTransform = GetOrCreateChild(player.transform, "Main Camera");
            cameraTransform.localPosition = new Vector3(0f, 1.6f, 0f);
            cameraTransform.localRotation = Quaternion.identity;
            cameraTransform.gameObject.tag = "MainCamera";
            Camera playerCamera = GetOrAddComponent<Camera>(cameraTransform.gameObject);
            GetOrAddComponent<AudioListener>(cameraTransform.gameObject);
            playerController.SetView(cameraTransform);

            Transform weaponTransform = GetOrCreateChild(cameraTransform, "Weapon");
            weaponTransform.localPosition = Vector3.zero;
            weaponTransform.localRotation = Quaternion.identity;
            WeaponController weapon = GetOrAddComponent<WeaponController>(weaponTransform.gameObject);
            weapon.SetAimCamera(playerCamera);

            Transform weaponVisual = GetOrCreateChild(weaponTransform, "WeaponVisual");
            if (weaponVisual.GetComponent<MeshFilter>() == null)
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "WeaponVisual";
                visual.transform.SetParent(weaponTransform, false);
                visual.transform.localPosition = new Vector3(0.25f, -0.25f, 0.55f);
                visual.transform.localScale = new Vector3(0.16f, 0.14f, 0.65f);
                Object.DestroyImmediate(weaponVisual.gameObject);

                Collider visualCollider = visual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    Object.DestroyImmediate(visualCollider);
                }
            }

            EditorUtility.SetDirty(playerHealth);
            EditorUtility.SetDirty(playerController);
            EditorUtility.SetDirty(weapon);
        }

        private static EnemyHealth GetOrCreateEnemyPrefab()
        {
            EnemyHealth existingPrefab = AssetDatabase.LoadAssetAtPath<EnemyHealth>(EnemyPrefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "Enemy";

            Collider primitiveCollider = enemyObject.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Object.DestroyImmediate(primitiveCollider);
            }

            CharacterController controller = enemyObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = Vector3.up;

            enemyObject.AddComponent<EnemyController>();
            enemyObject.AddComponent<EnemyHealth>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemyObject, EnemyPrefabPath);
            Object.DestroyImmediate(enemyObject);
            return prefab.GetComponent<EnemyHealth>();
        }

        private static void SetupSpawner(
            Transform gameSystems,
            Transform spawnRoot,
            EnemyHealth enemyPrefab)
        {
            Transform spawnerTransform = GetOrCreateChild(gameSystems, "EnemySpawner");
            EnemySpawner spawner = GetOrAddComponent<EnemySpawner>(spawnerTransform.gameObject);

            Transform[] spawnPoints = spawnRoot
                .Cast<Transform>()
                .Where(point => point.name.StartsWith("Spawn_"))
                .OrderBy(point => point.name)
                .ToArray();

            spawner.Configure(enemyPrefab, spawnPoints);
            EditorUtility.SetDirty(spawner);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
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

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
