using System;
using RuleForge.Runtime;
using UnityEngine;

namespace RuleForge.Weapons
{
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;

        [Header("Weapon")]
        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0f)] private float range = 100f;
        [SerializeField, Min(0f)] private float shotsPerSecond = 4f;
        [SerializeField] private LayerMask hitMask = ~0;

        private float nextFireTime;

        public event Action Fired;

        public float Damage => damage;

        public float Range => range;

        private void Update()
        {
            if (Input.GetButton("Fire1"))
            {
                TryFire();
            }
        }

        public void SetAimCamera(Camera newAimCamera)
        {
            aimCamera = newAimCamera;
        }

        public bool TryFire()
        {
            if (aimCamera == null || damage <= 0f || range <= 0f || shotsPerSecond <= 0f)
            {
                return false;
            }

            if (Time.time < nextFireTime)
            {
                return false;
            }

            nextFireTime = Time.time + 1f / shotsPerSecond;
            Fired?.Invoke();

            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    range,
                    hitMask,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            if (TryFindDamageable(hit.collider, out IDamageable damageable))
            {
                damageable.TakeDamage(new DamageInfo(
                    damage,
                    hit.point,
                    ray.direction,
                    gameObject));
            }

            return true;
        }

        private static bool TryFindDamageable(Collider hitCollider, out IDamageable damageable)
        {
            MonoBehaviour[] components = hitCollider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is IDamageable candidate)
                {
                    damageable = candidate;
                    return true;
                }
            }

            damageable = null;
            return false;
        }
    }
}
