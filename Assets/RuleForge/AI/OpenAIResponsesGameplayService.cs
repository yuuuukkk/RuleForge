using System;
using System.Collections;
using System.Text;
using RuleForge.DSL;
using RuleForge.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RuleForge.AI
{
    [DisallowMultipleComponent]
    public sealed class OpenAIResponsesGameplayService :
        MonoBehaviour,
        IAIGameplayService
    {
        [SerializeField, Min(1)] private int timeoutSeconds = 60;
        [SerializeField, Min(256)] private int maxOutputTokens = 4096;

        private AIProviderConnectionState connectionState =
            AIProviderConnectionState.Unverified;
        private string connectionMessage = string.Empty;
        private string connectionSignature = string.Empty;

        public string ProviderName
        {
            get
            {
                switch (RuntimeOpenAICredentials.SelectedProvider)
                {
                    case RuntimeAIProvider.DeepSeek:
                        return "DeepSeek Responses API";
                    case RuntimeAIProvider.CustomApi:
                        return "Custom Compatible API";
                    default:
                        return "OpenAI Responses API";
                }
            }
        }
        public AIProviderKind ProviderKind => AIProviderKind.Real;
        public bool IsBenchmarkEligible => true;
        public bool IsConfigured => TryResolveConnection(
            out _, out _, out _, out _);
        public AIProviderConnectionState ConnectionState
        {
            get
            {
                if (!TryResolveConnection(out string resolvedEndpoint,
                        out string resolvedModel,
                        out string authorization,
                        out _))
                {
                    return AIProviderConnectionState.NotConfigured;
                }

                string currentSignature = BuildConnectionSignature(
                    resolvedEndpoint,
                    resolvedModel,
                    authorization,
                    RuntimeOpenAICredentials.ConfiguredProtocol);
                return string.Equals(
                    connectionSignature,
                    currentSignature,
                    StringComparison.Ordinal)
                    ? connectionState
                    : AIProviderConnectionState.Unverified;
            }
        }
        public string ConnectionMessage
        {
            get
            {
                if (!TryResolveConnection(out _, out _, out _,
                        out string configurationError))
                {
                    return configurationError;
                }

                return ConnectionState == AIProviderConnectionState.Unverified
                    ? "Credentials are present, but this provider has not been verified."
                    : connectionMessage;
            }
        }

        public IEnumerator VerifyConnection(Action<bool, string> onComplete)
        {
            if (!TryResolveConnection(
                    out string resolvedEndpoint,
                    out string resolvedModel,
                    out string authorization,
                    out string error))
            {
                connectionState = AIProviderConnectionState.NotConfigured;
                connectionMessage = error;
                onComplete?.Invoke(false, error);
                yield break;
            }

            connectionSignature = BuildConnectionSignature(
                resolvedEndpoint,
                resolvedModel,
                authorization,
                RuntimeOpenAICredentials.ConfiguredProtocol);
            connectionState = AIProviderConnectionState.Verifying;
            connectionMessage = "Verifying provider connection...";

            AIGameplayResult<string> result = null;
            yield return SendStructuredRequest(
                "Return only a JSON object confirming that this API supports the " +
                "Responses API structured-output contract required by RuleForge.",
                "Return {\"ok\":true} as JSON.",
                "ruleforge_connection_test",
                "{\"type\":\"object\",\"properties\":{" +
                "\"ok\":{\"type\":\"boolean\"}}," +
                "\"required\":[\"ok\"],\"additionalProperties\":false}",
                response => result = response);

            bool success = result != null && result.Success;
            string message = success
                ? ProviderName + " connection verified."
                : result != null
                    ? result.Error
                    : "Provider verification did not complete.";
            onComplete?.Invoke(success, message);
        }

        public IEnumerator AnalyzeGameplay(
            AIProposalAnalysisRequest request,
            Action<AIGameplayResult<GameplayProposal>> onComplete)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                onComplete?.Invoke(AIGameplayResult<GameplayProposal>.Failed(
                    "A gameplay idea is required."));
                yield break;
            }

            string instructions =
                "Act as RuleForge's prompt editor, Interpreter, Gameplay Designer, " +
                "and light Gameplay Critic. The player may describe an imprecise " +
                "feeling; rewrite it into a precise, playable proposal instead of " +
                "exposing system fields. Assess whether goal, trigger, reward, risk, " +
                "scaling, and limit are explicit. Prefer a concrete, playable " +
                "proposal over asking questions: infer sensible missing details " +
                "and explain them briefly. Ask one clarification only when the " +
                "ambiguity would produce materially different designs. Identify " +
                "conflicts and unsupported requests. Warn about one-sided rewards, " +
                "runaway growth, or missing loops, but preserve the player's final " +
                "choice, including deliberately extreme designs. Use only the " +
                "provided vocabulary. A numeric request beyond safe limits should " +
                "remain visible in the proposal with a warning and canGenerate true " +
                "so the real Validator, not this analysis, makes the ruling. " +
                "If the core mechanic requires a primitive outside the supplied " +
                "vocabulary, set withinVocabulary and canGenerate false and offer " +
                "the nearest supported design or one focused clarification. " +
                "A kill-to-extend-life countdown uses a TimeBank goal, " +
                "timeLimit as starting seconds, timeDamageScale for enemy " +
                "hit damage converted into lost seconds, and AddTime on " +
                "EnemyKilled. If the ending is unspecified, do not invent a " +
                "kill target or extra risk. Set canGenerate false and offer " +
                "three distinct actionSuggestions in the player's language: " +
                "reach a chosen time bank (TimeBankTarget), survive a chosen " +
                "real duration (TimeBankSurvive), or play until time runs out " +
                "(TimeBankEndless). These suggestions are natural-language " +
                "refinements, not executable templates. " +
                "Record every indispensable mechanic in requiredMechanics " +
                "using exact vocabulary trigger and effectId identifiers. " +
                "If the player specifies an amount, set exactValue true and " +
                "record that exact value; otherwise use exactValue false. " +
                "If the player specifies a stack limit, record it for each " +
                "affected mechanic in maxStacks and set exactMaxStacks true; " +
                "otherwise set exactMaxStacks false. A stack limit is not a " +
                "victory target. EnemyKilled has no numeric event value: " +
                "never use kill-count EventValue conditions for per-kill growth. " +
                "If the player explicitly says survive N seconds, use goal " +
                "Survive with target N and no separate countdown, never a " +
                "KillCount goal inherited from the active challenge. " +
                "Record the proposed goal enum in requiredGoalType and a " +
                "countdown in requiredTimeLimitSeconds (0 if absent). " +
                "Record requiredTimeDamageScale as 0 for ordinary goals. " +
                "For TimeBank choose 0.1 seconds per incoming damage point " +
                "unless the player specifies another allowed conversion, " +
                "and explain this conversion in the proposal. " +
                "When canGenerate is true, requiredGoalType cannot be empty, " +
                "and nonempty suggestedRules require nonempty requiredMechanics. " +
                "Set requiredGoalTarget to the confirmed numerical victory " +
                "target; never silently change it during generation or repair. " +
                "If no risk was requested, put optional risk ideas only in " +
                "actionSuggestions, not in requiredMechanics or suggestedRules. " +
                "For kill-to-extend-life, requiredMechanics must contain " +
                "EnemyKilled + AddTime with the player's requested seconds, " +
                "and requiredTimeLimitSeconds must contain the starting time. " +
                "TimeBankEndless has requiredGoalTarget 0. " +
                "Never substitute ammo, damage, or enemy-speed effects for " +
                "this core mechanic. " +
                "The summary must be exactly one concise player-facing sentence " +
                "in the player's language that explains the goal, the advantage, " +
                "the cost, and escalation when present. Do not use DSL names, JSON " +
                "field names, or a technical list in summary. Set penaltyRewardRatio " +
                "to penalty strength divided by reward strength: 0 means no risk, " +
                "1 means balanced, and values above 1 mean risk grows faster. " +
                "Suggestions are at most three short, distinct natural-language " +
                "design directions, never executable values or UI shortcuts. " +
                "Return player-facing " +
                "structured JSON only; do not reveal hidden chain-of-thought.";
            string input =
                "ORIGINAL PLAYER IDEA:\n" + request.UserPrompt +
                "\n\nCURRENT PROPOSAL JSON (may be empty):\n" +
                (request.CurrentProposal != null
                    ? JsonUtility.ToJson(request.CurrentProposal, true)
                    : "none") +
                "\n\nPLAYER REFINEMENT (may be empty):\n" +
                request.RefinementIntent +
                "\n\nCREATOR PREFERENCES:\n" + request.CreatorPreferences +
                "\n\nDESIGN GUIDANCE:\n" + GameplayVocabulary.BuildDesignGuidance() +
                "\n\nGAMEPLAY VOCABULARY:\n" + request.GameplayVocabulary;
            AIGameplayResult<string> response = null;
            yield return SendStructuredRequest(
                instructions,
                input,
                "ruleforge_gameplay_proposal",
                request.OutputSchema,
                result => response = result);

            onComplete?.Invoke(ParseStructured<GameplayProposal>(response));
        }

        public IEnumerator GenerateChallenge(
            AIChallengeGenerationRequest request,
            Action<AIGameplayResult<ChallengeSpec>> onComplete)
        {
            if (request == null ||
                request.ConfirmedProposal == null ||
                string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                onComplete?.Invoke(AIGameplayResult<ChallengeSpec>.Failed(
                    "A confirmed gameplay proposal is required."));
                yield break;
            }

            string instructions =
                "Convert the player-confirmed GameplayProposal into exactly one " +
                "RuleForge ChallengeSpec JSON object. The proposal, not a guessed " +
                "template, is the source of truth. Use only the supplied gameplay " +
                "vocabulary and exact effect mappings. Do not invent runtime code. " +
                "Preserve explicit numbers and ratios exactly, even when they may " +
                "exceed a limit; the real Validator and Repairer own that decision. " +
                "Honor the confirmed proposal's penaltyRewardRatio when choosing " +
                "or revising reward and penalty magnitudes. " +
                "Every requiredMechanic in the confirmed proposal is a hard " +
                "contract: preserve its trigger, effectId, explicit value and " +
                "explicit maxStacks, " +
                "as well as requiredGoalType, requiredGoalTarget and " +
                "requiredTimeLimitSeconds and requiredTimeDamageScale. " +
                "Do not add effects that were not " +
                "listed in requiredMechanics. " +
                "For every effectId, copy kind, target, statId, operation and " +
                "stringValue from the same catalog row. Check allowedValue on " +
                "that row. A positive damage bonus cannot use a negative value. " +
                "EnemyKilled has no numeric event value. For every-kill growth " +
                "use a plain EnemyKilled trigger, empty conditions, and a single " +
                "Stack effect with the confirmed maxStacks. Do not split one " +
                "per-kill effect into kill-count threshold rules. " +
                "Do not translate unsupported mechanics into a misleading " +
                "different mechanic. " +
                "Return no prose and no markdown.";
            string input =
                "USER PROMPT:\n" + request.UserPrompt +
                "\n\nCONFIRMED GAMEPLAY PROPOSAL JSON:\n" +
                JsonUtility.ToJson(request.ConfirmedProposal, true) +
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

        public IEnumerator AnalyzeModification(
            AIModificationProposalRequest request,
            Action<AIGameplayResult<GameplayModificationProposal>> onComplete)
        {
            if (request == null || request.CurrentChallenge == null ||
                string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                onComplete?.Invoke(
                    AIGameplayResult<GameplayModificationProposal>.Failed(
                        "A current challenge and player feedback are required."));
                yield break;
            }

            string instructions =
                "Act as RuleForge's Modifier and Gameplay Critic. Interpret both " +
                "precise edits and experiential feedback such as 'late game is too " +
                "wild' against the real current challenge. Identify the smallest " +
                "relevant parameter change and explain it before any patch exists. " +
                "If a reference is genuinely ambiguous between multiple current " +
                "rules, ask one focused clarification and set canModify false. " +
                "Never silently change unrelated rules. patchInstruction must be a " +
                "complete natural-language instruction for a later patch request, " +
                "not JSON. summary must be exactly one concise sentence in the " +
                "player's language describing what will feel different, without " +
                "DSL names or field labels. Return concise player-facing JSON only.";
            string input =
                "PLAYER FEEDBACK:\n" + request.UserPrompt +
                "\n\nCURRENT CHALLENGE JSON:\n" +
                JsonUtility.ToJson(request.CurrentChallenge, true) +
                "\n\nCREATOR PREFERENCES:\n" + request.CreatorPreferences +
                "\n\nDESIGN GUIDANCE:\n" + GameplayVocabulary.BuildDesignGuidance() +
                "\n\nGAMEPLAY VOCABULARY:\n" + request.GameplayVocabulary;
            AIGameplayResult<string> response = null;
            yield return SendStructuredRequest(
                instructions,
                input,
                "ruleforge_modification_proposal",
                request.OutputSchema,
                result => response = result);

            onComplete?.Invoke(
                ParseStructured<GameplayModificationProposal>(response));
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
                "numbers and ratios exactly as requested; do not silently clamp " +
                "them because the real Validator and Repairer own limits. Use " +
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

        public IEnumerator RepairChallenge(
            AIChallengeRepairRequest request,
            Action<AIGameplayResult<GameplayRepairResult>> onComplete)
        {
            if (request == null || request.InvalidChallenge == null ||
                string.IsNullOrWhiteSpace(request.ValidationContext))
            {
                onComplete?.Invoke(AIGameplayResult<GameplayRepairResult>.Failed(
                    "An invalid challenge and real validation context are required."));
                yield break;
            }

            string instructions =
                "Act as RuleForge's Repairer. Repair the invalid ChallengeSpec using " +
                "the exact Validator errors and warnings supplied. The Validator is " +
                "the final authority: never bypass it or reinterpret its limits. " +
                "For every effectId, copy kind, target, statId, operation and " +
                "stringValue exactly from that effect's catalog mapping. " +
                "A missing-health damage bonus cannot use a negative value; " +
                "use the separate movement-speed penalty effect for a " +
                "negative movement modifier. Check every effect-specific value " +
                "range before returning. If a mechanic has no legal representation, " +
                "do not disguise it as another effect. " +
                "Preserve the confirmed player intent and every unrelated field. " +
                "The confirmed proposal's requiredMechanics, requiredGoalType, " +
                "requiredGoalTarget and requiredTimeLimitSeconds are " +
                "non-negotiable. Do not add any unconfirmed effects. If an exact " +
                "stack limit was requested, preserve maxStacks for that effect. " +
                "If EnemyKilled has an invalid EventValue condition intended " +
                "to count kills, remove only that condition and keep the " +
                "per-kill stack effect. Never convert the survival goal to a " +
                "kill goal or split one effect into milestone rules. " +
                "If an exact " +
                "requested value is illegal, do not replace its effect with " +
                "an unrelated one; the application will reject that repair. " +
                "For modifications, compare against the previous challenge and keep " +
                "the repair minimal. Balance context may guide an explanation but " +
                "must not force balance on an intentionally extreme legal design. " +
                "Return a repaired full ChallengeSpec plus concise player-facing " +
                "changes. Do not reveal hidden reasoning or return markdown.";
            string input =
                "PLAYER REQUEST:\n" + request.OriginalPrompt +
                "\n\nCONFIRMED PROPOSAL JSON (may be none):\n" +
                (request.ConfirmedProposal != null
                    ? JsonUtility.ToJson(request.ConfirmedProposal, true)
                    : "none") +
                "\n\nINVALID CHALLENGE JSON:\n" +
                JsonUtility.ToJson(request.InvalidChallenge, true) +
                "\n\nPREVIOUS CHALLENGE JSON (may be none):\n" +
                (request.PreviousChallenge != null
                    ? JsonUtility.ToJson(request.PreviousChallenge, true)
                    : "none") +
                "\n\nREAL VALIDATOR RESULT:\n" + request.ValidationContext +
                "\n\nREAL BALANCE RESULT:\n" + request.BalanceContext +
                "\n\nGAMEPLAY VOCABULARY:\n" + request.GameplayVocabulary;
            AIGameplayResult<string> response = null;
            yield return SendStructuredRequest(
                instructions,
                input,
                "ruleforge_challenge_repair",
                request.OutputSchema,
                result => response = result);

            onComplete?.Invoke(ParseStructured<GameplayRepairResult>(response));
        }

        public IEnumerator AnalyzeImprovements(
            AIImprovementAnalysisRequest request,
            Action<AIGameplayResult<GameplayImprovementSet>> onComplete)
        {
            if (request == null || request.CurrentChallenge == null)
            {
                onComplete?.Invoke(AIGameplayResult<GameplayImprovementSet>.Failed(
                    "A current challenge is required."));
                yield break;
            }

            string instructions =
                "Act as RuleForge's Gameplay Critic. Analyze the real current " +
                "ChallengeSpec and provide at most three materially distinct, " +
                "optional improvements. Look for missing risk/reward tension, " +
                "resource pressure, scaling shape, useful randomness, or a clearer " +
                "gameplay loop. Do not assume every challenge needs balancing, and " +
                "do not rewrite its core identity. Each intent must be a complete " +
                "natural-language request suitable for the existing Modifier; it " +
                "must not be JSON or executable UI logic. Use only the supplied " +
                "vocabulary. Return concise player-facing JSON only.";
            string input =
                "CURRENT CHALLENGE JSON:\n" +
                JsonUtility.ToJson(request.CurrentChallenge, true) +
                "\n\nCREATOR PREFERENCES:\n" + request.CreatorPreferences +
                "\n\nDESIGN GUIDANCE:\n" + GameplayVocabulary.BuildDesignGuidance() +
                "\n\nGAMEPLAY VOCABULARY:\n" + request.GameplayVocabulary;
            AIGameplayResult<string> response = null;
            yield return SendStructuredRequest(
                instructions,
                input,
                "ruleforge_improvement_suggestions",
                request.OutputSchema,
                result => response = result);

            onComplete?.Invoke(ParseStructured<GameplayImprovementSet>(response));
        }

        public IEnumerator RepairPatch(
            AIChallengePatchRepairRequest request,
            Action<AIGameplayResult<ChallengePatch>> onComplete)
        {
            if (request == null || request.CurrentChallenge == null ||
                request.RejectedPatch == null ||
                string.IsNullOrWhiteSpace(request.PatchError))
            {
                onComplete?.Invoke(AIGameplayResult<ChallengePatch>.Failed(
                    "A rejected patch and its real application error are required."));
                yield break;
            }

            string instructions =
                "Act as RuleForge's Patch Repairer. Return one corrected " +
                "ChallengePatch JSON object using the exact patch application " +
                "error supplied. Preserve the player's requested smallest change. " +
                "Do not add unrelated operations, replace the whole challenge, or " +
                "bypass the patch applier. Use only supplied operations and " +
                "vocabulary. Return no prose and no markdown.";
            string input =
                "PLAYER MODIFICATION:\n" + request.OriginalPrompt +
                "\n\nCURRENT CHALLENGE JSON:\n" +
                JsonUtility.ToJson(request.CurrentChallenge, true) +
                "\n\nREJECTED PATCH JSON:\n" +
                JsonUtility.ToJson(request.RejectedPatch, true) +
                "\n\nREAL PATCH APPLICATION ERROR:\n" + request.PatchError +
                "\n\nCREATOR PREFERENCES:\n" + request.CreatorPreferences +
                "\n\nGAMEPLAY VOCABULARY:\n" + request.GameplayVocabulary;
            AIGameplayResult<string> response = null;
            yield return SendStructuredRequest(
                instructions,
                input,
                "ruleforge_repaired_challenge_patch",
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
            if (!TryResolveConnection(
                    out string resolvedEndpoint,
                    out string resolvedModel,
                    out string authorization,
                    out string configurationError))
            {
                onComplete?.Invoke(AIGameplayResult<string>.Failed(
                    configurationError));
                yield break;
            }

            string languageInstructions = RuleForgeLocalization.Current ==
                                          RuleForgeLanguage.Chinese
                ? "Write every player-facing string in Simplified Chinese, including summary, detectedIntent, suggestedRules, designReasoningSummary, warnings, clarificationQuestion, actionSuggestions, repair explanations and change descriptions. Do not leave any of these in English. "
                : "Write every player-facing string in English, including summary, detectedIntent, suggestedRules, designReasoningSummary, warnings, clarificationQuestion, actionSuggestions, repair explanations and change descriptions. ";
            string requestJson = BuildRequestJson(
                instructions + " " + languageInstructions +
                " Keep JSON property names, enum values, effect IDs and other machine-readable identifiers exactly as specified by the schema and vocabulary.",
                input,
                schemaName,
                schema,
                resolvedModel);
            RuntimeAIProtocol requestProtocol =
                RuntimeOpenAICredentials.ConfiguredProtocol;
            string requestSignature = BuildConnectionSignature(
                resolvedEndpoint,
                resolvedModel,
                authorization,
                requestProtocol);
            using (UnityWebRequest request = new UnityWebRequest(
                       resolvedEndpoint,
                       "POST"))
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
                    string failure =
                        $"AI request failed ({request.responseCode}): {message}";
                    MarkConnectionFailed(requestSignature, failure);
                    onComplete?.Invoke(AIGameplayResult<string>.Failed(failure));
                    yield break;
                }

                string outputText;
                string parseError;
                if (!TryReadOutputText(
                        request.downloadHandler.text,
                        requestProtocol,
                        out outputText,
                        out parseError))
                {
                    MarkConnectionFailed(requestSignature, parseError);
                    onComplete?.Invoke(AIGameplayResult<string>.Failed(parseError));
                    yield break;
                }

                MarkConnectionVerified(requestSignature);
                onComplete?.Invoke(AIGameplayResult<string>.Succeeded(outputText));
            }
        }

        private string BuildRequestJson(
            string instructions,
            string input,
            string schemaName,
            string schema,
            string requestModel)
        {
            if (RuntimeOpenAICredentials.ConfiguredProtocol ==
                RuntimeAIProtocol.ChatCompletions)
            {
                string schemaInstruction = instructions +
                    "\nReturn one JSON object matching this schema exactly:\n" +
                    schema;
                return "{" +
                       "\"model\":\"" +
                       GameplayVocabulary.EscapeJson(requestModel) + "\"," +
                       "\"max_tokens\":" +
                       Mathf.Max(256, maxOutputTokens) + "," +
                       "\"messages\":[{" +
                       "\"role\":\"system\",\"content\":\"" +
                       GameplayVocabulary.EscapeJson(schemaInstruction) +
                       "\"},{\"role\":\"user\",\"content\":\"" +
                       GameplayVocabulary.EscapeJson(input) + "\"}]," +
                       "\"response_format\":{\"type\":\"json_object\"}}";
            }

            bool useDeepSeekContract =
                RuntimeOpenAICredentials.SelectedProvider ==
                RuntimeAIProvider.DeepSeek;
            return "{" +
                   "\"model\":\"" + GameplayVocabulary.EscapeJson(requestModel) + "\"," +
                   (useDeepSeekContract ? string.Empty : "\"store\":false,") +
                   "\"max_output_tokens\":" + Mathf.Max(256, maxOutputTokens) + "," +
                   (useDeepSeekContract
                       ? "\"reasoning\":{\"effort\":\"none\"},"
                       : string.Empty) +
                   "\"instructions\":\"" + GameplayVocabulary.EscapeJson(instructions) + "\"," +
                   "\"input\":\"" + GameplayVocabulary.EscapeJson(input) + "\"," +
                   "\"text\":{\"format\":{" +
                   "\"type\":\"json_schema\"," +
                   "\"name\":\"" + GameplayVocabulary.EscapeJson(schemaName) + "\"," +
                   (useDeepSeekContract ? string.Empty : "\"strict\":true,") +
                   "\"schema\":" + schema +
                   "}}}";
        }

        private bool TryResolveConnection(
            out string resolvedEndpoint,
            out string resolvedModel,
            out string authorization,
            out string error)
        {
            resolvedEndpoint = RuntimeOpenAICredentials.ConfiguredEndpoint;
            resolvedModel = RuntimeOpenAICredentials.ConfiguredModel;
            authorization = string.Empty;
            if (string.IsNullOrWhiteSpace(resolvedEndpoint) ||
                !Uri.TryCreate(resolvedEndpoint, UriKind.Absolute, out Uri uri))
            {
                error = "AI endpoint must be an absolute URL.";
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps &&
                !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
            {
                error = "AI endpoint must use HTTPS; HTTP is allowed only for localhost.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(resolvedModel))
            {
                error = "AI model is not configured.";
                return false;
            }

            string environmentVariable =
                RuntimeOpenAICredentials.GetEnvironmentVariableName(
                    RuntimeOpenAICredentials.SelectedProvider);

            authorization = Environment.GetEnvironmentVariable(environmentVariable);
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
                ProviderName + " API Key 未配置。请在开发者视图中输入 Key，" +
                "或通过 " + environmentVariable + " 环境变量配置。";
            return false;
        }

        private static string BuildConnectionSignature(
            string resolvedEndpoint,
            string resolvedModel,
            string authorization,
            RuntimeAIProtocol protocol)
        {
            return resolvedEndpoint + "\n" + resolvedModel + "\n" +
                   protocol + "\n" +
                   (authorization ?? string.Empty).GetHashCode();
        }

        private void MarkConnectionVerified(string signature)
        {
            connectionSignature = signature;
            connectionState = AIProviderConnectionState.Verified;
            connectionMessage = ProviderName + " connection verified.";
        }

        private void MarkConnectionFailed(string signature, string message)
        {
            connectionSignature = signature;
            connectionState = AIProviderConnectionState.Failed;
            connectionMessage = message ?? "Provider connection failed.";
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
            RuntimeAIProtocol protocol,
            out string outputText,
            out string error)
        {
            outputText = string.Empty;
            if (protocol == RuntimeAIProtocol.ChatCompletions)
            {
                try
                {
                    OpenAIChatResponse chatResponse =
                        JsonUtility.FromJson<OpenAIChatResponse>(json);
                    if (chatResponse?.choices != null)
                    {
                        for (int index = 0;
                             index < chatResponse.choices.Length;
                             index++)
                        {
                            string content =
                                chatResponse.choices[index]?.message?.content;
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                outputText = content;
                                error = string.Empty;
                                return true;
                            }
                        }
                    }
                }
                catch (ArgumentException exception)
                {
                    error =
                        $"Chat Completions response JSON is invalid: {exception.Message}";
                    return false;
                }

                error =
                    "Chat Completions response did not contain structured JSON content.";
                return false;
            }

            OpenAIResponse response;
            try
            {
                response = JsonUtility.FromJson<OpenAIResponse>(json);
            }
            catch (ArgumentException exception)
            {
                error = $"Responses API JSON is invalid: {exception.Message}";
                return false;
            }

            if (response == null)
            {
                error = "Responses API returned an unreadable response.";
                return false;
            }

            if (string.Equals(response.status, "failed",
                    StringComparison.OrdinalIgnoreCase))
            {
                error = response.error != null &&
                        !string.IsNullOrWhiteSpace(response.error.message)
                    ? response.error.message
                    : "Responses API reported a failed response.";
                return false;
            }

            if (string.Equals(response.status, "incomplete",
                    StringComparison.OrdinalIgnoreCase))
            {
                string reason = response.incomplete_details != null
                    ? response.incomplete_details.reason
                    : string.Empty;
                error = string.Equals(reason, "max_output_tokens",
                    StringComparison.OrdinalIgnoreCase)
                    ? "AI 输出额度耗尽，未生成可用的玩法方案。请重试；若持续出现，请提高 Provider 的 maxOutputTokens。"
                    : "AI 响应未完成" +
                      (string.IsNullOrWhiteSpace(reason)
                          ? "。"
                          : "（" + reason + "）。");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(response.output_text))
            {
                outputText = response.output_text;
                error = string.Empty;
                return true;
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

                        if ((string.Equals(
                                 content.type,
                                 "output_text",
                                 StringComparison.OrdinalIgnoreCase) ||
                             (string.Equals(item.type, "message",
                                  StringComparison.OrdinalIgnoreCase) &&
                              string.IsNullOrWhiteSpace(content.type))) &&
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

            string outputType = response.output != null &&
                                response.output.Length > 0 &&
                                response.output[0] != null
                ? response.output[0].type
                : "none";
            error = "Responses API 返回成功但没有可用的结构化文本" +
                    "（status=" + (response.status ?? "unknown") +
                    ", first output=" + (outputType ?? "unknown") + "）。";
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
            public string status;
            public string output_text;
            public OpenAIIncompleteDetails incomplete_details;
            public OpenAIError error;
            public OpenAIOutputItem[] output;
        }

        [Serializable]
        private sealed class OpenAIIncompleteDetails
        {
            public string reason;
        }

        [Serializable]
        private sealed class OpenAIChatResponse
        {
            public OpenAIChatChoice[] choices;
        }

        [Serializable]
        private sealed class OpenAIChatChoice
        {
            public OpenAIChatMessage message;
        }

        [Serializable]
        private sealed class OpenAIChatMessage
        {
            public string content;
        }

        [Serializable]
        private sealed class OpenAIOutputItem
        {
            public string type;
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
