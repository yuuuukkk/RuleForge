using System.Collections.Generic;
using RuleForge.Player;
using RuleForge.Runtime.Stats;
using UnityEngine;

namespace RuleForge.Runtime.Services
{
    [DisallowMultipleComponent]
    public sealed class PlayerRuntimeService : MonoBehaviour
    {
        [SerializeField] private PlayerRuntimeStats runtimeStats;
        [SerializeField] private PlayerHealth playerHealth;

        private readonly List<AppliedModifier> appliedModifiers =
            new List<AppliedModifier>();

        public PlayerHealth PlayerHealth => playerHealth;

        public float MissingHealthPercent
        {
            get
            {
                ResolveReferences();
                if (playerHealth == null || playerHealth.MaxHealth <= 0f)
                {
                    return 0f;
                }

                return 1f - Mathf.Clamp01(
                    playerHealth.CurrentHealth / playerHealth.MaxHealth);
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void Configure(PlayerRuntimeStats stats, PlayerHealth health)
        {
            runtimeStats = stats;
            playerHealth = health;
        }

        public bool ApplyModifier(
            RuntimeStatId statId,
            string source,
            StatModifierOperation operation,
            float value)
        {
            ResolveReferences();
            if (runtimeStats == null ||
                !runtimeStats.TryGetStat(statId, out RuntimeStat stat))
            {
                return false;
            }

            StatModifier modifier = stat.AddModifier(source, operation, value);
            appliedModifiers.Add(new AppliedModifier(source, stat, modifier));
            return true;
        }

        public bool SetModifier(
            RuntimeStatId statId,
            string source,
            StatModifierOperation operation,
            float value)
        {
            RemoveModifiersFromSource(source);
            return ApplyModifier(statId, source, operation, value);
        }

        public void ClearRuleModifiers()
        {
            for (int index = appliedModifiers.Count - 1; index >= 0; index--)
            {
                AppliedModifier applied = appliedModifiers[index];
                applied.Stat?.RemoveModifier(applied.Modifier);
            }

            appliedModifiers.Clear();
        }

        public void ResetForChallenge()
        {
            ClearRuleModifiers();
            ResolveReferences();
            playerHealth?.ResetHealth();
        }

        public bool TryGetStat(RuntimeStatId statId, out RuntimeStat stat)
        {
            ResolveReferences();
            if (runtimeStats != null)
            {
                return runtimeStats.TryGetStat(statId, out stat);
            }

            stat = null;
            return false;
        }

        private void ResolveReferences()
        {
            if (runtimeStats == null)
            {
                runtimeStats = FindObjectOfType<PlayerRuntimeStats>();
            }

            if (playerHealth == null && runtimeStats != null)
            {
                playerHealth = runtimeStats.GetComponent<PlayerHealth>();
            }
        }

        public void RemoveModifiersFromSource(string source)
        {
            string normalizedSource = source ?? string.Empty;
            for (int index = appliedModifiers.Count - 1; index >= 0; index--)
            {
                AppliedModifier applied = appliedModifiers[index];
                if (applied.Source != normalizedSource)
                {
                    continue;
                }

                applied.Stat?.RemoveModifier(applied.Modifier);
                appliedModifiers.RemoveAt(index);
            }
        }

        private readonly struct AppliedModifier
        {
            public AppliedModifier(
                string source,
                RuntimeStat stat,
                StatModifier modifier)
            {
                Source = source ?? string.Empty;
                Stat = stat;
                Modifier = modifier;
            }

            public string Source { get; }
            public RuntimeStat Stat { get; }
            public StatModifier Modifier { get; }
        }
    }
}
