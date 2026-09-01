using System;
using UnityEngine;

namespace RuleForge.AI
{
    public static class RuntimeOpenAICredentials
    {
        private const string PlayerPrefsKey =
            "RuleForge.LocalTesting.OpenAIKey";

        private static string sessionKey = string.Empty;
        private static bool loadedSavedKey;

        public static bool HasSessionKey
        {
            get
            {
                EnsureSavedKeyLoaded();
                return !string.IsNullOrWhiteSpace(sessionKey);
            }
        }

        public static bool HasSavedKey =>
            PlayerPrefs.HasKey(PlayerPrefsKey);

        public static bool TrySet(
            string apiKey,
            bool rememberOnThisComputer,
            out string error)
        {
            string normalized = (apiKey ?? string.Empty).Trim();
            if (normalized.Length < 20 ||
                normalized.IndexOfAny(new[] { ' ', '\r', '\n', '\t' }) >= 0)
            {
                error = "API Key 格式无效。请粘贴完整的 OpenAI API Key。";
                return false;
            }

            sessionKey = normalized;
            loadedSavedKey = true;
            if (rememberOnThisComputer)
            {
                PlayerPrefs.SetString(PlayerPrefsKey, normalized);
            }
            else
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
            }

            PlayerPrefs.Save();
            error = string.Empty;
            return true;
        }

        public static bool TryGet(out string apiKey)
        {
            EnsureSavedKeyLoaded();
            apiKey = sessionKey;
            return !string.IsNullOrWhiteSpace(apiKey);
        }

        public static void Clear()
        {
            sessionKey = string.Empty;
            loadedSavedKey = true;
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        private static void EnsureSavedKeyLoaded()
        {
            if (loadedSavedKey)
            {
                return;
            }

            loadedSavedKey = true;
            sessionKey = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            sessionKey = string.Empty;
            loadedSavedKey = false;
        }
    }
}
