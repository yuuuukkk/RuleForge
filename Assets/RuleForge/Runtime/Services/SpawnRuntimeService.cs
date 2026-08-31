using RuleForge.Enemies;
using UnityEngine;

namespace RuleForge.Runtime.Services
{
    [DisallowMultipleComponent]
    public sealed class SpawnRuntimeService : MonoBehaviour
    {
        [SerializeField] private EnemySpawner enemySpawner;

        public int AliveEnemyCount => enemySpawner != null
            ? enemySpawner.AliveEnemyCount
            : 0;

        private void Awake()
        {
            ResolveSpawner();
        }

        public void Configure(EnemySpawner spawner)
        {
            enemySpawner = spawner;
        }

        public EnemyHealth SpawnEnemy()
        {
            ResolveSpawner();
            return enemySpawner != null ? enemySpawner.SpawnEnemy() : null;
        }

        public EnemyHealth SpawnEnemy(string enemyType)
        {
            ResolveSpawner();
            if (enemySpawner == null)
            {
                return null;
            }

            EnemyHealth enemy = enemySpawner.SpawnEnemy(enemyType);
            if (enemy == null)
            {
                return null;
            }
            return enemy;
        }

        public void ResetForChallenge()
        {
            ResolveSpawner();
            enemySpawner?.ResetForChallenge();
        }

        private void ResolveSpawner()
        {
            if (enemySpawner == null)
            {
                enemySpawner = FindObjectOfType<EnemySpawner>();
            }
        }
    }
}
