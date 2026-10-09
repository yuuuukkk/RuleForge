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
        public AIProviderConnectionState ConnectionState =>
            IsConfigured
                ? AIProviderConnectionState.Verified
                : AIProviderConnectionState.NotConfigured;
        public string ConnectionMessage => IsConfigured
            ? "Offline mock is available; it is not a real AI connection."
            : "Offline mock is disabled.";

        public IEnumerator VerifyConnection(Action<bool, string> onComplete)
        {
            onComplete?.Invoke(
                false,
                "Mock Provider 不是可验证的真实 AI 接口。");
            yield break;
        }

        public void Configure(TextAsset challengeJson, TextAsset patchJson)
        {
            generatedChallengeJson = challengeJson;
            modificationPatchJson = patchJson;
        }

        public IEnumerator AnalyzeGameplay(
            AIProposalAnalysisRequest request,
            Action<AIGameplayResult<GameplayProposal>> onComplete)
        {
            yield return null;
            onComplete?.Invoke(AIGameplayResult<GameplayProposal>.Failed(
                "Mock Provider 只能提供固定离线样例，不能冒充 AI Gameplay Designer。"));
        }

        public IEnumerator AnalyzeModification(
            AIModificationProposalRequest request,
            Action<AIGameplayResult<GameplayModificationProposal>> onComplete)
        {
            yield return null;
            onComplete?.Invoke(
                AIGameplayResult<GameplayModificationProposal>.Failed(
                    "Mock Provider 无法理解自然语言试玩反馈。"));
        }

        public IEnumerator RepairChallenge(
            AIChallengeRepairRequest request,
            Action<AIGameplayResult<GameplayRepairResult>> onComplete)
        {
            yield return null;
            onComplete?.Invoke(AIGameplayResult<GameplayRepairResult>.Failed(
                "Mock Provider 无法根据 Validator 结果修复玩法。"));
        }

        public IEnumerator AnalyzeImprovements(
            AIImprovementAnalysisRequest request,
            Action<AIGameplayResult<GameplayImprovementSet>> onComplete)
        {
            yield return null;
            onComplete?.Invoke(AIGameplayResult<GameplayImprovementSet>.Failed(
                "Mock Provider 无法提供真实 AI 改进建议。"));
        }

        public IEnumerator RepairPatch(
            AIChallengePatchRepairRequest request,
            Action<AIGameplayResult<ChallengePatch>> onComplete)
        {
            yield return null;
            onComplete?.Invoke(AIGameplayResult<ChallengePatch>.Failed(
                "Mock Provider 无法根据 Patch 错误修复修改。"));
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
