using NUnit.Framework;
using RuleForge.Config;
using RuleForge.Enemies;
using RuleForge.Runtime;
using UnityEngine;

namespace RuleForge.Tests
{
    public sealed class DamageSystemTests
    {
        [Test]
        public void EnemyHealth_TakesDamageAndDiesAtZero()
        {
            GameObject enemyObject = new GameObject("Test Enemy");
            EnemyConfig enemyConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            EnemyRuntimeStats runtimeStats = enemyObject.AddComponent<EnemyRuntimeStats>();
            runtimeStats.Configure(enemyConfig);
            EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
            enemyHealth.ResetHealth();
            bool died = false;
            enemyHealth.Died += _ => died = true;

            enemyHealth.TakeDamage(new DamageInfo(enemyHealth.MaxHealth * 0.5f));
            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(enemyHealth.MaxHealth * 0.5f));
            Assert.That(enemyHealth.IsAlive, Is.True);

            enemyHealth.TakeDamage(new DamageInfo(enemyHealth.MaxHealth));
            Assert.That(enemyHealth.CurrentHealth, Is.Zero);
            Assert.That(enemyHealth.IsAlive, Is.False);
            Assert.That(died, Is.True);

            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(enemyConfig);
        }

        [Test]
        public void DamageInfo_ClampsNegativeDamageToZero()
        {
            DamageInfo damageInfo = new DamageInfo(-10f);

            Assert.That(damageInfo.Amount, Is.Zero);
        }
    }
}
