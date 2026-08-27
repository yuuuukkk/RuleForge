using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.Enemies
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyHealth enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Min(1)] private int desiredAliveEnemies = 1;
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        private readonly HashSet<EnemyHealth> aliveEnemies = new HashSet<EnemyHealth>();
        private int nextSpawnPointIndex;

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

        public EnemyHealth SpawnEnemy()
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
            enemy.Died += HandleEnemyDied;
            aliveEnemies.Add(enemy);
            return enemy;
        }

        private void FillMissingEnemies()
        {
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
            enemy.Died -= HandleEnemyDied;
            aliveEnemies.Remove(enemy);
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            if (respawnDelay > 0f)
            {
                yield return new WaitForSeconds(respawnDelay);
            }

            FillMissingEnemies();
        }
    }
}
