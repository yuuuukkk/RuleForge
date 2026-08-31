using System;
using RuleForge.AI;
using RuleForge.Analytics;
using RuleForge.Config;
using RuleForge.Rules;
using RuleForge.Runtime.Goals;
using RuleForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone10SceneBootstrap
    {
        private const string Defaults = "Assets/RuleForge/Config/Defaults/";
        private const string Examples =
            "Assets/RuleForge/Challenge/Examples/";

        [MenuItem("RuleForge/Setup Milestone 10 Analytics & Showcases")]
        public static void SetupMilestone10AnalyticsAndShowcases()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before running Milestone 10 setup.");
            }

            Milestone9SceneBootstrap.SetupMilestone9FPSContent();

            Scene scene = SceneManager.GetActiveScene();
            Transform runtimeRoot = FindDescendant(scene, "RuleRuntime");
            if (runtimeRoot == null)
            {
                throw new MissingReferenceException("RuleRuntime was not found.");
            }

            RuleEngine ruleEngine = runtimeRoot.GetComponent<RuleEngine>();
            ChallengeGoalController goals =
                runtimeRoot.GetComponent<ChallengeGoalController>();
            AIGameplayController aiController =
                runtimeRoot.GetComponent<AIGameplayController>();
            GameplayBalanceConfig balance = Load<GameplayBalanceConfig>(
                Defaults + "GameplayBalanceConfig.asset");
            EffectCatalog catalog = Load<EffectCatalog>(
                Defaults + "EffectCatalog.asset");
            if (ruleEngine == null || goals == null || aiController == null)
            {
                throw new MissingReferenceException(
                    "Milestone 10 requires the completed Milestone 9 setup.");
            }

            ConfigureLastStandEffect(balance, catalog);
            ruleEngine.ConfigureTuning(balance, catalog);

            AnalyticsRecorder recorder =
                GetOrAdd<AnalyticsRecorder>(runtimeRoot.gameObject);
            recorder.Configure(ruleEngine, goals, aiController);
            AnalyticsPanel panel =
                GetOrAdd<AnalyticsPanel>(runtimeRoot.gameObject);
            panel.Configure(recorder);

            EditorUtility.SetDirty(balance);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(ruleEngine);
            EditorUtility.SetDirty(recorder);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "RuleForge Milestone 10 setup complete. F3 opens the " +
                "real-data Analytics panel. No sample results were generated.");
        }

        [MenuItem("RuleForge/Showcases/Load 01 Blood Pact")]
        public static void LoadBloodPact()
        {
            LoadShowcase(Examples + "blood_pact.json", "Blood Pact");
        }

        [MenuItem("RuleForge/Showcases/Load 02 Last Stand")]
        public static void LoadLastStand()
        {
            LoadShowcase(
                Examples + "showcase_last_stand.json",
                "Last Stand");
        }

        [MenuItem("RuleForge/Showcases/Load 03 Reload Gamble")]
        public static void LoadReloadGamble()
        {
            LoadShowcase(
                Examples + "showcase_reload_gamble.json",
                "Reload Gamble");
        }

        private static void LoadShowcase(string path, string displayName)
        {
            SetupMilestone10AnalyticsAndShowcases();
            TextAsset challenge = Load<TextAsset>(path);
            RuleEngine ruleEngine = UnityEngine.Object.FindObjectOfType<RuleEngine>();
            if (ruleEngine == null)
            {
                throw new MissingReferenceException("RuleEngine was not found.");
            }

            SerializedObject serializedEngine = new SerializedObject(ruleEngine);
            SerializedProperty challengeProperty =
                serializedEngine.FindProperty("challengeJson");
            challengeProperty.objectReferenceValue = challenge;
            serializedEngine.ApplyModifiedPropertiesWithoutUndo();

            Scene scene = SceneManager.GetActiveScene();
            EditorUtility.SetDirty(ruleEngine);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"RuleForge Showcase loaded: {displayName}. Enter Play Mode " +
                "to create a real gameplay analytics session.");
        }

        private static void ConfigureLastStandEffect(
            GameplayBalanceConfig balance,
            EffectCatalog catalog)
        {
            const string effectId = "PlayerMoveSpeedFromMissingHealth";
            catalog.EnsureDefinition(
                effectId,
                "Missing-Health Move Speed",
                EffectPolarity.Penalty);
            catalog.ConfigureBalanceWeight(effectId, 12f);
            catalog.ConfigureCreatorTemplate(
                effectId,
                false,
                "StatModifier",
                "Player",
                "PlayerMoveSpeed",
                "AddPercent",
                0f,
                string.Empty,
                "None",
                1);
            balance.EnsureEffectValueLimit(effectId, -0.75f, 0f);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
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
