using UnityEngine;

namespace RuleForge.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyRuntimeIdentity : MonoBehaviour
    {
        [SerializeField] private string enemyType = "Grunt";

        public string EnemyType => string.IsNullOrWhiteSpace(enemyType)
            ? "Grunt"
            : enemyType;

        public void Configure(string newEnemyType)
        {
            enemyType = string.IsNullOrWhiteSpace(newEnemyType)
                ? "Grunt"
                : newEnemyType.Trim();
        }
    }
}
