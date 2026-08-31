using System;
using System.Globalization;
using RuleForge.AI;
using RuleForge.DSL;
using RuleForge.Rules;
using RuleForge.Runtime.Goals;
using UnityEngine;

namespace RuleForge.Analytics
{
    [DisallowMultipleComponent]
    public sealed class AnalyticsRecorder : MonoBehaviour
    {
        [SerializeField] private RuleEngine ruleEngine;
        [SerializeField] private ChallengeGoalController goalController;
        [SerializeField] private AIGameplayController aiController;

        [Header("Runtime Debug")]
        [SerializeField] private bool sessionActive;
        [SerializeField] private string activeChallengeName;
        [SerializeField] private int currentKills;
        [SerializeField] private int currentHeadshots;
        [SerializeField] private int currentRulesTriggered;
        [SerializeField] private AnalyticsSummary latestSummary =
            new AnalyticsSummary();

        private string activeChallengeId;
        private string sessionStartedUtc;
        private float sessionStartedAt;
        private AIGenerationRecord latestAIRecord;
        private GameplaySessionRecord latestGameplayRecord;
        private bool subscribed;
        private string pendingOutcome;
        private bool pendingVictory;

        public bool SessionActive => sessionActive;
        public string ActiveChallengeName => activeChallengeName ?? string.Empty;
        public int CurrentKills => currentKills;
        public int CurrentHeadshots => currentHeadshots;
        public int CurrentRulesTriggered => currentRulesTriggered;
        public float CurrentPlayDuration => sessionActive
            ? Mathf.Max(0f, Time.unscaledTime - sessionStartedAt)
            : 0f;
        public AnalyticsSummary LatestSummary => latestSummary;
        public AIGenerationRecord LatestAIRecord => latestAIRecord;
        public GameplaySessionRecord LatestGameplayRecord => latestGameplayRecord;
        public string OutputDirectory => AnalyticsStorage.OutputDirectory;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResolveReferences();
            Subscribe();
            RefreshSummary();
        }

        private void Start()
        {
            if (Application.isPlaying &&
                !sessionActive &&
                ruleEngine != null &&
                ruleEngine.ActiveChallenge != null)
            {
                BeginSession(ruleEngine.ActiveChallenge);
            }
        }

        private void OnDisable()
        {
            if (sessionActive)
            {
                FinishSession(
                    string.IsNullOrWhiteSpace(pendingOutcome)
                        ? "Stopped"
                        : pendingOutcome,
                    pendingVictory);
            }

            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (sessionActive && !string.IsNullOrWhiteSpace(pendingOutcome))
            {
                FinishSession(pendingOutcome, pendingVictory);
            }
        }

        public void Configure(
            RuleEngine engine,
            ChallengeGoalController goals,
            AIGameplayController ai)
        {
            Unsubscribe();
            ruleEngine = engine;
            goalController = goals;
            aiController = ai;
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void RefreshSummary()
        {
            try
            {
                latestSummary = AnalyticsStorage.RebuildSummary();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "RuleForge analytics summary could not be rebuilt: " +
                    exception.Message,
                    this);
            }
        }

        private void HandleAIRequestCompleted(AIGenerationTelemetry telemetry)
        {
            if (telemetry == null)
            {
                return;
            }

            bool validationPassed = telemetry.Validation != null &&
                                    telemetry.Validation.IsValid;
            string validationResult = telemetry.Validation != null
                ? telemetry.Validation.BuildSummary()
                : telemetry.ResultMessage;
            AIGenerationRecord record = new AIGenerationRecord
            {
                timestampUtc = UtcNow(),
                requestType = telemetry.RequestType,
                provider = telemetry.Provider,
                prompt = telemetry.Prompt,
                generationTimeMs = (float)telemetry.GenerationTimeMs,
                parseSuccess = telemetry.ParseSuccess,
                validationPassed = validationPassed,
                validationResult = validationResult ?? string.Empty,
                ruleCount = telemetry.Challenge != null
                    ? telemetry.Challenge.Rules.Length
                    : 0,
                rewardScore = telemetry.Balance != null
                    ? telemetry.Balance.RewardScore
                    : 0f,
                penaltyScore = telemetry.Balance != null
                    ? telemetry.Balance.PenaltyScore
                    : 0f,
                isRetry = AnalyticsStorage.WasPreviousMatchingAttemptFailure(
                    telemetry.RequestType,
                    telemetry.Provider,
                    telemetry.Prompt),
                benchmarkEligible = telemetry.BenchmarkEligible
            };

            if (!AnalyticsStorage.AppendAIRecord(record, out string error))
            {
                Debug.LogError(
                    "RuleForge AI analytics record failed: " + error,
                    this);
                return;
            }

            latestAIRecord = record;
            RefreshSummary();
        }

