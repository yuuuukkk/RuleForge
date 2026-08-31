using System;
using UnityEngine;

namespace RuleForge.Runtime.Stats
{
    [Serializable]
    public sealed class StatModifier
    {
        [SerializeField] private string source;
        [SerializeField] private StatModifierOperation operation;
        [SerializeField] private float value;

        public StatModifier(string source, StatModifierOperation operation, float value)
        {
            this.source = source ?? string.Empty;
            this.operation = operation;
            this.value = value;
        }

        public string Source => source;

        public StatModifierOperation Operation => operation;

        public float Value => value;
    }
}
