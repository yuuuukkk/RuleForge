using System;
using RuleForge.DSL;
using RuleForge.Player;
using RuleForge.Rules;
using RuleForge.Runtime.Services;
using RuleForge.Weapons;
using UnityEngine;

namespace RuleForge.Runtime.Goals
{
    public enum ChallengeGoalState
    {
        Inactive,
        Running,
        Victory,
        Defeat
    }

    [DisallowMultipleComponent]
    public sealed class ChallengeGoalController : MonoBehaviour
    {
        [SerializeField] private RuleEngine ruleEngine;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField] private TimeRuntimeService timeService;
        [SerializeField, Min(1)] private int scorePerKill = 100;

        private ChallengeGoalType goalType;
        private float goalTarget;
        private float timeLimit;
        private float survivedSeconds;
        private int killCount;
        private int score;
        private ChallengeGoalState state = ChallengeGoalState.Inactive;
        private bool lastDefeatWasTimeout;

        public event Action<ChallengeGoalState> StateChanged;

        public ChallengeGoalType GoalType => goalType;
        public float GoalTarget => goalTarget;
        public float TimeLimit => timeLimit;
        public bool UsesTimeAsHealth => goalType == ChallengeGoalType.TimeBankTarget ||
                                        goalType == ChallengeGoalType.TimeBankSurvive ||
                                        goalType == ChallengeGoalType.TimeBankEndless;
        public float TimeDamageScale => playerHealth != null
            ? playerHealth.TimeDamageScale
            : 0f;
        public float RemainingSeconds => timeService != null
            ? timeService.RemainingSeconds
            : 0f;
        public float SurvivedSeconds => survivedSeconds;
        public int KillCount => killCount;
        public int Score => score;
        public ChallengeGoalState State => state;
        public bool LastDefeatWasTimeout => lastDefeatWasTimeout;
        public float ProgressNormalized
        {
            get
            {
                if (goalType == ChallengeGoalType.TimeBankEndless)
                {
                    return Mathf.Clamp01(survivedSeconds / 60f);
                }

                if (goalTarget <= 0f)
                {
                    return 0f;
                }

                switch (goalType)
                {
                    case ChallengeGoalType.Survive:
                        return Mathf.Clamp01(survivedSeconds / goalTarget);
                    case ChallengeGoalType.KillCount:
                        return Mathf.Clamp01(killCount / goalTarget);
                    case ChallengeGoalType.Score:
                        return Mathf.Clamp01(score / goalTarget);
                    case ChallengeGoalType.TimeBankTarget:
                        return Mathf.Clamp01(RemainingSeconds / goalTarget);
                    case ChallengeGoalType.TimeBankSurvive:
                        return Mathf.Clamp01(survivedSeconds / goalTarget);
                    default:
                        return 0f;
                }
            }
        }
        public float PlayerCurrentHealth => playerHealth != null
            ? playerHealth.CurrentHealth
            : 0f;
        public float PlayerMaxHealth => playerHealth != null
            ? playerHealth.MaxHealth
            : 0f;
        public string ActiveWeaponName => weaponLoadout != null
            ? weaponLoadout.CurrentWeaponName
            : "无武器";

        private void OnEnable()
        {
            ResolveReferences();
            if (ruleEngine != null)
            {
                ruleEngine.ChallengeRestarted -= HandleChallengeRestarted;
                ruleEngine.ChallengeRestarted += HandleChallengeRestarted;
            }

            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
                playerHealth.Died += HandlePlayerDied;
            }

