using System;
using RuleForge.AI;
using UnityEngine;

namespace RuleForge.UI
{
    internal sealed class AIProviderSetupView
    {
        private static readonly string[] ProviderLabelsEnglish =
        {
            "OpenAI",
            "DeepSeek",
            "Custom Compatible API"
        };

        private static readonly string[] ProviderLabelsChinese =
        {
            "OpenAI",
            "DeepSeek",
            "自定义兼容 API"
        };

        private string apiKeyDraft = string.Empty;
        private bool rememberApiKeyOnThisComputer;
        private string status = string.Empty;
        private bool initialized;
        private int providerIndex;
        private int protocolIndex;
        private string customEndpointDraft = string.Empty;
        private string modelDraft = string.Empty;

        public void Draw(AIGameplayController controller)
        {
            GUILayout.Space(8f);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI Provider — shared playtest",
                "AI Provider — 共享试玩版"), GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "A DeepSeek test key is included for playtesting. You can also select another provider and enter your own key.",
                "已内置 DeepSeek 试玩 Key；也可以选择其他 Provider 并填写自己的 Key。"));

            EnsureInitialized();
            DrawProviderConfiguration(controller);
            DrawApiKeyConfiguration(controller);
            DrawConnectionVerification(controller);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            providerIndex =
                (int)RuntimeOpenAICredentials.SelectedProvider;
            protocolIndex =
                (int)RuntimeOpenAICredentials.ConfiguredProtocol;
            customEndpointDraft =
                RuntimeOpenAICredentials.SelectedProvider ==
                RuntimeAIProvider.CustomApi
                    ? RuntimeOpenAICredentials.ConfiguredEndpoint
                    : string.Empty;
            modelDraft = RuntimeOpenAICredentials.ConfiguredModel;
        }

        private void DrawProviderConfiguration(
            AIGameplayController controller)
        {
            int previousProviderIndex = providerIndex;
            providerIndex = GUILayout.SelectionGrid(
                Mathf.Clamp(providerIndex, 0, 2),
                RuleForgeLocalization.Current ==
                RuleForgeLanguage.Chinese
                    ? ProviderLabelsChinese
                    : ProviderLabelsEnglish,
                3,
                GUILayout.Height(34f));
            RuntimeAIProvider draftProvider =
                (RuntimeAIProvider)providerIndex;
            if (previousProviderIndex != providerIndex)
            {
                modelDraft =
                    RuntimeOpenAICredentials.GetDefaultModel(draftProvider);
            }

            GUILayout.Label(RuleForgeLocalization.T("Model", "模型"));
            modelDraft = GUILayout.TextField(
                modelDraft ?? string.Empty,
                GUILayout.Height(28f));
            if (draftProvider == RuntimeAIProvider.CustomApi)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "API Protocol", "API 协议"));
                protocolIndex = GUILayout.SelectionGrid(
                    Mathf.Clamp(protocolIndex, 0, 1),
                    new[] { "Responses", "Chat Completions" },
                    2,
                    GUILayout.Height(30f));
                GUILayout.Label("API Endpoint");
                customEndpointDraft = GUILayout.TextField(
                    customEndpointDraft ?? string.Empty,
                    GUILayout.Height(28f));
                GUILayout.Label(RuleForgeLocalization.T(
                    "The endpoint must implement the selected OpenAI-compatible protocol and return structured JSON.",
                    "端点必须实现所选 OpenAI 兼容协议，并能返回结构化 JSON。"));
            }
            else
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Endpoint: ", "接口：") +
                    (draftProvider == RuntimeAIProvider.DeepSeek
                        ? RuntimeOpenAICredentials.DeepSeekEndpoint
                        : RuntimeOpenAICredentials.OpenAIEndpoint));
            }

            GUI.enabled = controller == null || !controller.IsBusy;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Apply Provider Configuration",
                    "应用 Provider 配置"), GUILayout.Height(30f)))
            {
                if (TryApplyDraftConfiguration(
                        controller, out string providerError))
                {
                    status = RuleForgeLocalization.T(
                        "Provider configuration saved. Connection is not verified yet.",
                        "Provider 配置已保存，连接尚未验证。");
                }
                else
                {
                    status = providerError;
                }
            }

            GUI.enabled = true;
        }

        private void DrawApiKeyConfiguration(
            AIGameplayController controller)
        {
            GUILayout.Label(RuleForgeLocalization.T(
                "API Key for the selected provider",
                "所选 Provider 的 API Key"));
            apiKeyDraft = GUILayout.PasswordField(
                apiKeyDraft ?? string.Empty,
                '•',
                GUILayout.Height(28f));
            rememberApiKeyOnThisComputer = GUILayout.Toggle(
                rememberApiKeyOnThisComputer,
                RuleForgeLocalization.T(
                    "Remember on this computer (not recommended on shared PCs)",
                    "记住到这台电脑（共用电脑不推荐）"));

            GUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrWhiteSpace(apiKeyDraft) &&
                          (controller == null || !controller.IsBusy);
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Use API Key",
                    "使用这个 API Key"), GUILayout.Height(30f)))
            {
                if (!TryApplyDraftConfiguration(
                        controller, out string providerError))
                {
                    status = providerError;
                }
                else if (RuntimeOpenAICredentials.TrySet(
                        apiKeyDraft,
                        rememberApiKeyOnThisComputer,
                        out string error))
                {
                    apiKeyDraft = string.Empty;
                    controller?.RefreshProviderSelection();
                    status = rememberApiKeyOnThisComputer
                        ? RuleForgeLocalization.T(
                            "API Key remembered on this computer. Verify the connection before generating.",
                            "API Key 已保存在本机。生成前请验证连接。")
                        : RuleForgeLocalization.T(
                            "API Key is active only for this Play session. Enter it again next time, then verify the connection.",
                            "API Key 仅本次运行有效；下次进入游戏需重新输入。请验证连接。");
                }
                else
                {
                    status = error;
                }
            }

            GUI.enabled = (RuntimeOpenAICredentials.HasSessionKey ||
                           RuntimeOpenAICredentials.HasSavedKey) &&
                          (RuntimeAIProvider)Mathf.Clamp(providerIndex, 0, 2) ==
                          RuntimeOpenAICredentials.SelectedProvider;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Clear Local Key",
                    "清除本机 Key"), GUILayout.Height(30f)))
            {
                RuntimeOpenAICredentials.Clear();
                apiKeyDraft = string.Empty;
                controller?.RefreshProviderSelection();
                status = RuleForgeLocalization.T(
                    "The locally entered API Key was cleared.",
                    "已清除本机输入的 API Key。");
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(
                RuntimeOpenAICredentials.HasEnvironmentKeyForSelectedProvider
                    ? RuleForgeLocalization.T(
                        "API Key status: available from ",
                        "API Key 状态：已从环境变量读取 ") +
                      RuntimeOpenAICredentials.GetEnvironmentVariableName(
                          RuntimeOpenAICredentials.SelectedProvider)
                    : RuntimeOpenAICredentials.HasSessionKey
                    ? RuleForgeLocalization.T(
                        RuntimeOpenAICredentials.HasSavedKey
                            ? "Local API Key status: Active and remembered"
                            : "Local API Key status: Active for this session",
                        RuntimeOpenAICredentials.HasSavedKey
                            ? "本机 API Key 状态：已启用并记住"
                            : "本机 API Key 状态：仅本次运行启用")
                    : RuntimeOpenAICredentials.HasEmbeddedKeyForSelectedProvider
                    ? RuleForgeLocalization.T(
                        "API Key status: built-in DeepSeek playtest key",
                        "API Key 状态：已内置 DeepSeek 试玩 Key")
                    : RuleForgeLocalization.T(
                        "Local API Key status: Not configured",
                        "本机 API Key 状态：未配置"));
        }

        private void DrawConnectionVerification(
            AIGameplayController controller)
        {
            GUI.enabled = controller != null &&
                          controller.ActiveProviderKind == AIProviderKind.Real &&
                          controller.ActiveProviderIsConfigured &&
                          DraftMatchesActiveConfiguration() &&
                          !controller.IsBusy;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Verify Real Connection",
                    "验证真实连接"), GUILayout.Height(34f)))
            {
                status = RuleForgeLocalization.T(
                    "Verifying provider connection...",
                    "正在验证 Provider 连接……");
                controller.VerifyActiveProvider((success, message) =>
                {
                    status = success
                        ? RuleForgeLocalization.T(
                            "Connection verified. Real AI is ready.",
                            "连接验证成功，真实 AI 可以使用。")
                        : RuleForgeLocalization.T(
                            "Connection failed: ",
                            "连接失败：") + message;
                });
            }

            GUI.enabled = true;
            if (!DraftMatchesActiveConfiguration())
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Apply the selected provider and model before verifying.",
                    "请先应用当前选择的 Provider 和模型，再验证连接。"));
            }

            if (controller != null &&
                controller.ActiveProviderKind == AIProviderKind.Real)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Connection: ", "连接：") +
                    GetConnectionStateLabel(
                        controller.ActiveProviderConnectionState));
                if (!string.IsNullOrWhiteSpace(
                        controller.ActiveProviderConnectionMessage))
                {
                    GUILayout.Label(
                        controller.ActiveProviderConnectionMessage,
                        GUI.skin.box);
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                GUILayout.Label(status, GUI.skin.box);
            }
        }

        private bool TryApplyDraftConfiguration(
            AIGameplayController controller,
            out string error)
        {
            bool configured = RuntimeOpenAICredentials.TryConfigureProvider(
                (RuntimeAIProvider)Mathf.Clamp(providerIndex, 0, 2),
                customEndpointDraft,
                modelDraft,
                (RuntimeAIProtocol)Mathf.Clamp(protocolIndex, 0, 1),
                out error);
            if (configured)
            {
                controller?.RefreshProviderSelection();
            }

            return configured;
        }

        private bool DraftMatchesActiveConfiguration()
        {
            RuntimeAIProvider provider =
                (RuntimeAIProvider)Mathf.Clamp(providerIndex, 0, 2);
            return provider == RuntimeOpenAICredentials.SelectedProvider &&
                   string.Equals((modelDraft ?? string.Empty).Trim(),
                       RuntimeOpenAICredentials.ConfiguredModel,
                       StringComparison.Ordinal) &&
                   (provider != RuntimeAIProvider.CustomApi ||
                    (string.Equals((customEndpointDraft ?? string.Empty).Trim(),
                         RuntimeOpenAICredentials.ConfiguredEndpoint,
                         StringComparison.Ordinal) &&
                     (RuntimeAIProtocol)Mathf.Clamp(protocolIndex, 0, 1) ==
                     RuntimeOpenAICredentials.ConfiguredProtocol));
        }

        private static string GetConnectionStateLabel(
            AIProviderConnectionState state)
        {
            switch (state)
            {
                case AIProviderConnectionState.Verified:
                    return RuleForgeLocalization.T("Verified", "已验证");
                case AIProviderConnectionState.Verifying:
                    return RuleForgeLocalization.T("Verifying", "验证中");
                case AIProviderConnectionState.Failed:
                    return RuleForgeLocalization.T("Failed", "失败");
                case AIProviderConnectionState.Unverified:
                    return RuleForgeLocalization.T("Unverified", "未验证");
                default:
                    return RuleForgeLocalization.T(
                        "Not configured", "未配置");
            }
        }
    }
}
