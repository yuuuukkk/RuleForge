using System.Collections.Generic;

namespace RuleForge.Runtime.Stats
{
    public static class StatCalculator
    {
        public static float Calculate(
            float baseValue,
            IReadOnlyList<StatModifier> modifiers)
        {
            float flatTotal = 0f;
            float additivePercentTotal = 0f;
            float multiplierProduct = 1f;

            if (modifiers != null)
            {
                for (int index = 0; index < modifiers.Count; index++)
                {
                    StatModifier modifier = modifiers[index];
                    if (modifier == null)
                    {
                        continue;
                    }

                    switch (modifier.Operation)
                    {
                        case StatModifierOperation.AddFlat:
                            flatTotal += modifier.Value;
                            break;
                        case StatModifierOperation.AddPercent:
                            additivePercentTotal += modifier.Value;
                            break;
                        case StatModifierOperation.Multiply:
                            multiplierProduct *= modifier.Value;
                            break;
                    }
                }
            }

            return (baseValue + flatTotal) *
                   (1f + additivePercentTotal) *
                   multiplierProduct;
        }
    }
}