        private void HandleChallengeRestarted(ChallengeSpec challenge)
        {
            if (sessionActive)
            {
                FinishSession(
                    string.IsNullOrWhiteSpace(pendingOutcome)
                        ? "Restarted"
                        : pendingOutcome,
                    pendingVictory);
            }

            BeginSession(challenge);
        }

        private void HandleGoalStateChanged(ChallengeGoalState state)
        {
            if (!sessionActive)
            {
                return;
            }

            if (state == ChallengeGoalState.Victory)
            {
                pendingOutcome = "Victory";
                pendingVictory = true;
            }
            else if (state == ChallengeGoalState.Defeat)
            {
                pendingOutcome = "Defeat";
                pendingVictory = false;
            }
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (!sessionActive)
            {
                return;
            }

            if (gameplayEvent.Type == GameplayEventType.EnemyKilled)
            {
                currentKills++;
            }
            else if (gameplayEvent.Type == GameplayEventType.Headshot)
            {
                currentHeadshots++;
            }
        }

        private void HandleRuleTriggered(RuleTriggerFeedback feedback)
        {
            if (sessionActive && feedback != null)
            {
                currentRulesTriggered++;
            }
        }

        private void BeginSession(ChallengeSpec challenge)
        {
            if (challenge == null)
            {
                return;
            }

            activeChallengeId = challenge.Id;
            activeChallengeName = string.IsNullOrWhiteSpace(challenge.DisplayName)
                ? challenge.Id
                : challenge.DisplayName;
            currentKills = 0;
            currentHeadshots = 0;
            currentRulesTriggered = 0;
            sessionStartedUtc = UtcNow();
            sessionStartedAt = Time.unscaledTime;
            pendingOutcome = string.Empty;
            pendingVictory = false;
            sessionActive = true;
        }

        private void FinishSession(string outcome, bool victory)
        {
            if (!sessionActive)
            {
                return;
            }

            GameplaySessionRecord record = new GameplaySessionRecord
            {
                timestampUtc = sessionStartedUtc,
                challengeId = activeChallengeId ?? string.Empty,
                challenge = activeChallengeName ?? string.Empty,
                playDurationSeconds = Mathf.Max(
                    0f,
                    Time.unscaledTime - sessionStartedAt),
                victory = victory,
                outcome = outcome ?? string.Empty,
                kills = currentKills,
                headshots = currentHeadshots,
                rulesTriggered = currentRulesTriggered
            };
            sessionActive = false;
            pendingOutcome = string.Empty;
            pendingVictory = false;

            if (!AnalyticsStorage.AppendGameplayRecord(record, out string error))
            {
                Debug.LogError(
                    "RuleForge gameplay analytics record failed: " + error,
                    this);
                return;
            }

            latestGameplayRecord = record;
            RefreshSummary();
        }

        private void ResolveReferences()
        {
            ruleEngine = ruleEngine != null
                ? ruleEngine
                : FindObjectOfType<RuleEngine>();
            goalController = goalController != null
                ? goalController
                : FindObjectOfType<ChallengeGoalController>();
            aiController = aiController != null
                ? aiController
                : FindObjectOfType<AIGameplayController>();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (ruleEngine != null)
            {
                ruleEngine.ChallengeRestarted += HandleChallengeRestarted;
                ruleEngine.RuleTriggered += HandleRuleTriggered;
            }

            if (goalController != null)
            {
                goalController.StateChanged += HandleGoalStateChanged;
            }

            if (aiController != null)
            {
                aiController.RequestCompleted += HandleAIRequestCompleted;
            }

            GameplayEventBus.EventPublished += HandleGameplayEvent;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (ruleEngine != null)
            {
                ruleEngine.ChallengeRestarted -= HandleChallengeRestarted;
                ruleEngine.RuleTriggered -= HandleRuleTriggered;
            }

            if (goalController != null)
            {
                goalController.StateChanged -= HandleGoalStateChanged;
            }

            if (aiController != null)
            {
                aiController.RequestCompleted -= HandleAIRequestCompleted;
            }

            GameplayEventBus.EventPublished -= HandleGameplayEvent;
            subscribed = false;
        }

        private static string UtcNow()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
