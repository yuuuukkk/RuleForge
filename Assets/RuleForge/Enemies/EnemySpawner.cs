using System;
using System.Collections;
using System.Collections.Generic;
using RuleForge.Config;
using UnityEngine;

namespace RuleForge.Enemies
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private EnemyConfig[] enemyTypes =
            Array.Empty<EnemyConfig>();

        private readonly HashSet<EnemyHealth> aliveEnemies = new HashSet<EnemyHealth>();
        private int nextSpawnPointIndex;
        private int nextEnemyTypeIndex;

        public event Action<EnemyHealth> EnemySpawned;

        public int AliveEnemyCount => aliveEnemies.Count;

        private void Start()
        {
            FillMissingEnemies();
        }

        private void OnDestroy()
        {
            foreach (EnemyHealth enemy in aliveEnemies)
            {
                if (enemy != null)
                {
                    enemy.Died -= HandleEnemyDied;
                }
            }
        }

        public void Configure(EnemyHealth prefab, Transform[] points)
        {
            enemyPrefab = prefab;
            spawnPoints = points;
        }

        public void ConfigureRoster(
            EnemyHealth prefab,
            Transform[] points,
            EnemyConfig[] configs)
        {
            enemyPrefab = prefab;
            spawnPoints = points;
            enemyTypes = configs ?? Array.Empty<EnemyConfig>();
        }

        public EnemyHealth SpawnEnemy()
        {
            EnemyConfig config = GetNextEnemyConfig();
            return SpawnConfiguredEnemy(config);
        }

        public EnemyHealth SpawnEnemy(string enemyType)
        {
            EnemyConfig config = FindEnemyConfig(enemyType);
            if (config == null && enemyTypes != null && enemyTypes.Length > 0)
            {
                Debug.LogWarning(
                    $"Enemy type '{enemyType}' is not configured.",
                    this);
                return null;
            }

            return SpawnConfiguredEnemy(config);
        }

        private EnemyHealth SpawnConfiguredEnemy(EnemyConfig config)
        {
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("EnemySpawner requires an enemy prefab and at least one spawn point.", this);
                return null;
            }

            Transform spawnPoint = spawnPoints[nextSpawnPointIndex % spawnPoints.Length];
            nextSpawnPointIndex++;

            EnemyHealth enemy = Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation);
            enemy.name = enemyPrefab.name;
            EnemyRuntimeStats stats = enemy.GetComponent<EnemyRuntimeStats>();
            if (config != null && stats != null)
            {
                stats.Configure(config);
            }

            EnemyRuntimeIdentity identity =
                enemy.GetComponent<EnemyRuntimeIdentity>();
            if (identity == null)
            {
                identity = enemy.gameObject.AddComponent<EnemyRuntimeIdentity>();
            }

            string typeName = config != null ? config.DisplayName : "Grunt";
            identity.Configure(typeName);
            enemy.name = $"Enemy [{identity.EnemyType}]";
            ApplyVisual(enemy.transform, config);
            enemy.ResetHealth();
            enemy.Died += HandleEnemyDied;
            aliveEnemies.Add(enemy);
            EnemySpawned?.Invoke(enemy);
            return enemy;
        }

        public void ResetForChallenge()
        {
            StopAllCoroutines();
            EnemyHealth[] enemies = new EnemyHealth[aliveEnemies.Count];
            aliveEnemies.CopyTo(enemies);
            aliveEnemies.Clear();

            for (int index = 0; index < enemies.Length; index++)
            {
                EnemyHealth enemy = enemies[index];
                if (enemy == null)
                {
                    continue;
                }

                enemy.Died -= HandleEnemyDied;
                enemy.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(enemy.gameObject);
                }
                else
                {
                    DestroyImmediate(enemy.gameObject);
                }
            }

            nextSpawnPointIndex = 0;
            nextEnemyTypeIndex = 0;
            FillMissingEnemies();
        }

        private void FillMissingEnemies()
        {
            int desiredAliveEnemies = GetDesiredAliveEnemyCount();
            if (desiredAliveEnemies <= 0)
            {
                Debug.LogWarning("EnemySpawner requires an Enemy prefab with configured runtime stats.", this);
                return;
            }

            while (aliveEnemies.Count < desiredAliveEnemies)
            {
                if (SpawnEnemy() == null)
                {
                    break;
                }
            }
        }

        private void HandleEnemyDied(EnemyHealth enemy)
        {
            EnemyRuntimeStats runtimeStats = enemy.GetComponent<EnemyRuntimeStats>();
            float respawnDelay = runtimeStats != null
                ? runtimeStats.RespawnDelayStat.FinalValue
                : 0f;
            enemy.Died -= HandleEnemyDied;
            aliveEnemies.Remove(enemy);
            StartCoroutine(RespawnAfterDelay(respawnDelay));
        }

        private IEnumerator RespawnAfterDelay(float respawnDelay)
        {
            if (respawnDelay > 0f)
            {
                yield return new WaitForSeconds(respawnDelay);
            }

            FillMissingEnemies();
        }

        private int GetDesiredAliveEnemyCount()
        {
            EnemyConfig baseline = FindEnemyConfig("Grunt");
            if (baseline == null && enemyTypes != null)
            {
                for (int index = 0; index < enemyTypes.Length; index++)
                {
                    if (enemyTypes[index] != null)
                    {
                        baseline = enemyTypes[index];
                        break;
                    }
                }
            }

            if (baseline != null)
            {
                return Mathf.Max(1, baseline.DesiredAliveEnemies);
            }

            EnemyRuntimeStats prefabStats = enemyPrefab != null
                ? enemyPrefab.GetComponent<EnemyRuntimeStats>()
                : null;
            return prefabStats != null && prefabStats.IsConfigured
                ? Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        prefabStats.DesiredAliveEnemiesStat.FinalValue))
                : 0;
        }

        private EnemyConfig GetNextEnemyConfig()
        {
            if (enemyTypes == null || enemyTypes.Length == 0)
            {
                return null;
            }

            for (int offset = 0; offset < enemyTypes.Length; offset++)
            {
                int index = (nextEnemyTypeIndex + offset) % enemyTypes.Length;
                EnemyConfig config = enemyTypes[index];
                if (config != null)
                {
                    nextEnemyTypeIndex = (index + 1) % enemyTypes.Length;
                    return config;
                }
            }

            return null;
        }

        private EnemyConfig FindEnemyConfig(string enemyType)
        {
            if (enemyTypes == null)
            {
                return null;
            }

            for (int index = 0; index < enemyTypes.Length; index++)
            {
                EnemyConfig config = enemyTypes[index];
                if (config != null &&
                    string.Equals(
                        config.DisplayName,
                        enemyType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return config;
                }
            }

            return null;
        }

        private static void ApplyVisual(
            Transform enemy,
            EnemyConfig config)
        {
            if (enemy == null || config == null)
            {
                return;
            }

            enemy.localScale = Vector3.one * config.VisualScale;
            EnemyVisualController visualController =
                enemy.GetComponent<EnemyVisualController>();
            if (visualController != null)
            {
                visualController.Apply(config.DisplayName, config.VisualColor);
            }

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", config.VisualColor);
                properties.SetColor("_BaseColor", config.VisualColor);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
