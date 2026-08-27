using System;
using RuleForge.Runtime;
using UnityEngine;

namespace RuleForge.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 50f;

        public event Action<EnemyHealth> Died;

        public event Action<float, float> HealthChanged;

        public float CurrentHealth { get; private set; }

        public float MaxHealth => maxHealth;

        public bool IsAlive => CurrentHealth > 0f;

        private void OnEnable()
        {
            ResetHealth();
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || damageInfo.Amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (IsAlive)
            {
                return;
            }

            Died?.Invoke(this);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
