using System;
using RuleForge.Runtime;
using RuleForge.Rules;
using RuleForge.Runtime.Services;
using UnityEngine;

namespace RuleForge.Player
{
    [RequireComponent(typeof(PlayerRuntimeStats))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        private PlayerRuntimeStats runtimeStats;

        [Header("Damage Protection")]
        [SerializeField, Min(0f)] private float spawnProtectionDuration = 3f;
        [SerializeField, Min(0f)] private float hitInvulnerabilityDuration = 0.5f;

        [Header("Runtime Health Debug")]
        [SerializeField] private float currentHealth;

        private float nextDamageAllowedAt;
        private TimeRuntimeService timeHealthService;
        private float timeDamageScale;

        public event Action<float, float> HealthChanged;

        public event Action<DamageInfo> Damaged;

        public event Action Died;

        public float CurrentHealth => currentHealth;

        public float MaxHealth => RuntimeStats != null
            ? RuntimeStats.MaxHealthStat.FinalValue
            : 0f;

        public bool IsAlive => UsesTimeAsHealth
            ? timeHealthService.RemainingSeconds > 0f
            : CurrentHealth > 0f;

        public float DamageProtectionRemaining => Mathf.Max(
            0f,
            nextDamageAllowedAt - Time.time);
        public bool UsesTimeAsHealth => timeHealthService != null &&
                                        timeDamageScale > 0f;
        public float TimeDamageScale => timeDamageScale;

        public void ConfigureTimeHealth(
            TimeRuntimeService service,
            float secondsPerDamagePoint)
        {
            timeHealthService = service;
            timeDamageScale = service != null
                ? Mathf.Max(0f, secondsPerDamagePoint)
                : 0f;
        }

        private void Awake()
        {
            runtimeStats = GetComponent<PlayerRuntimeStats>();
            ResetHealth();
        }

        private void OnEnable()
        {
            runtimeStats = runtimeStats != null
                ? runtimeStats
                : GetComponent<PlayerRuntimeStats>();
            runtimeStats.MaxHealthStat.ValueChanged += HandleMaxHealthChanged;
        }

        private void OnDisable()
        {
            if (runtimeStats != null)
            {
                runtimeStats.MaxHealthStat.ValueChanged -= HandleMaxHealthChanged;
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive ||
                damageInfo.Amount <= 0f ||
                Time.time < nextDamageAllowedAt)
            {
                return;
            }

            nextDamageAllowedAt = Time.time + hitInvulnerabilityDuration;
            if (UsesTimeAsHealth)
            {
                float secondsLost = timeHealthService.RemoveCountdownSeconds(
                    damageInfo.Amount * timeDamageScale);
                if (secondsLost <= 0f)
                {
                    return;
                }

                Damaged?.Invoke(damageInfo);
                GameplayEventBus.Publish(new GameplayEvent(
                    GameplayEventType.PlayerHit,
                    gameObject,
                    damageInfo.Source,
                    value: damageInfo.Amount));
                PublishHealthChanged(damageInfo.Source);
                return;
            }

            float healthBefore = CurrentHealth;
            currentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            float appliedDamage = healthBefore - CurrentHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            Damaged?.Invoke(damageInfo);
            GameplayEventBus.Publish(new GameplayEvent(
                GameplayEventType.PlayerHit,
                gameObject,
                damageInfo.Source,
                value: appliedDamage));
            PublishHealthChanged(damageInfo.Source);

            if (!IsAlive)
            {
                Died?.Invoke();
                Debug.Log("Player defeated.", this);
            }
        }

        public void ResetHealth()
        {
            currentHealth = MaxHealth;
            nextDamageAllowedAt = Time.time + spawnProtectionDuration;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void HandleMaxHealthChanged(float previousMaximum, float newMaximum)
        {
            currentHealth = Mathf.Min(CurrentHealth, newMaximum);
            HealthChanged?.Invoke(CurrentHealth, newMaximum);
            PublishHealthChanged();
        }

        private float HealthPercent => UsesTimeAsHealth
            ? timeHealthService.InitialCountdownSeconds > 0f
                ? Mathf.Clamp01(timeHealthService.RemainingSeconds /
                                timeHealthService.InitialCountdownSeconds)
                : 0f
            : MaxHealth > 0f
                ? Mathf.Clamp01(CurrentHealth / MaxHealth)
                : 0f;

        private void PublishHealthChanged(GameObject instigator = null)
        {
            GameplayEventBus.Publish(new GameplayEvent(
                GameplayEventType.PlayerHPChanged,
                gameObject,
                instigator,
                value: HealthPercent));
        }

        private PlayerRuntimeStats RuntimeStats
        {
            get
            {
                if (runtimeStats == null)
                {
                    runtimeStats = GetComponent<PlayerRuntimeStats>();
                }

                return runtimeStats;
            }
        }
    }
}
