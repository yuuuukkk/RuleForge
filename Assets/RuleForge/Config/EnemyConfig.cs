using UnityEngine;

namespace RuleForge.Config
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "RuleForge/Config/Enemy")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Grunt";
        [SerializeField] private Color visualColor = new Color(0.75f, 0.25f, 0.2f);
        [SerializeField, Min(0.1f)] private float visualScale = 1f;

        [Header("Health")]
        [SerializeField, Min(1f)] private float maxHealth = 50f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float stoppingDistance = 1.5f;
        [SerializeField, Min(0f)] private float turnSpeed = 10f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float contactDamage = 10f;
        [SerializeField, Min(0.01f)] private float attacksPerSecond = 1f;

        [Header("Spawning")]
        [SerializeField, Min(1)] private int desiredAliveEnemies = 1;
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? "Grunt"
            : displayName;
        public Color VisualColor => visualColor;
        public float VisualScale => visualScale;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float StoppingDistance => stoppingDistance;
        public float TurnSpeed => turnSpeed;
        public float ContactDamage => contactDamage;
        public float AttacksPerSecond => attacksPerSecond;
        public int DesiredAliveEnemies => desiredAliveEnemies;
        public float RespawnDelay => respawnDelay;

        public void ConfigureProfile(
            string name,
            float health,
            float speed,
            float stopDistance,
            float turningSpeed,
            float damage,
            float attackRate,
            int desiredAlive,
            float respawnSeconds,
            Color color,
            float scale)
        {
            displayName = name ?? string.Empty;
            maxHealth = Mathf.Max(1f, health);
            moveSpeed = Mathf.Max(0f, speed);
            stoppingDistance = Mathf.Max(0f, stopDistance);
            turnSpeed = Mathf.Max(0f, turningSpeed);
            contactDamage = Mathf.Max(0f, damage);
            attacksPerSecond = Mathf.Max(0.01f, attackRate);
            desiredAliveEnemies = Mathf.Max(1, desiredAlive);
            respawnDelay = Mathf.Max(0f, respawnSeconds);
            visualColor = color;
            visualScale = Mathf.Max(0.1f, scale);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            contactDamage = Mathf.Max(0f, contactDamage);
            attacksPerSecond = Mathf.Max(0.01f, attacksPerSecond);
            desiredAliveEnemies = Mathf.Max(1, desiredAliveEnemies);
            respawnDelay = Mathf.Max(0f, respawnDelay);
            visualScale = Mathf.Max(0.1f, visualScale);
        }
    }
}
