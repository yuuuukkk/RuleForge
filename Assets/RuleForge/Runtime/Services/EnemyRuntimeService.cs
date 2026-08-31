using System.Collections.Generic;
using RuleForge.Enemies;
using RuleForge.Runtime.Stats;
using UnityEngine;

namespace RuleForge.Runtime.Services
{
    [DisallowMultipleComponent]
    public sealed class EnemyRuntimeService : MonoBehaviour
    {
        [SerializeField] private EnemySpawner enemySpawner;

        private readonly HashSet<EnemyRuntimeStats> activeEnemies =
            new HashSet<EnemyRuntimeStats>();
        private readonly List<GlobalModifier> globalModifiers =
            new List<GlobalModifier>();

        private void OnEnable()
        {
            ResolveSpawner();
            SubscribeToSpawner();
            RemoveMissingEnemies();

            EnemyRuntimeStats[] existingEnemies = FindObjectsOfType<EnemyRuntimeStats>();
            for (int index = 0; index < existingEnemies.Length; index++)
            {
                RegisterEnemy(existingEnemies[index]);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromSpawner();
            foreach (EnemyRuntimeStats enemy in activeEnemies)
            {
                UnsubscribeFromEnemy(enemy);
            }
        }

        public void Configure(EnemySpawner spawner)
        {
            UnsubscribeFromSpawner();
            enemySpawner = spawner;
            if (isActiveAndEnabled)
            {
                SubscribeToSpawner();
            }
        }

        public bool ApplyGlobalModifier(
            RuntimeStatId statId,
            string source,
            StatModifierOperation operation,
            float value)
        {
            if (!IsEnemyStat(statId))
            {
                return false;
            }

            GlobalModifier modifier = new GlobalModifier(
                statId,
                source,
                operation,
                value);
            globalModifiers.Add(modifier);

            RemoveMissingEnemies();
            foreach (EnemyRuntimeStats enemy in activeEnemies)
            {
                ApplyToEnemy(enemy, modifier);
            }

            return true;
        }

        public bool SetGlobalModifier(
            RuntimeStatId statId,
            string source,
            StatModifierOperation operation,
            float value)
        {
            RemoveGlobalModifiersFromSource(source);
            return ApplyGlobalModifier(statId, source, operation, value);
        }

        public void ClearRuleModifiers()
        {
            RemoveMissingEnemies();
            foreach (EnemyRuntimeStats enemy in activeEnemies)
            {
                for (int index = 0; index < globalModifiers.Count; index++)
                {
                    GlobalModifier modifier = globalModifiers[index];
                    if (enemy.TryGetStat(modifier.StatId, out RuntimeStat stat))
                    {
                        stat.RemoveModifiersFromSource(modifier.Source);
                    }
                }
            }

            globalModifiers.Clear();
        }

        public bool TryGetRepresentativeStat(
            RuntimeStatId statId,
            out RuntimeStat stat)
        {
            RemoveMissingEnemies();
            foreach (EnemyRuntimeStats enemy in activeEnemies)
            {
                if (enemy != null && enemy.TryGetStat(statId, out stat))
                {
                    return true;
                }
            }

            stat = null;
            return false;
        }

        private void HandleEnemySpawned(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return;
            }

            EnemyRuntimeStats stats = enemyHealth.GetComponent<EnemyRuntimeStats>();
            RegisterEnemy(stats);
            enemyHealth.ResetHealth();
        }

        private void RegisterEnemy(EnemyRuntimeStats enemy)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.Died -= HandleEnemyDied;
                health.Died += HandleEnemyDied;
            }

            if (!activeEnemies.Add(enemy))
            {
                return;
            }

            for (int index = 0; index < globalModifiers.Count; index++)
            {
                ApplyToEnemy(enemy, globalModifiers[index]);
            }
        }

        private void HandleEnemyDied(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Died -= HandleEnemyDied;
            EnemyRuntimeStats stats = enemyHealth.GetComponent<EnemyRuntimeStats>();
            if (stats != null)
            {
                activeEnemies.Remove(stats);
            }
        }

        private static void ApplyToEnemy(
            EnemyRuntimeStats enemy,
            GlobalModifier modifier)
        {
            if (enemy != null &&
                enemy.TryGetStat(modifier.StatId, out RuntimeStat stat))
            {
                stat.AddModifier(
                    modifier.Source,
                    modifier.Operation,
                    modifier.Value);
            }
        }

        private void ResolveSpawner()
        {
            if (enemySpawner == null)
            {
                enemySpawner = FindObjectOfType<EnemySpawner>();
            }
        }

        private void SubscribeToSpawner()
        {
            if (enemySpawner != null)
            {
                enemySpawner.EnemySpawned -= HandleEnemySpawned;
                enemySpawner.EnemySpawned += HandleEnemySpawned;
            }
        }

        private void UnsubscribeFromSpawner()
        {
            if (enemySpawner != null)
            {
                enemySpawner.EnemySpawned -= HandleEnemySpawned;
            }
        }

        private void UnsubscribeFromEnemy(EnemyRuntimeStats enemy)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.Died -= HandleEnemyDied;
            }
        }

        private void RemoveMissingEnemies()
        {
            activeEnemies.RemoveWhere(enemy => enemy == null);
        }

        public void RemoveGlobalModifiersFromSource(string source)
        {
            string normalizedSource = source ?? string.Empty;
            RemoveMissingEnemies();
            foreach (EnemyRuntimeStats enemy in activeEnemies)
            {
                for (int index = 0; index < globalModifiers.Count; index++)
                {
                    GlobalModifier modifier = globalModifiers[index];
                    if (modifier.Source == normalizedSource &&
                        enemy.TryGetStat(modifier.StatId, out RuntimeStat stat))
                    {
                        stat.RemoveModifiersFromSource(normalizedSource);
                    }
                }
            }

            globalModifiers.RemoveAll(
                modifier => modifier.Source == normalizedSource);
        }

        private static bool IsEnemyStat(RuntimeStatId statId)
        {
            switch (statId)
            {
                case RuntimeStatId.EnemyMaxHealth:
                case RuntimeStatId.EnemyMoveSpeed:
                case RuntimeStatId.EnemyStoppingDistance:
                case RuntimeStatId.EnemyTurnSpeed:
                case RuntimeStatId.EnemyContactDamage:
                case RuntimeStatId.EnemyAttacksPerSecond:
                case RuntimeStatId.EnemyDesiredAliveCount:
                case RuntimeStatId.EnemyRespawnDelay:
                    return true;
                default:
                    return false;
            }
        }

        private readonly struct GlobalModifier
        {
            public GlobalModifier(
                RuntimeStatId statId,
                string source,
                StatModifierOperation operation,
                float value)
            {
                StatId = statId;
                Source = source ?? string.Empty;
                Operation = operation;
                Value = value;
            }

            public RuntimeStatId StatId { get; }
            public string Source { get; }
            public StatModifierOperation Operation { get; }
            public float Value { get; }
        }
    }
}
