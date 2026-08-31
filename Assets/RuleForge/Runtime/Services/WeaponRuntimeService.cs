using System.Collections.Generic;
using RuleForge.Runtime.Stats;
using RuleForge.Weapons;
using UnityEngine;

namespace RuleForge.Runtime.Services
{
    [DisallowMultipleComponent]
    public sealed class WeaponRuntimeService : MonoBehaviour
    {
        [SerializeField] private WeaponRuntimeStats runtimeStats;
        [SerializeField] private WeaponController weaponController;

        private readonly List<AppliedModifier> appliedModifiers =
            new List<AppliedModifier>();

        private void Awake()
        {
            ResolveReferences();
        }

        public void Configure(
            WeaponRuntimeStats stats,
            WeaponController controller = null)
        {
            runtimeStats = stats;
            weaponController = controller != null
                ? controller
                : stats != null
                    ? stats.GetComponent<WeaponController>()
                    : null;
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

        public bool GiveAmmo(float amount)
        {
            ResolveReferences();
            if (weaponController == null || amount <= 0f)
            {
                return false;
            }

            weaponController.AddReserveAmmo(Mathf.Max(0, Mathf.RoundToInt(amount)));
            return true;
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
            weaponController?.ResetAmmo();
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
                runtimeStats = FindObjectOfType<WeaponRuntimeStats>();
            }

            if (weaponController == null && runtimeStats != null)
            {
                weaponController = runtimeStats.GetComponent<WeaponController>();
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
