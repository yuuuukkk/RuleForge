using System;
using RuleForge.DSL;
using RuleForge.Player;
using RuleForge.Rules;
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
        [SerializeField, Min(1)] private int scorePerKill = 100;

        private ChallengeGoalType goalType;
        private float goalTarget;
        private float survivedSeconds;
        private int killCount;
        private int score;
        private ChallengeGoalState state = ChallengeGoalState.Inactive;

        public event Action<ChallengeGoalState> StateChanged;

        public ChallengeGoalType GoalType => goalType;
        public float GoalTarget => goalTarget;
        public float SurvivedSeconds => survivedSeconds;
        public int KillCount => killCount;
        public int Score => score;
        public ChallengeGoalState State => state;
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
            if (state != ChallengeGoalState.Running ||
                goalType != ChallengeGoalType.Survive)
            {
                return;
            }

            survivedSeconds += Time.deltaTime;
            if (survivedSeconds >= goalTarget)
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
                        $"击杀数  {killCount} / {Mathf.CeilToInt(goalTarget)}";
                case ChallengeGoalType.Score:
                    return $"分数  {score} / {Mathf.CeilToInt(goalTarget)}";
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
            survivedSeconds = 0f;
            killCount = 0;
            score = 0;
            if (challenge == null ||
                !Enum.TryParse(challenge.Goal, true, out goalType) ||
                challenge.GoalTarget <= 0f)
            {
                SetState(ChallengeGoalState.Inactive);
                return;
            }

            goalTarget = challenge.GoalTarget;
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
        }
    }
}
