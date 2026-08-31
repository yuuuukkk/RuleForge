using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.Runtime.Stats
{
    [Serializable]
    public sealed class RuntimeStat
    {
        [SerializeField] private RuntimeStatId id;
        [SerializeField] private float baseValue;
        [SerializeField] private bool enforceMinimum = true;
        [SerializeField] private float minimumValue;
        [SerializeField] private float finalValue;
        [SerializeField] private List<StatModifier> modifiers = new List<StatModifier>();

        public event Action<float, float> ValueChanged;

        public RuntimeStatId Id => id;

        public float BaseValue => baseValue;

        public float FinalValue => finalValue;

        public IReadOnlyList<StatModifier> Modifiers => modifiers;

        public void Initialize(
            RuntimeStatId statId,
            float newBaseValue,
            float newMinimumValue = 0f,
            bool shouldEnforceMinimum = true)
        {
            id = statId;
            baseValue = newBaseValue;
            minimumValue = newMinimumValue;
            enforceMinimum = shouldEnforceMinimum;
            EnsureModifierList();
            Recalculate();
        }

        public void SetBaseValue(float newBaseValue)
        {
            baseValue = newBaseValue;
            Recalculate();
        }

        public StatModifier AddModifier(
            string source,
            StatModifierOperation operation,
            float value)
        {
            EnsureModifierList();
            StatModifier modifier = new StatModifier(source, operation, value);
            modifiers.Add(modifier);
            Recalculate();
            return modifier;
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            EnsureModifierList();
            bool removed = modifiers.Remove(modifier);
            if (removed)
            {
                Recalculate();
            }

            return removed;
        }

        public int RemoveModifiersFromSource(string source)
        {
            EnsureModifierList();
            string normalizedSource = source ?? string.Empty;
            int removedCount = modifiers.RemoveAll(
                modifier => modifier != null && modifier.Source == normalizedSource);
            if (removedCount > 0)
            {
                Recalculate();
            }

            return removedCount;
        }

        public void ClearModifiers()
        {
            EnsureModifierList();
            if (modifiers.Count == 0)
            {
                return;
            }

            modifiers.Clear();
            Recalculate();
        }

        private void Recalculate()
        {
            float previousValue = finalValue;
            float calculatedValue = StatCalculator.Calculate(baseValue, modifiers);
            finalValue = enforceMinimum
                ? Mathf.Max(minimumValue, calculatedValue)
                : calculatedValue;

            if (!Mathf.Approximately(previousValue, finalValue))
            {
                ValueChanged?.Invoke(previousValue, finalValue);
            }
        }

        private void EnsureModifierList()
        {
            if (modifiers == null)
            {
                modifiers = new List<StatModifier>();
            }
        }
    }
}
