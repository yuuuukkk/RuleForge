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
        private Transform target;
        private PlayerHealth targetHealth;
        private float nextAttackTime;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            runtimeStats = GetComponent<EnemyRuntimeStats>();
        }

        private void Update()
        {
            if (!TryAcquireTarget())
            {
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
                attacksPerSecond <= 0f ||
                Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + 1f / attacksPerSecond;
            targetHealth.TakeDamage(new DamageInfo(
                contactDamage,
                target.position,
                direction,
                gameObject));
        }
    }
}
