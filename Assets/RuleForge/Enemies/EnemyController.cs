using RuleForge.Player;
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
            attackHitsAt = Time.time + attackWindupDuration;
            attackVisual?.BeginWindup(attackWindupDuration);
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
