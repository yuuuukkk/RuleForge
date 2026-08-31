using System;
using System.Collections;
using RuleForge.DSL;
using UnityEngine;

namespace RuleForge.AI
{
    [DisallowMultipleComponent]
    public sealed class MockAIGameplayService : MonoBehaviour, IAIGameplayService
    {
        [SerializeField] private TextAsset generatedChallengeJson;
        [SerializeField] private TextAsset modificationPatchJson;

        public string ProviderName => "Mock AI (fixed offline sample)";
        public AIProviderKind ProviderKind => AIProviderKind.Mock;
        public bool IsConfigured =>
            generatedChallengeJson != null && modificationPatchJson != null;
        public bool IsBenchmarkEligible => false;

        public void Configure(TextAsset challengeJson, TextAsset patchJson)
        {
            generatedChallengeJson = challengeJson;
            modificationPatchJson = patchJson;
        }

        public IEnumerator GenerateChallenge(
            AIChallengeGenerationRequest request,
            Action<AIGameplayResult<ChallengeSpec>> onComplete)
        {
            yield return null;
            onComplete?.Invoke(Parse<ChallengeSpec>(generatedChallengeJson));
        }

        public IEnumerator ModifyChallenge(
            AIChallengeModificationRequest request,
            Action<AIGameplayResult<ChallengePatch>> onComplete)
        {
            yield return null;
            onComplete?.Invoke(Parse<ChallengePatch>(modificationPatchJson));
        }

        private static AIGameplayResult<T> Parse<T>(TextAsset jsonAsset)
            where T : class
        {
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                return AIGameplayResult<T>.Failed(
                    "Mock AI sample JSON is not configured.");
            }

            try
            {
                T value = JsonUtility.FromJson<T>(jsonAsset.text);
                return AIGameplayResult<T>.Succeeded(value);
            }
            catch (ArgumentException exception)
            {
                return AIGameplayResult<T>.Failed(
                    $"Mock AI JSON is invalid: {exception.Message}");
            }
        }
    }
}
