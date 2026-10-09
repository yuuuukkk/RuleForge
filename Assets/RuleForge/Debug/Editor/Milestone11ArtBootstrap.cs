using System;
using RuleForge.Enemies;
using RuleForge.UI;
using RuleForge.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone11ArtBootstrap
    {
        private const string ArtRoot = "Assets/ThirdParty/Kenney/";
        private const string EnemyPrefabPath =
            "Assets/RuleForge/Enemies/Enemy.prefab";

        [MenuItem("RuleForge/Setup Milestone 11 Art Placeholders")]
        public static void SetupMilestone11ArtPlaceholders()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before running art setup.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Milestone10SceneBootstrap.SetupMilestone10AnalyticsAndShowcases();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            GameObject characterModel = Load<GameObject>(
                ArtRoot +
                "AnimatedCharactersSurvivors/Model/characterMedium.fbx");
            Texture2D survivorFemale = Load<Texture2D>(
                ArtRoot +
                "AnimatedCharactersSurvivors/Skins/survivorFemaleA.png");
            Texture2D zombieA = Load<Texture2D>(
                ArtRoot +
                "AnimatedCharactersSurvivors/Skins/zombieA.png");
            Texture2D zombieC = Load<Texture2D>(
                ArtRoot +
                "AnimatedCharactersSurvivors/Skins/zombieC.png");

            ConfigureEnemyArt(
                characterModel,
                survivorFemale,
                zombieA,
                zombieC);

            Scene scene = SceneManager.GetActiveScene();
            ConfigureWeaponArt(scene);
            ConfigureHudArt(scene);
            ConfigureEnvironmentArt(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "RuleForge Milestone 11 art setup complete. " +
                "Kenney environment, enemy skins, weapon models, HUD icons, " +
                "and crosshair are now bound. Runtime behavior was not started.");
        }

        private static void ConfigureEnemyArt(
            GameObject characterModel,
            Texture2D survivorFemale,
            Texture2D zombieA,
            Texture2D zombieC)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(
                EnemyPrefabPath);
            try
            {
                EnemyVisualController visual = GetOrAdd<EnemyVisualController>(
                    prefabRoot);
                visual.Configure(
                    characterModel,
                    new[]
                    {
                        new EnemyVisualSkin
                        {
                            enemyType = "Grunt",
                            texture = zombieA
                        },
                        new EnemyVisualSkin
                        {
                            enemyType = "Runner",
                            texture = zombieC
                        },
                        new EnemyVisualSkin
                        {
                            enemyType = "Tank",
                            texture = survivorFemale
                        }
                    },
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.one);

                MeshRenderer fallbackRenderer =
                    prefabRoot.GetComponent<MeshRenderer>();
                if (fallbackRenderer != null)
                {
                    fallbackRenderer.enabled = false;
                    EditorUtility.SetDirty(fallbackRenderer);
                }

                EditorUtility.SetDirty(visual);
                PrefabUtility.SaveAsPrefabAsset(
                    prefabRoot,
                    EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ConfigureWeaponArt(Scene scene)
        {
            WeaponLoadout loadout = Find<WeaponLoadout>(scene);
            Transform weaponVisual = FindDescendant(scene, "WeaponVisual");
            if (loadout == null || weaponVisual == null)
            {
                throw new MissingReferenceException(
                    "WeaponLoadout or WeaponVisual was not found.");
            }

            WeaponVisualController visual =
                GetOrAdd<WeaponVisualController>(weaponVisual.gameObject);
            visual.Configure(
                new[]
                {
                    CreateWeaponBinding(
                        "Assault Rifle",
                        "blaster-a.fbx",
                        0.38f),
                    CreateWeaponBinding(
                        "Shotgun",
                        "blaster-j.fbx",
                        0.42f),
                    CreateWeaponBinding(
                        "Sniper",
                        "blaster-r.fbx",
                        0.46f)
                });
            loadout.ConfigureArt(visual);

            MeshRenderer fallbackRenderer =
                weaponVisual.GetComponent<MeshRenderer>();
            if (fallbackRenderer != null)
            {
                fallbackRenderer.enabled = false;
                EditorUtility.SetDirty(fallbackRenderer);
            }

            EditorUtility.SetDirty(visual);
            EditorUtility.SetDirty(loadout);
        }

        private static WeaponVisualBinding CreateWeaponBinding(
            string weaponName,
            string fileName,
            float scale)
        {
            return new WeaponVisualBinding
            {
                weaponName = weaponName,
                modelPrefab = Load<GameObject>(
                    ArtRoot + "BlasterKit/Models/" + fileName),
                localPosition = Vector3.zero,
                localEulerAngles = Vector3.zero,
                localScale = Vector3.one * scale
            };
        }

        private static void ConfigureHudArt(Scene scene)
        {
            GameplayHud hud = Find<GameplayHud>(scene);
            if (hud == null)
            {
                throw new MissingReferenceException("GameplayHud was not found.");
            }

            hud.ConfigureArt(
                Load<Texture2D>(
                    ArtRoot + "CrosshairPack/Outline/crosshair-000.png"),
                Load<Texture2D>(ArtRoot + "GameIcons/White2x/target.png"),
                Load<Texture2D>(ArtRoot + "GameIcons/White2x/cross.png"),
                Load<Texture2D>(
                    ArtRoot + "UISciFi/Grey/bar_round_large.png"),
                Load<Texture2D>(
                    ArtRoot + "UISciFi/Red/bar_round_gloss_large.png"),
                Load<Texture2D>(
                    ArtRoot + "GameIcons/White2x/barsHorizontal.png"),
                Load<Texture2D>(
                    ArtRoot + "UISciFi/Extra/panel_glass.png"));
            hud.ConfigureThemeArt(
                Load<Texture2D>(
                    ArtRoot + "UISciFi/Extra/button_rectangle.png"),
                Load<Texture2D>(
                    ArtRoot + "UISciFi/Extra/button_rectangle_depth.png"),
                Load<Texture2D>(
                    ArtRoot + "UISciFi/Red/button_square_header_large_rectangle.png"),
                Load<Font>(ArtRoot + "UISciFi/Fonts/Kenney Future.ttf"));
            EditorUtility.SetDirty(hud);
        }

        private static void ConfigureEnvironmentArt(Scene scene)
        {
            Transform environment = FindDescendant(scene, "Environment");
            if (environment == null)
            {
                throw new MissingReferenceException("Environment was not found.");
            }

            Transform previous = environment.Find("ArtPlaceholder");
            if (previous != null)
            {
                Undo.DestroyObjectImmediate(previous.gameObject);
            }

            GameObject root = new GameObject("ArtPlaceholder");
            Undo.RegisterCreatedObjectUndo(root, "Create art placeholder root");
            root.transform.SetParent(environment, false);

            AddArtPrefab(
                root.transform,
                "Floor",
                "SpaceStationKit/Models/floor.fbx",
                new Vector3(0f, 0.01f, 0f),
                Vector3.zero,
                new Vector3(20f, 1f, 20f));
            AddArtPrefab(
                root.transform,
                "BackWall",
                "SpaceStationKit/Models/wall.fbx",
                new Vector3(0f, 1f, 9.9f),
                Vector3.zero,
                new Vector3(20f, 2f, 1f));
            AddArtPrefab(
                root.transform,
                "FrontWall",
                "SpaceStationKit/Models/wall.fbx",
                new Vector3(0f, 1f, -9.9f),
                Vector3.zero,
                new Vector3(20f, 2f, 1f));
            AddArtPrefab(
                root.transform,
                "RightWall",
                "SpaceStationKit/Models/wall.fbx",
                new Vector3(9.9f, 1f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(20f, 2f, 1f));
            AddArtPrefab(
                root.transform,
                "LeftWall",
                "SpaceStationKit/Models/wall.fbx",
                new Vector3(-9.9f, 1f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(20f, 2f, 1f));
            AddArtPrefab(
                root.transform,
                "EntryDoor",
                "SpaceStationKit/Models/door-double.fbx",
                new Vector3(0f, 1f, 9.8f),
                Vector3.zero,
                new Vector3(2f, 2f, 1f));
            AddArtPrefab(
                root.transform,
                "CoverContainer",
                "SpaceStationKit/Models/container.fbx",
                new Vector3(-4f, 0.5f, 0f),
                Vector3.zero,
                new Vector3(2f, 1.5f, 1f));
            AddArtPrefab(
                root.transform,
                "CoverTallContainer",
                "SpaceStationKit/Models/container-tall.fbx",
                new Vector3(4f, 0.75f, 0f),
                Vector3.zero,
                new Vector3(1.5f, 1.5f, 1.5f));
            AddArtPrefab(
                root.transform,
                "Console",
                "SpaceStationKit/Models/computer-screen.fbx",
                new Vector3(0f, 1f, -6f),
                Vector3.zero,
                new Vector3(2f, 2f, 2f));
        }

        private static void AddArtPrefab(
            Transform parent,
            string displayName,
            string assetPath,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            GameObject prefab = Load<GameObject>(ArtRoot + assetPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                parent) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate art asset: " + assetPath);
            }

            instance.name = "Art_" + displayName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
            instance.transform.localScale = localScale;
            GameObjectUtility.SetStaticEditorFlags(
                instance,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic);
            Undo.RegisterCreatedObjectUndo(instance, "Place art placeholder");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new MissingReferenceException(
                    "Art asset was not imported yet: " + path);
            }

            return asset;
        }

        private static T GetOrAdd<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                T component = roots[index].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
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
