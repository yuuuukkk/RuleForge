using RuleForge.Config;
using RuleForge.Runtime.Stats;
using UnityEngine;

namespace RuleForge.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerRuntimeStats : MonoBehaviour, IRuntimeStatProvider
    {
        [SerializeField] private PlayerConfig config;

        [Header("Runtime Stat Debug")]
        [SerializeField] private RuntimeStat maxHealth = new RuntimeStat();
        [SerializeField] private RuntimeStat moveSpeed = new RuntimeStat();
        [SerializeField] private RuntimeStat jumpHeight = new RuntimeStat();
        [SerializeField] private RuntimeStat gravity = new RuntimeStat();
        [SerializeField] private RuntimeStat groundedVerticalVelocity = new RuntimeStat();
        [SerializeField] private RuntimeStat lookSensitivity = new RuntimeStat();
        [SerializeField] private RuntimeStat maximumLookAngle = new RuntimeStat();
        [SerializeField] private RuntimeStat enemyTopSlideSpeed = new RuntimeStat();

        public PlayerConfig Config => config;
        public bool IsConfigured => config != null;
        public RuntimeStat MaxHealthStat => maxHealth;
        public RuntimeStat MoveSpeedStat => moveSpeed;
        public RuntimeStat JumpHeightStat => jumpHeight;
        public RuntimeStat GravityStat => gravity;
        public RuntimeStat GroundedVerticalVelocityStat => groundedVerticalVelocity;
        public RuntimeStat LookSensitivityStat => lookSensitivity;
        public RuntimeStat MaximumLookAngleStat => maximumLookAngle;
        public RuntimeStat EnemyTopSlideSpeedStat => enemyTopSlideSpeed;

        private void Awake()
        {
            RefreshBaseValues();
        }

        private void Start()
        {
            if (!IsConfigured)
            {
                Debug.LogError("PlayerRuntimeStats requires a PlayerConfig.", this);
            }
        }

        private void OnValidate()
        {
            RefreshBaseValues();
        }

        public void Configure(PlayerConfig newConfig)
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

            maxHealth.Initialize(RuntimeStatId.PlayerMaxHealth, config.MaxHealth, 1f);
            moveSpeed.Initialize(RuntimeStatId.PlayerMoveSpeed, config.MoveSpeed);
            jumpHeight.Initialize(RuntimeStatId.PlayerJumpHeight, config.JumpHeight);
            gravity.Initialize(RuntimeStatId.PlayerGravity, config.Gravity, 0f, false);
            groundedVerticalVelocity.Initialize(
                RuntimeStatId.PlayerGroundedVerticalVelocity,
                config.GroundedVerticalVelocity,
                0f,
                false);
            lookSensitivity.Initialize(
                RuntimeStatId.PlayerLookSensitivity,
                config.LookSensitivity);
            maximumLookAngle.Initialize(
                RuntimeStatId.PlayerMaximumLookAngle,
                config.MaximumLookAngle,
                1f);
            enemyTopSlideSpeed.Initialize(
                RuntimeStatId.PlayerEnemyTopSlideSpeed,
                config.EnemyTopSlideSpeed);
        }

        public bool TryGetStat(RuntimeStatId statId, out RuntimeStat runtimeStat)
        {
            switch (statId)
            {
                case RuntimeStatId.PlayerMaxHealth:
                    runtimeStat = maxHealth;
                    return true;
                case RuntimeStatId.PlayerMoveSpeed:
                    runtimeStat = moveSpeed;
                    return true;
                case RuntimeStatId.PlayerJumpHeight:
                    runtimeStat = jumpHeight;
                    return true;
                case RuntimeStatId.PlayerGravity:
                    runtimeStat = gravity;
                    return true;
                case RuntimeStatId.PlayerGroundedVerticalVelocity:
                    runtimeStat = groundedVerticalVelocity;
                    return true;
                case RuntimeStatId.PlayerLookSensitivity:
                    runtimeStat = lookSensitivity;
                    return true;
                case RuntimeStatId.PlayerMaximumLookAngle:
                    runtimeStat = maximumLookAngle;
                    return true;
                case RuntimeStatId.PlayerEnemyTopSlideSpeed:
                    runtimeStat = enemyTopSlideSpeed;
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
            jumpHeight.ClearModifiers();
            gravity.ClearModifiers();
            groundedVerticalVelocity.ClearModifiers();
            lookSensitivity.ClearModifiers();
            maximumLookAngle.ClearModifiers();
            enemyTopSlideSpeed.ClearModifiers();
        }
    }
}
