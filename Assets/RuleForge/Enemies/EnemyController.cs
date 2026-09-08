using RuleForge.Player;
using RuleForge.Config;
using RuleForge.Runtime;
using UnityEngine;

namespace RuleForge.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyRuntimeStats))]
    public sealed class EnemyController : MonoBehaviour
    {
        private CharacterController characterController;
        private EnemyRuntimeStats runtimeStats;
        private EnemyAttackVisual attackVisual;
        private Transform target;
        private PlayerHealth targetHealth;
        private float nextAttackTime;
        private bool attackPending;
        private float attackHitsAt;
        private bool dashWindupPending;
        private bool dashActive;
        private float dashStateEndsAt;
        private float nextDashTime;
        private Vector3 dashDirection;

        [Header("Attack Readability")]
        [SerializeField, Min(0.05f)] private float attackWindupDuration = 0.45f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            runtimeStats = GetComponent<EnemyRuntimeStats>();
            attackVisual = GetComponent<EnemyAttackVisual>();
            if (attackVisual == null)
            {
                attackVisual = gameObject.AddComponent<EnemyAttackVisual>();
            }
        }

        private void Update()
        {
            if (!TryAcquireTarget())
            {
                CancelPendingAttack();
                CancelDash();
                characterController.SimpleMove(Vector3.zero);
                return;
            }

            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            Vector3 direction = distance > 0f ? offset / distance : Vector3.zero;

            if (direction.sqrMagnitude > 0f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                float turnSpeed = runtimeStats.TurnSpeedStat.FinalValue;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    turnSpeed * Time.deltaTime);
            }

            if (HandleDash(distance, direction))
            {
                return;
            }

            if (distance > runtimeStats.StoppingDistanceStat.FinalValue)
            {
                CancelPendingAttack();
                characterController.SimpleMove(
                    direction * runtimeStats.MoveSpeedStat.FinalValue);
                return;
            }

            characterController.SimpleMove(Vector3.zero);
            TryAttack(direction);
        }

        public void SetTarget(PlayerHealth playerHealth)
        {
            targetHealth = playerHealth;
            target = playerHealth != null ? playerHealth.transform : null;
        }

        private bool TryAcquireTarget()
        {
            if (runtimeStats == null || !runtimeStats.IsConfigured)
            {
                return false;
            }

            if (targetHealth != null && targetHealth.IsAlive)
            {
                return true;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                target = null;
                targetHealth = null;
                return false;
            }

            SetTarget(playerObject.GetComponent<PlayerHealth>());
            return targetHealth != null && targetHealth.IsAlive;
        }

        private void TryAttack(Vector3 direction)
        {
            float contactDamage = runtimeStats.ContactDamageStat.FinalValue;
            float attacksPerSecond = runtimeStats.AttacksPerSecondStat.FinalValue;
            if (targetHealth == null ||
                !targetHealth.IsAlive ||
                contactDamage <= 0f ||
                attacksPerSecond <= 0f)
            {
                CancelPendingAttack();
                return;
            }

            if (attackPending)
            {
                if (Time.time < attackHitsAt)
                {
                    return;
                }

                attackPending = false;
                nextAttackTime = Time.time + 1f / attacksPerSecond;
                attackVisual?.ShowImpact();
                targetHealth.TakeDamage(new DamageInfo(
                    contactDamage,
                    target.position,
                    direction,
                    gameObject));
                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            attackPending = true;
            float windup = attackWindupDuration * GetAttackWindupMultiplier();
            attackHitsAt = Time.time + windup;
            attackVisual?.BeginWindup(windup);
        }

        private bool HandleDash(float distance, Vector3 direction)
        {
            EnemyConfig config = runtimeStats != null
                ? runtimeStats.Config
                : null;
            if (config == null ||
                config.CombatStyle != EnemyCombatStyle.Dasher)
            {
                CancelDash();
                return false;
            }

            if (dashWindupPending)
            {
                characterController.SimpleMove(Vector3.zero);
                if (Time.time < dashStateEndsAt)
                {
                    return true;
                }

                dashWindupPending = false;
                dashActive = true;
                dashStateEndsAt = Time.time + config.DashDuration;
                dashDirection = direction.sqrMagnitude > 0f
                    ? direction
                    : transform.forward;
            }

            if (dashActive)
            {
                if (Time.time < dashStateEndsAt)
                {
                    characterController.SimpleMove(
                        dashDirection *
                        runtimeStats.MoveSpeedStat.FinalValue *
                        config.DashSpeedMultiplier);
                    return true;
                }

                dashActive = false;
                nextDashTime = Time.time + config.DashCooldown;
            }

            float minimumDashDistance =
                runtimeStats.StoppingDistanceStat.FinalValue * 1.8f;
            if (Time.time < nextDashTime ||
                distance <= minimumDashDistance ||
                distance > 11f ||
                direction.sqrMagnitude <= 0f)
            {
                return false;
            }

            CancelPendingAttack();
            dashWindupPending = true;
            dashStateEndsAt = Time.time + config.DashWindup;
            dashDirection = direction;
            characterController.SimpleMove(Vector3.zero);
            attackVisual?.BeginWindup(config.DashWindup);
            return true;
        }

        private float GetAttackWindupMultiplier()
        {
            EnemyConfig config = runtimeStats != null
                ? runtimeStats.Config
                : null;
            return config != null
                ? config.AttackWindupMultiplier
                : 1f;
        }

        private void CancelDash()
        {
            if (!dashWindupPending && !dashActive)
            {
                return;
            }

            dashWindupPending = false;
            dashActive = false;
            dashStateEndsAt = 0f;
            attackVisual?.Cancel();
        }

        private void CancelPendingAttack()
        {
            if (!attackPending)
            {
                return;
            }

            attackPending = false;
            attackHitsAt = 0f;
            attackVisual?.Cancel();
        }
    }
}
