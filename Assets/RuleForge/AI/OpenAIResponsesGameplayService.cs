using System;
using System.Collections;
using System.Text;
using RuleForge.DSL;
using UnityEngine;
using UnityEngine.Networking;

namespace RuleForge.AI
{
    [DisallowMultipleComponent]
    public sealed class OpenAIResponsesGameplayService :
        MonoBehaviour,
        IAIGameplayService
    {
        [SerializeField] private string endpoint =
            "https://api.openai.com/v1/responses";
        [SerializeField] private string model = "gpt-5-mini";
        [SerializeField, Min(1)] private int timeoutSeconds = 60;
        [SerializeField, Min(256)] private int maxOutputTokens = 4096;

        public string ProviderName => "OpenAI Responses API";
        public AIProviderKind ProviderKind => AIProviderKind.Real;
        public bool IsBenchmarkEligible => true;
        public bool IsConfigured => TryResolveAuthorization(out _, out _);

        public IEnumerator GenerateChallenge(
            AIChallengeGenerationRequest request,
            Action<AIGameplayResult<ChallengeSpec>> onComplete)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                onComplete?.Invoke(AIGameplayResult<ChallengeSpec>.Failed(
                    "A generation prompt is required."));
                yield break;
            }

            string instructions =
                "You are the gameplay designer for RuleForge. Generate one " +
                "RuleForge ChallengeSpec JSON object only. Use only the " +
                "provided gameplay vocabulary and exact effect mappings. Do not " +
                "invent events, effects, weapons, operations, or runtime code. " +
                "Treat explicit numbers and ratios as requirements when they fit " +
                "the supplied limits. Choose mechanics that express the player's " +
                "intent; do not reuse a familiar rule combination merely because " +
                "it is valid. Return no prose and no markdown.";
            string input =
                "USER PROMPT:\n" + request.UserPrompt +
                "\n\nCREATOR PREFERENCES:\n" + request.CreatorPreferences +
                "\n\nDESIGN GUIDANCE:\n" +
                GameplayVocabulary.BuildDesignGuidance() +
                "\n\nGAMEPLAY VOCABULARY:\n" + request.GameplayVocabulary;
            AIGameplayResult<string> response = null;
            yield return SendStructuredRequest(
                instructions,
                input,
                "ruleforge_challenge_spec",
                request.OutputSchema,
                result => response = result);

