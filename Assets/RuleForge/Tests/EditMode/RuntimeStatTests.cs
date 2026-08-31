using NUnit.Framework;
using RuleForge.Runtime.Stats;

namespace RuleForge.Tests
{
    public sealed class RuntimeStatTests
    {
        [Test]
        public void StatCalculator_AppliesOperationsInDefinedOrder()
        {
            StatModifier[] modifiers =
            {
                new StatModifier("Flat", StatModifierOperation.AddFlat, 10f),
                new StatModifier("Percent", StatModifierOperation.AddPercent, 0.5f),
                new StatModifier("Multiplier", StatModifierOperation.Multiply, 2f)
            };

            float finalValue = StatCalculator.Calculate(100f, modifiers);

            Assert.That(finalValue, Is.EqualTo(330f));
        }

        [Test]
        public void RuntimeStat_RemovesOnlyModifiersFromRequestedSource()
        {
            RuntimeStat runtimeStat = new RuntimeStat();
            runtimeStat.Initialize(RuntimeStatId.WeaponDamage, 20f);
            runtimeStat.AddModifier("Challenge", StatModifierOperation.AddPercent, 0.5f);
            runtimeStat.AddModifier("Difficulty", StatModifierOperation.Multiply, 2f);

            int removedCount = runtimeStat.RemoveModifiersFromSource("Challenge");

            Assert.That(removedCount, Is.EqualTo(1));
            Assert.That(runtimeStat.FinalValue, Is.EqualTo(40f));
            Assert.That(runtimeStat.Modifiers.Count, Is.EqualTo(1));
            Assert.That(runtimeStat.Modifiers[0].Source, Is.EqualTo("Difficulty"));
        }

        [Test]
        public void RuntimeStat_ClearModifiers_RestoresBaseValue()
        {
            RuntimeStat runtimeStat = new RuntimeStat();
            runtimeStat.Initialize(RuntimeStatId.EnemyMoveSpeed, 3f);
            runtimeStat.AddModifier("Challenge", StatModifierOperation.AddPercent, 1f);

            runtimeStat.ClearModifiers();

            Assert.That(runtimeStat.FinalValue, Is.EqualTo(3f));
            Assert.That(runtimeStat.Modifiers, Is.Empty);
        }
    }
}
