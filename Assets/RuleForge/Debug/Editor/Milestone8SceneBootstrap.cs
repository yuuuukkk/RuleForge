using RuleForge.AI;
using RuleForge.Rules;
using RuleForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuleForge.Editor
{
    public static class Milestone8SceneBootstrap
    {
        private const string MockChallengePath =
            "Assets/RuleForge/Challenge/Examples/rule_expression_showcase.json";
        private const string MockPatchPath =
            "Assets/RuleForge/Challenge/Examples/mock_penalty_patch.json";

        [MenuItem("RuleForge/Setup Milestone 8 AI Gameplay")]
        public static void SetupMilestone8AIGameplay()
        {
            Milestone7SceneBootstrap.SetupMilestone7CreatorUI();

            Scene scene = SceneManager.GetActiveScene();
            Transform runtimeRoot = FindDescendant(scene, "RuleRuntime");
            if (runtimeRoot == null)
            {
                throw new MissingReferenceException("RuleRuntime was not found.");
            }

            RuleEngine ruleEngine = runtimeRoot.GetComponent<RuleEngine>();
            ChallengeCreatorPanel creator =
                runtimeRoot.GetComponent<ChallengeCreatorPanel>();
            TextAsset mockChallenge =
                AssetDatabase.LoadAssetAtPath<TextAsset>(MockChallengePath);
            TextAsset mockPatch =
                AssetDatabase.LoadAssetAtPath<TextAsset>(MockPatchPath);
            if (ruleEngine == null || creator == null ||
                mockChallenge == null || mockPatch == null)
            {
                throw new MissingReferenceException(
                    "Milestone 8 prerequisites or Mock AI JSON assets are missing.");
            }

            MockAIGameplayService mock =
                GetOrAdd<MockAIGameplayService>(runtimeRoot.gameObject);
            OpenAIResponsesGameplayService openAI =
                GetOrAdd<OpenAIResponsesGameplayService>(runtimeRoot.gameObject);
            AIGameplayController controller =
                GetOrAdd<AIGameplayController>(runtimeRoot.gameObject);

            mock.Configure(mockChallenge, mockPatch);
            controller.Configure(ruleEngine, mock, openAI);
            creator.ConfigureAI(controller);

            EditorUtility.SetDirty(mock);
            EditorUtility.SetDirty(openAI);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(creator);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "RuleForge Milestone 8 AI Gameplay setup complete. " +
                "Mock AI is the default provider; F2 opens Creator Preview.");
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