            onComplete?.Invoke(ParseStructured<ChallengeSpec>(response));
        }

        public IEnumerator ModifyChallenge(
            AIChallengeModificationRequest request,
            Action<AIGameplayResult<ChallengePatch>> onComplete)
        {
            if (request == null ||
                request.CurrentChallenge == null ||
                string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                onComplete?.Invoke(AIGameplayResult<ChallengePatch>.Failed(
                    "A current challenge and modification prompt are required."));
                yield break;
            }

            string instructions =
                "You modify RuleForge challenges by returning ChallengePatch JSON " +
                "only. Make the smallest possible field-level change. Keep every " +
                "rule, condition, effect, identity, numeric value, goal, and weapon " +
                "the player did not request unchanged. Prefer a granular Modify " +
                "operation over RemoveRule/AddRule. Use multiple operations only " +
                "when the request explicitly requires multiple changes. Treat " +
                "numbers and ratios as requirements within supplied limits. Use " +
                "only supplied operations and vocabulary. For unused operation " +
                "fields, return empty strings, zero values, and null objects. " +
                "ModifyGoal must include goal and goalTarget. AddRule must contain " +
                "one complete rule. Use AddCondition/RemoveCondition for randomness " +
                "or filters, ModifyTrigger for event changes, and effect-level " +
                "operations instead of replacing a whole rule whenever possible. " +
                "Return no prose and no markdown.";
            string input =
                "USER MODIFICATION:\n" + request.UserPrompt +
                "\n\nCURRENT CHALLENGE JSON:\n" +
                JsonUtility.ToJson(request.CurrentChallenge, true) +
                "\n\nCREATOR PREFERENCES:\n" + request.CreatorPreferences +
                "\n\nDESIGN GUIDANCE:\n" +
                GameplayVocabulary.BuildDesignGuidance() +
                "\n\nGAMEPLAY VOCABULARY:\n" + request.GameplayVocabulary;
            AIGameplayResult<string> response = null;
            yield return SendStructuredRequest(
                instructions,
                input,
                "ruleforge_challenge_patch",
                request.OutputSchema,
                result => response = result);

            onComplete?.Invoke(ParseStructured<ChallengePatch>(response));
        }

        private IEnumerator SendStructuredRequest(
            string instructions,
            string input,
            string schemaName,
            string schema,
            Action<AIGameplayResult<string>> onComplete)
        {
            if (!TryResolveAuthorization(
                    out string authorization,
                    out string configurationError))
            {
                onComplete?.Invoke(AIGameplayResult<string>.Failed(
                    configurationError));
                yield break;
            }

            string requestJson = BuildRequestJson(
                instructions,
                input,
                schemaName,
                schema);
            using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(requestJson));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Max(1, timeoutSeconds);
                request.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(authorization))
                {
                    request.SetRequestHeader(
                        "Authorization",
                        "Bearer " + authorization);
                }

                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string message = TryReadApiError(
                        request.downloadHandler != null
                            ? request.downloadHandler.text
                            : string.Empty);
                    onComplete?.Invoke(AIGameplayResult<string>.Failed(
                        $"AI request failed ({request.responseCode}): {message}"));
                    yield break;
                }

                string outputText;
                string parseError;
                if (!TryReadOutputText(
                        request.downloadHandler.text,
                        out outputText,
                        out parseError))
                {
                    onComplete?.Invoke(AIGameplayResult<string>.Failed(parseError));
                    yield break;
                }

                onComplete?.Invoke(AIGameplayResult<string>.Succeeded(outputText));
            }
        }

        private string BuildRequestJson(
            string instructions,
            string input,
            string schemaName,
            string schema)
        {
            return "{" +
                   "\"model\":\"" + GameplayVocabulary.EscapeJson(model) + "\"," +
                   "\"store\":false," +
                   "\"max_output_tokens\":" + Mathf.Max(256, maxOutputTokens) + "," +
                   "\"instructions\":\"" + GameplayVocabulary.EscapeJson(instructions) + "\"," +
                   "\"input\":\"" + GameplayVocabulary.EscapeJson(input) + "\"," +
                   "\"text\":{\"format\":{" +
                   "\"type\":\"json_schema\"," +
                   "\"name\":\"" + GameplayVocabulary.EscapeJson(schemaName) + "\"," +
                   "\"strict\":true," +
                   "\"schema\":" + schema +
                   "}}}";
        }

        private bool TryResolveAuthorization(
            out string authorization,
            out string error)
        {
            authorization = string.Empty;
            if (string.IsNullOrWhiteSpace(endpoint) ||
                !Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri))
            {
                error = "AI endpoint must be an absolute URL.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                error = "AI model is not configured.";
                return false;
            }

            bool isDirectOpenAI = string.Equals(
                uri.Host,
                "api.openai.com",
                StringComparison.OrdinalIgnoreCase);
            if (!isDirectOpenAI)
            {
                error = string.Empty;
                return true;
            }

            authorization = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                error = string.Empty;
                return true;
            }

            if (RuntimeOpenAICredentials.TryGet(out authorization))
            {
                error = string.Empty;
                return true;
            }

            error =
                "OpenAI API Key 未配置。请在开发者视图中输入自己的 Key，" +
                "或通过 OPENAI_API_KEY 环境变量配置。";
            return false;
        }

        private static AIGameplayResult<T> ParseStructured<T>(
            AIGameplayResult<string> response)
            where T : class
        {
            if (response == null || !response.Success)
            {
                return AIGameplayResult<T>.Failed(
                    response != null ? response.Error : "AI request did not complete.");
            }

            try
            {
                T value = JsonUtility.FromJson<T>(response.Value);
                return AIGameplayResult<T>.Succeeded(value);
            }
            catch (ArgumentException exception)
            {
                return AIGameplayResult<T>.Failed(
                    $"AI structured output could not be parsed: {exception.Message}");
            }
        }

        private static bool TryReadOutputText(
            string json,
            out string outputText,
            out string error)
        {
            outputText = string.Empty;
            OpenAIResponse response;
            try
            {
                response = JsonUtility.FromJson<OpenAIResponse>(json);
            }
            catch (ArgumentException exception)
            {
                error = $"OpenAI response JSON is invalid: {exception.Message}";
                return false;
            }

            if (response?.output != null)
            {
                for (int itemIndex = 0;
                     itemIndex < response.output.Length;
                     itemIndex++)
                {
                    OpenAIOutputItem item = response.output[itemIndex];
                    if (item?.content == null)
                    {
                        continue;
                    }

                    for (int contentIndex = 0;
                         contentIndex < item.content.Length;
                         contentIndex++)
                    {
                        OpenAIContentItem content = item.content[contentIndex];
                        if (content == null)
                        {
                            continue;
                        }

                        if (string.Equals(
                                content.type,
                                "output_text",
                                StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(content.text))
                        {
                            outputText = content.text;
                            error = string.Empty;
                            return true;
                        }

                        if (string.Equals(
                                content.type,
                                "refusal",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            error = string.IsNullOrWhiteSpace(content.refusal)
                                ? "The AI provider refused the request."
                                : content.refusal;
                            return false;
                        }
                    }
                }
            }

            error = "OpenAI response did not contain structured output text.";
            return false;
        }

        private static string TryReadApiError(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "No response body.";
            }

            try
            {
                OpenAIErrorEnvelope envelope =
                    JsonUtility.FromJson<OpenAIErrorEnvelope>(json);
                if (envelope?.error != null &&
                    !string.IsNullOrWhiteSpace(envelope.error.message))
                {
                    return envelope.error.message;
                }
            }
            catch (ArgumentException)
            {
            }

            return "The provider returned an unreadable error response.";
        }

        [Serializable]
        private sealed class OpenAIResponse
        {
            public OpenAIOutputItem[] output;
        }

        [Serializable]
        private sealed class OpenAIOutputItem
        {
            public OpenAIContentItem[] content;
        }

        [Serializable]
        private sealed class OpenAIContentItem
        {
            public string type;
            public string text;
            public string refusal;
        }

        [Serializable]
        private sealed class OpenAIErrorEnvelope
        {
            public OpenAIError error;
        }

        [Serializable]
        private sealed class OpenAIError
        {
            public string message;
        }
    }
}
