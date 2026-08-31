using UnityEngine;

namespace RuleForge.Config
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "RuleForge/Config/Player")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedVerticalVelocity = -2f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float lookSensitivity = 2f;
        [SerializeField, Range(1f, 89f)] private float maximumLookAngle = 85f;

        [Header("Collision")]
        [SerializeField, Min(0f)] private float enemyTopSlideSpeed = 6f;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float GroundedVerticalVelocity => groundedVerticalVelocity;
        public float LookSensitivity => lookSensitivity;
        public float MaximumLookAngle => maximumLookAngle;
        public float EnemyTopSlideSpeed => enemyTopSlideSpeed;

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            gravity = Mathf.Min(-0.01f, gravity);
            groundedVerticalVelocity = Mathf.Min(0f, groundedVerticalVelocity);
            lookSensitivity = Mathf.Max(0f, lookSensitivity);
            maximumLookAngle = Mathf.Clamp(maximumLookAngle, 1f, 89f);
            enemyTopSlideSpeed = Mathf.Max(0f, enemyTopSlideSpeed);
        }
    }
}
