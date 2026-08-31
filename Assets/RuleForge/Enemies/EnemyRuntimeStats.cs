using RuleForge.Config;
using RuleForge.Runtime.Stats;
using UnityEngine;

namespace RuleForge.Enemies
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class EnemyRuntimeStats : MonoBehaviour, IRuntimeStatProvider
    {
        [SerializeField] private EnemyConfig config;

        [Header("Runtime Stat Debug")]
        [SerializeField] private RuntimeStat maxHealth = new RuntimeStat();
        [SerializeField] private RuntimeStat moveSpeed = new RuntimeStat();
        [SerializeField] private RuntimeStat stoppingDistance = new RuntimeStat();
        [SerializeField] private RuntimeStat turnSpeed = new RuntimeStat();
        [SerializeField] private RuntimeStat contactDamage = new RuntimeStat();
        [SerializeField] private RuntimeStat attacksPerSecond = new RuntimeStat();
        [SerializeField] private RuntimeStat desiredAliveEnemies = new RuntimeStat();
        [SerializeField] private RuntimeStat respawnDelay = new RuntimeStat();

        public EnemyConfig Config => config;
        public bool IsConfigured => config != null;
        public RuntimeStat MaxHealthStat => maxHealth;
        public RuntimeStat MoveSpeedStat => moveSpeed;
        public RuntimeStat StoppingDistanceStat => stoppingDistance;
        public RuntimeStat TurnSpeedStat => turnSpeed;
        public RuntimeStat ContactDamageStat => contactDamage;
        public RuntimeStat AttacksPerSecondStat => attacksPerSecond;
        public RuntimeStat DesiredAliveEnemiesStat => desiredAliveEnemies;
        public RuntimeStat RespawnDelayStat => respawnDelay;

        private void Awake()
        {
            RefreshBaseValues();
        }

        private void Start()
        {
            if (!IsConfigured)
            {
                Debug.LogError("EnemyRuntimeStats requires an EnemyConfig.", this);
            }
        }

        private void OnValidate()
        {
            RefreshBaseValues();
        }

        public void Configure(EnemyConfig newConfig)
        {
            config = newConfig;
            RefreshBaseValues();
        }

        public void RefreshBaseValues()
        {
            if (config == null)
            {
                return;
            }

            maxHealth.Initialize(RuntimeStatId.EnemyMaxHealth, config.MaxHealth, 1f);
            moveSpeed.Initialize(RuntimeStatId.EnemyMoveSpeed, config.MoveSpeed);
            stoppingDistance.Initialize(
                RuntimeStatId.EnemyStoppingDistance,
                config.StoppingDistance);
            turnSpeed.Initialize(RuntimeStatId.EnemyTurnSpeed, config.TurnSpeed);
            contactDamage.Initialize(RuntimeStatId.EnemyContactDamage, config.ContactDamage);
            attacksPerSecond.Initialize(
                RuntimeStatId.EnemyAttacksPerSecond,
                config.AttacksPerSecond,
                0.01f);
            desiredAliveEnemies.Initialize(
                RuntimeStatId.EnemyDesiredAliveCount,
                config.DesiredAliveEnemies,
                1f);
            respawnDelay.Initialize(RuntimeStatId.EnemyRespawnDelay, config.RespawnDelay);
        }

        public bool TryGetStat(RuntimeStatId statId, out RuntimeStat runtimeStat)
        {
            switch (statId)
            {
                case RuntimeStatId.EnemyMaxHealth:
                    runtimeStat = maxHealth;
                    return true;
                case RuntimeStatId.EnemyMoveSpeed:
                    runtimeStat = moveSpeed;
                    return true;
                case RuntimeStatId.EnemyStoppingDistance:
                    runtimeStat = stoppingDistance;
                    return true;
                case RuntimeStatId.EnemyTurnSpeed:
                    runtimeStat = turnSpeed;
                    return true;
                case RuntimeStatId.EnemyContactDamage:
                    runtimeStat = contactDamage;
                    return true;
                case RuntimeStatId.EnemyAttacksPerSecond:
                    runtimeStat = attacksPerSecond;
                    return true;
                case RuntimeStatId.EnemyDesiredAliveCount:
                    runtimeStat = desiredAliveEnemies;
                    return true;
                case RuntimeStatId.EnemyRespawnDelay:
                    runtimeStat = respawnDelay;
                    return true;
                default:
                    runtimeStat = null;
                    return false;
            }
        }

        public void ClearAllModifiers()
        {
            maxHealth.ClearModifiers();
            moveSpeed.ClearModifiers();
            stoppingDistance.ClearModifiers();
            turnSpeed.ClearModifiers();
            contactDamage.ClearModifiers();
            attacksPerSecond.ClearModifiers();
            desiredAliveEnemies.ClearModifiers();
            respawnDelay.ClearModifiers();
        }
    }
}
