using System;
using UnityEngine;

namespace RuleForge.AI
{
    public enum RuntimeAIProvider
    {
        OpenAI,
        DeepSeek,
        CustomApi
    }

    public enum RuntimeAIProtocol
    {
        Responses,
        ChatCompletions
    }

    public static class RuntimeOpenAICredentials
    {
        private const string PlayerPrefsKey =
            "RuleForge.LocalTesting.OpenAIKey";
        private const string ProviderPrefsKey =
            "RuleForge.LocalTesting.AIProvider";
        private const string CustomEndpointPrefsKey =
            "RuleForge.LocalTesting.AICustomEndpoint";
        private const string ModelPrefsKey =
            "RuleForge.LocalTesting.AIModel";
        private const string OpenAIModelPrefsKey =
            "RuleForge.LocalTesting.OpenAIModel";
        private const string DeepSeekModelPrefsKey =
            "RuleForge.LocalTesting.DeepSeekModel";
        private const string CustomModelPrefsKey =
            "RuleForge.LocalTesting.CustomAIModel";
        private const string ProtocolPrefsKey =
            "RuleForge.LocalTesting.AIProtocol";
        // Shared playtest credential. Replace or revoke it before public release.
        private const string SharedDeepSeekTestKey =
            "sk-ad3b6aeed0394f9a8b6c2477f11f7fbb";

        public const string OpenAIEndpoint =
            "https://api.openai.com/v1/responses";
        public const string DeepSeekEndpoint =
            "https://api.deepseek.com/responses";
        public const string DefaultOpenAIModel = "gpt-5-mini";
        public const string DefaultDeepSeekModel = "deepseek-flash";
        private const string PreviousDeepSeekDefaultModel =
            "deepseek-v4-flash";

        private static string sessionKey = string.Empty;
        private static bool loadedSavedKey;
        private static RuntimeAIProvider sessionKeyProvider;

        public static bool HasSessionKey
        {
            get
            {
                EnsureSavedKeyLoaded();
                return !string.IsNullOrWhiteSpace(sessionKey);
            }
        }

        public static bool HasSavedKey =>
            PlayerPrefs.HasKey(GetPlayerPrefsKey(SelectedProvider));

