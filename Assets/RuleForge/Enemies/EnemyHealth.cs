using System;
using RuleForge.Runtime;
using RuleForge.Rules;
using UnityEngine;

namespace RuleForge.Enemies
{
    [RequireComponent(typeof(EnemyRuntimeStats))]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        private EnemyRuntimeStats runtimeStats;

        public event Action<EnemyHealth> Died;

        public event Action<float, float> HealthChanged;

        public float CurrentHealth { get; private set; }

        public float MaxHealth => RuntimeStats != null
            ? RuntimeStats.MaxHealthStat.FinalValue
            : 0f;

        public bool IsAlive => CurrentHealth > 0f;

        private void OnEnable()
        {
            runtimeStats = GetComponent<EnemyRuntimeStats>();
            runtimeStats.MaxHealthStat.ValueChanged += HandleMaxHealthChanged;
            ResetHealth();
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
            if (!IsAlive || damageInfo.Amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (IsAlive)
            {
                return;
            }

            EnemyRuntimeIdentity identity = GetComponent<EnemyRuntimeIdentity>();
            GameplayEventBus.Publish(new GameplayEvent(
                GameplayEventType.EnemyKilled,
                gameObject,
                damageInfo.Source,
                identity != null ? identity.EnemyType : "Grunt"));
            Died?.Invoke(this);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void HandleMaxHealthChanged(float previousMaximum, float newMaximum)
        {
            float healthRatio = previousMaximum > 0f
                ? Mathf.Clamp01(CurrentHealth / previousMaximum)
                : 1f;
            CurrentHealth = Mathf.Clamp(
                newMaximum * healthRatio,
                0f,
                newMaximum);
            HealthChanged?.Invoke(CurrentHealth, newMaximum);
        }

        private EnemyRuntimeStats RuntimeStats
        {
            get
            {
                if (runtimeStats == null)
                {
                    runtimeStats = GetComponent<EnemyRuntimeStats>();
                }

                return runtimeStats;
            }
        }
    }
}
