using UnityEngine;

namespace RuleForge.Config
{
    public enum EnemyCombatStyle
    {
        Pursuer,
        Dasher,
        Bruiser
    }

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

        [Header("Behavior")]
        [SerializeField] private EnemyCombatStyle combatStyle =
            EnemyCombatStyle.Pursuer;
        [SerializeField, Min(0.1f)] private float attackWindupMultiplier = 1f;
        [SerializeField, Min(1f)] private float dashSpeedMultiplier = 2.2f;
        [SerializeField, Min(0.05f)] private float dashWindup = 0.35f;
        [SerializeField, Min(0.05f)] private float dashDuration = 0.32f;
        [SerializeField, Min(0.1f)] private float dashCooldown = 2.8f;

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
        public EnemyCombatStyle CombatStyle => combatStyle;
        public float AttackWindupMultiplier => attackWindupMultiplier;
        public float DashSpeedMultiplier => dashSpeedMultiplier;
        public float DashWindup => dashWindup;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;
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

        public void ConfigureBehavior(
            EnemyCombatStyle style,
            float windupMultiplier,
            float speedMultiplier = 2.2f,
            float windupSeconds = 0.35f,
            float durationSeconds = 0.32f,
            float cooldownSeconds = 2.8f)
        {
            combatStyle = style;
            attackWindupMultiplier = Mathf.Max(0.1f, windupMultiplier);
            dashSpeedMultiplier = Mathf.Max(1f, speedMultiplier);
            dashWindup = Mathf.Max(0.05f, windupSeconds);
            dashDuration = Mathf.Max(0.05f, durationSeconds);
            dashCooldown = Mathf.Max(0.1f, cooldownSeconds);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            contactDamage = Mathf.Max(0f, contactDamage);
            attacksPerSecond = Mathf.Max(0.01f, attacksPerSecond);
            attackWindupMultiplier = Mathf.Max(0.1f, attackWindupMultiplier);
            dashSpeedMultiplier = Mathf.Max(1f, dashSpeedMultiplier);
            dashWindup = Mathf.Max(0.05f, dashWindup);
            dashDuration = Mathf.Max(0.05f, dashDuration);
            dashCooldown = Mathf.Max(0.1f, dashCooldown);
            desiredAliveEnemies = Mathf.Max(1, desiredAliveEnemies);
            respawnDelay = Mathf.Max(0f, respawnDelay);
            visualScale = Mathf.Max(0.1f, visualScale);
        }
    }
}
