using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.Config
{
    public enum EffectPolarity
    {
        Neutral,
        Reward,
        Penalty
    }

    [Serializable]
    public sealed class EffectDefinition
    {
        [SerializeField] private string effectId;
        [SerializeField] private string displayName;
        [SerializeField] private EffectPolarity polarity;
        [SerializeField, Min(0f)] private float balanceWeight = 10f;

        [Header("Creator Template")]
        [SerializeField] private bool creatorAvailable;
        [SerializeField] private string creatorKind;
        [SerializeField] private string creatorTarget;
        [SerializeField] private string creatorStatId;
        [SerializeField] private string creatorOperation;
        [SerializeField] private float creatorDefaultValue;
        [SerializeField] private string creatorStringValue;
        [SerializeField] private string creatorStackMode = "None";
        [SerializeField, Min(0)] private int creatorMaxStacks = 1;

        public EffectDefinition(
            string id,
            string name,
            EffectPolarity effectPolarity,
            float evaluationWeight = 10f)
        {
            effectId = id ?? string.Empty;
            displayName = name ?? string.Empty;
            polarity = effectPolarity;
            balanceWeight = Mathf.Max(0f, evaluationWeight);
        }

        public string EffectId => effectId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? EffectId
            : displayName;
        public EffectPolarity Polarity => polarity;
        public float BalanceWeight => Mathf.Max(0f, balanceWeight);
        public bool CreatorAvailable => creatorAvailable;
        public string CreatorKind => creatorKind ?? string.Empty;
        public string CreatorTarget => creatorTarget ?? string.Empty;
        public string CreatorStatId => creatorStatId ?? string.Empty;
        public string CreatorOperation => creatorOperation ?? string.Empty;
        public float CreatorDefaultValue => creatorDefaultValue;
        public string CreatorStringValue => creatorStringValue ?? string.Empty;
        public string CreatorStackMode => creatorStackMode ?? string.Empty;
        public int CreatorMaxStacks => Mathf.Max(0, creatorMaxStacks);

        public void ConfigureBalanceWeight(float value)
        {
            balanceWeight = Mathf.Max(0f, value);
        }

        public void ConfigureCreatorTemplate(
            bool available,
            string effectKind,
            string target,
            string statId,
            string operation,
            float defaultValue,
            string stringValue,
            string stackMode,
            int maxStacks)
        {
            creatorAvailable = available;
            creatorKind = effectKind ?? string.Empty;
            creatorTarget = target ?? string.Empty;
            creatorStatId = statId ?? string.Empty;
            creatorOperation = operation ?? string.Empty;
            creatorDefaultValue = defaultValue;
            creatorStringValue = stringValue ?? string.Empty;
            creatorStackMode = stackMode ?? string.Empty;
            creatorMaxStacks = Mathf.Max(0, maxStacks);
        }
    }

    [CreateAssetMenu(
        fileName = "EffectCatalog",
        menuName = "RuleForge/Config/Effect Catalog")]
    public sealed class EffectCatalog : ScriptableObject
    {
        [SerializeField] private List<EffectDefinition> effects =
            new List<EffectDefinition>();

        public IReadOnlyList<EffectDefinition> Effects => effects;

        public bool TryGetDefinition(
            string effectId,
            out EffectDefinition definition)
        {
            EnsureList();
            string normalizedId = effectId ?? string.Empty;
            for (int index = 0; index < effects.Count; index++)
            {
                EffectDefinition candidate = effects[index];
                if (candidate != null &&
                    string.Equals(
                        candidate.EffectId,
                        normalizedId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public void EnsureDefinition(
            string effectId,
            string displayName,
            EffectPolarity polarity)
        {
            EnsureList();
            if (TryGetDefinition(effectId, out _))
            {
                return;
            }

            effects.Add(new EffectDefinition(effectId, displayName, polarity));
        }

        public void ConfigureBalanceWeight(string effectId, float value)
        {
            if (TryGetDefinition(effectId, out EffectDefinition definition))
            {
                definition.ConfigureBalanceWeight(value);
            }
        }

        public void ConfigureCreatorTemplate(
            string effectId,
            bool available,
            string effectKind,
            string target,
            string statId,
            string operation,
            float defaultValue,
            string stringValue,
            string stackMode,
            int maxStacks)
        {
            if (TryGetDefinition(effectId, out EffectDefinition definition))
            {
                definition.ConfigureCreatorTemplate(
                    available,
                    effectKind,
                    target,
                    statId,
                    operation,
                    defaultValue,
                    stringValue,
                    stackMode,
                    maxStacks);
            }
        }

        private void EnsureList()
        {
            if (effects == null)
            {
                effects = new List<EffectDefinition>();
            }
        }
    }
}