        public static bool HasEnvironmentKeyForSelectedProvider =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                GetEnvironmentVariableName(SelectedProvider)));

        public static bool HasEmbeddedKeyForSelectedProvider =>
            SelectedProvider == RuntimeAIProvider.DeepSeek &&
            !string.IsNullOrWhiteSpace(SharedDeepSeekTestKey);

        public static RuntimeAIProvider SelectedProvider
        {
            get
            {
                if (!PlayerPrefs.HasKey(ProviderPrefsKey) &&
                    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                        "OPENAI_API_KEY")) &&
                    (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                         "DEEPSEEK_API_KEY")) ||
                     !string.IsNullOrWhiteSpace(SharedDeepSeekTestKey)))
                {
                    return RuntimeAIProvider.DeepSeek;
                }

                int value = PlayerPrefs.GetInt(
                    ProviderPrefsKey,
                    (int)RuntimeAIProvider.OpenAI);
                return Enum.IsDefined(typeof(RuntimeAIProvider), value)
                    ? (RuntimeAIProvider)value
                    : RuntimeAIProvider.OpenAI;
            }
        }

        public static string ConfiguredEndpoint
        {
            get
            {
                switch (SelectedProvider)
                {
                    case RuntimeAIProvider.DeepSeek:
                        return DeepSeekEndpoint;
                    case RuntimeAIProvider.CustomApi:
                        return PlayerPrefs.GetString(
                            CustomEndpointPrefsKey,
                            string.Empty).Trim();
                    default:
                        return OpenAIEndpoint;
                }
            }
        }

        public static string ConfiguredModel
        {
            get
            {
                string fallback = GetDefaultModel(SelectedProvider);
                string providerKey = GetModelPrefsKey(SelectedProvider);
                if (PlayerPrefs.HasKey(providerKey))
                {
                    return NormalizeStoredModel(
                        SelectedProvider,
                        PlayerPrefs.GetString(providerKey, fallback));
                }

                // Preserve the model saved by older versions for the provider
                // that was selected then, without carrying it to another API.
                if (PlayerPrefs.HasKey(ProviderPrefsKey) &&
                    PlayerPrefs.GetInt(ProviderPrefsKey) ==
                    (int)SelectedProvider)
                {
                    return NormalizeStoredModel(
                        SelectedProvider,
                        PlayerPrefs.GetString(ModelPrefsKey, fallback));
                }

                return fallback;
            }
        }

        public static string GetEnvironmentVariableName(
            RuntimeAIProvider provider)
        {
            switch (provider)
            {
                case RuntimeAIProvider.DeepSeek:
                    return "DEEPSEEK_API_KEY";
                case RuntimeAIProvider.CustomApi:
                    return "RULEFORGE_AI_API_KEY";
                default:
                    return "OPENAI_API_KEY";
            }
        }

        public static RuntimeAIProtocol ConfiguredProtocol
        {
            get
            {
                if (SelectedProvider != RuntimeAIProvider.CustomApi)
                {
                    return RuntimeAIProtocol.Responses;
                }

                int value = PlayerPrefs.GetInt(
                    ProtocolPrefsKey,
                    (int)RuntimeAIProtocol.Responses);
                return Enum.IsDefined(typeof(RuntimeAIProtocol), value)
                    ? (RuntimeAIProtocol)value
                    : RuntimeAIProtocol.Responses;
            }
        }

        public static string GetDefaultModel(RuntimeAIProvider provider)
        {
            return provider == RuntimeAIProvider.DeepSeek
                ? DefaultDeepSeekModel
                : provider == RuntimeAIProvider.OpenAI
                    ? DefaultOpenAIModel
                    : string.Empty;
        }

        public static bool TryConfigureProvider(
            RuntimeAIProvider provider,
            string customEndpoint,
            string model,
            RuntimeAIProtocol protocol,
            out string error)
        {
            string normalizedModel = (model ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedModel))
            {
                error = "必须填写模型名称。";
                return false;
            }

            string normalizedEndpoint = provider == RuntimeAIProvider.CustomApi
                ? (customEndpoint ?? string.Empty).Trim()
                : provider == RuntimeAIProvider.DeepSeek
                    ? DeepSeekEndpoint
                    : OpenAIEndpoint;
            if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttps &&
                 !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            {
                error = "API Endpoint 必须是 HTTPS 地址；仅本机地址允许 HTTP。";
                return false;
            }

            MigrateLegacyModel();
            PlayerPrefs.SetInt(ProviderPrefsKey, (int)provider);
            PlayerPrefs.SetString(GetModelPrefsKey(provider), normalizedModel);
            PlayerPrefs.SetInt(ProtocolPrefsKey, (int)protocol);
            if (provider == RuntimeAIProvider.CustomApi)
            {
                PlayerPrefs.SetString(CustomEndpointPrefsKey, normalizedEndpoint);
            }

            PlayerPrefs.Save();
            error = string.Empty;
            return true;
        }

        public static bool TrySet(
            string apiKey,
            bool rememberOnThisComputer,
            out string error)
        {
            string normalized = (apiKey ?? string.Empty).Trim();
            if (normalized.Length < 20 ||
                normalized.IndexOfAny(new[] { ' ', '\r', '\n', '\t' }) >= 0)
            {
                error = "API Key 格式无效。请粘贴当前 Provider 的完整 API Key。";
                return false;
            }

            sessionKey = normalized;
            loadedSavedKey = true;
            sessionKeyProvider = SelectedProvider;
            string selectedKey = GetPlayerPrefsKey(SelectedProvider);
            if (rememberOnThisComputer)
            {
                PlayerPrefs.SetString(selectedKey, normalized);
            }
            else
            {
                PlayerPrefs.DeleteKey(selectedKey);
            }

            PlayerPrefs.Save();
            error = string.Empty;
            return true;
        }

        public static bool TryGet(out string apiKey)
        {
            EnsureSavedKeyLoaded();
            apiKey = !string.IsNullOrWhiteSpace(sessionKey)
                ? sessionKey
                : HasEmbeddedKeyForSelectedProvider
                    ? SharedDeepSeekTestKey
                    : string.Empty;
            return !string.IsNullOrWhiteSpace(apiKey);
        }

        public static void Clear()
        {
            sessionKey = string.Empty;
            loadedSavedKey = true;
            sessionKeyProvider = SelectedProvider;
            PlayerPrefs.DeleteKey(GetPlayerPrefsKey(SelectedProvider));
            PlayerPrefs.Save();
        }

        private static void EnsureSavedKeyLoaded()
        {
            RuntimeAIProvider selectedProvider = SelectedProvider;
            if (loadedSavedKey && sessionKeyProvider == selectedProvider)
            {
                return;
            }

            loadedSavedKey = true;
            sessionKeyProvider = selectedProvider;
            sessionKey = PlayerPrefs.GetString(
                GetPlayerPrefsKey(selectedProvider),
                string.Empty);
        }

        private static string GetPlayerPrefsKey(RuntimeAIProvider provider)
        {
            switch (provider)
            {
                case RuntimeAIProvider.DeepSeek:
                    return "RuleForge.LocalTesting.DeepSeekKey";
                case RuntimeAIProvider.CustomApi:
                    return "RuleForge.LocalTesting.CustomAIKey";
                default:
                    return PlayerPrefsKey;
            }
        }

        private static string GetModelPrefsKey(RuntimeAIProvider provider)
        {
            switch (provider)
            {
                case RuntimeAIProvider.DeepSeek:
                    return DeepSeekModelPrefsKey;
                case RuntimeAIProvider.CustomApi:
                    return CustomModelPrefsKey;
                default:
                    return OpenAIModelPrefsKey;
            }
        }

        private static void MigrateLegacyModel()
        {
            if (!PlayerPrefs.HasKey(ModelPrefsKey) ||
                !PlayerPrefs.HasKey(ProviderPrefsKey))
            {
                return;
            }

            int previous = PlayerPrefs.GetInt(ProviderPrefsKey);
            if (Enum.IsDefined(typeof(RuntimeAIProvider), previous))
            {
                RuntimeAIProvider previousProvider =
                    (RuntimeAIProvider)previous;
                string key = GetModelPrefsKey(previousProvider);
                if (!PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.SetString(key,
                        PlayerPrefs.GetString(ModelPrefsKey));
                }
            }

            PlayerPrefs.DeleteKey(ModelPrefsKey);
        }

        private static string NormalizeStoredModel(
            RuntimeAIProvider provider,
            string model)
        {
            string normalized = (model ?? string.Empty).Trim();
            return provider == RuntimeAIProvider.DeepSeek &&
                   string.Equals(normalized, PreviousDeepSeekDefaultModel,
                       StringComparison.OrdinalIgnoreCase)
                ? DefaultDeepSeekModel
                : normalized;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            sessionKey = string.Empty;
            loadedSavedKey = false;
            sessionKeyProvider = RuntimeAIProvider.OpenAI;
        }
    }
}