            GameplayEventBus.EventPublished += HandleGameplayEvent;
        }

        private void Start()
        {
            if (state == ChallengeGoalState.Inactive && ruleEngine != null)
            {
                BeginChallenge(ruleEngine.ActiveChallenge);
            }
        }

        private void Update()
        {
            if (state != ChallengeGoalState.Running)
            {
                return;
            }

            if (timeLimit > 0f && RemainingSeconds <= 0f)
            {
                lastDefeatWasTimeout = true;
                SetState(ChallengeGoalState.Defeat);
                return;
            }

            if (goalType == ChallengeGoalType.TimeBankTarget &&
                RemainingSeconds >= goalTarget)
            {
                SetState(ChallengeGoalState.Victory);
                return;
            }

            if (goalType != ChallengeGoalType.Survive &&
                goalType != ChallengeGoalType.TimeBankSurvive &&
                goalType != ChallengeGoalType.TimeBankEndless &&
                goalType != ChallengeGoalType.TimeBankTarget)
            {
                return;
            }

            survivedSeconds += Time.deltaTime;
            if ((goalType == ChallengeGoalType.Survive ||
                 goalType == ChallengeGoalType.TimeBankSurvive) &&
                survivedSeconds >= goalTarget)
            {
                survivedSeconds = goalTarget;
                SetState(ChallengeGoalState.Victory);
            }
        }

        private void OnDisable()
        {
            GameplayEventBus.EventPublished -= HandleGameplayEvent;
            if (ruleEngine != null)
            {
                ruleEngine.ChallengeRestarted -= HandleChallengeRestarted;
            }

            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
            }
        }

        public void Configure(
            RuleEngine engine,
            PlayerHealth health,
            WeaponLoadout loadout)
        {
            ruleEngine = engine;
            playerHealth = health;
            weaponLoadout = loadout;
        }

        public string BuildProgressText()
        {
            switch (goalType)
            {
                case ChallengeGoalType.Survive:
                    return $"生存时间  {survivedSeconds:0.0} / {goalTarget:0.0} 秒";
                case ChallengeGoalType.KillCount:
                    return
                        $"击杀数  {killCount} / {Mathf.CeilToInt(goalTarget)}" +
                        (timeLimit > 0f
                            ? $"  ·  剩余 {RemainingSeconds:0.0} 秒"
                            : string.Empty);
                case ChallengeGoalType.Score:
                    return $"分数  {score} / {Mathf.CeilToInt(goalTarget)}" +
                           (timeLimit > 0f
                               ? $"  ·  剩余 {RemainingSeconds:0.0} 秒"
                               : string.Empty);
                case ChallengeGoalType.TimeBankTarget:
                    return $"时间生命  {RemainingSeconds:0.0} / " +
                           $"{goalTarget:0.#} 秒";
                case ChallengeGoalType.TimeBankSurvive:
                    return $"坚持  {survivedSeconds:0.0} / {goalTarget:0.#} 秒";
                case ChallengeGoalType.TimeBankEndless:
                    return $"无尽试炼  已坚持 {survivedSeconds:0.0} 秒";
                default:
                    return "目标不可用";
            }
        }

        private void HandleChallengeRestarted(ChallengeSpec challenge)
        {
            BeginChallenge(challenge);
        }

        private void BeginChallenge(ChallengeSpec challenge)
        {
            ResolveReferences();
            survivedSeconds = 0f;
            killCount = 0;
            score = 0;
            timeLimit = 0f;
            lastDefeatWasTimeout = false;
            timeService?.BeginCountdown(0f);
            if (challenge == null ||
                !Enum.TryParse(challenge.Goal, true, out goalType) ||
                (goalType == ChallengeGoalType.TimeBankEndless
                    ? challenge.GoalTarget != 0f
                    : challenge.GoalTarget <= 0f))
            {
                playerHealth?.ConfigureTimeHealth(null, 0f);
                SetState(ChallengeGoalState.Inactive);
                return;
            }

            goalTarget = challenge.GoalTarget;
            timeLimit = challenge.TimeLimit;
            timeService?.BeginCountdown(timeLimit);
            playerHealth?.ConfigureTimeHealth(
                UsesTimeAsHealth ? timeService : null,
                challenge.TimeDamageScale);
            SetState(ChallengeGoalState.Running);
            weaponLoadout?.EquipByName(challenge.Weapon);
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (state != ChallengeGoalState.Running ||
                gameplayEvent.Type != GameplayEventType.EnemyKilled)
            {
                return;
            }

            killCount++;
            score += Mathf.Max(1, scorePerKill);
            if ((goalType == ChallengeGoalType.KillCount &&
                 killCount >= Mathf.CeilToInt(goalTarget)) ||
                (goalType == ChallengeGoalType.Score &&
                 score >= Mathf.CeilToInt(goalTarget)))
            {
                SetState(ChallengeGoalState.Victory);
            }
        }

        private void HandlePlayerDied()
        {
            if (state == ChallengeGoalState.Running)
            {
                lastDefeatWasTimeout = false;
                SetState(ChallengeGoalState.Defeat);
            }
        }

        private void SetState(ChallengeGoalState newState)
        {
            if (state == newState)
            {
                return;
            }

            state = newState;
            StateChanged?.Invoke(state);
        }

        private void ResolveReferences()
        {
            ruleEngine = ruleEngine != null
                ? ruleEngine
                : FindObjectOfType<RuleEngine>();
            playerHealth = playerHealth != null
                ? playerHealth
                : FindObjectOfType<PlayerHealth>();
            weaponLoadout = weaponLoadout != null
                ? weaponLoadout
                : FindObjectOfType<WeaponLoadout>();
            timeService = timeService != null
                ? timeService
                : FindObjectOfType<TimeRuntimeService>();
        }
    }
}
