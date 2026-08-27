using System;
using RuleForge.Runtime;
using UnityEngine;

namespace RuleForge.Player
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public event Action<float, float> HealthChanged;

        public event Action Died;

        public float CurrentHealth { get; private set; }

        public float MaxHealth => maxHealth;

        public bool IsAlive => CurrentHealth > 0f;

        private void Awake()
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

            if (!IsAlive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Died?.Invoke();
                Debug.Log("Player defeated.", this);
            }
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
