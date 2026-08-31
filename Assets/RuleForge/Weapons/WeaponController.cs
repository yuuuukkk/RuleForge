using System;
using RuleForge.Enemies;
using RuleForge.Runtime;
using RuleForge.Runtime.Stats;
using RuleForge.Rules;
using RuleForge.Player;
using RuleForge.UI;
using UnityEngine;

namespace RuleForge.Weapons
{
    [RequireComponent(typeof(WeaponRuntimeStats))]
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField, Range(0.5f, 0.95f)]
        private float headshotHeightThreshold = 0.72f;

        private WeaponRuntimeStats runtimeStats;
        private PlayerHealth ownerHealth;
        private WeaponFireVisualController fireVisuals;
        private float nextFireTime;

        [Header("Runtime Ammo Debug")]
        [SerializeField] private int currentMagazineAmmo;
        [SerializeField] private int reserveAmmo;
        [SerializeField] private bool isReloading;

        private float reloadCompleteTime;

        public event Action Fired;

        public float Damage => runtimeStats != null
            ? runtimeStats.DamageStat.FinalValue
            : 0f;

        public float Range => runtimeStats != null
            ? runtimeStats.RangeStat.FinalValue
            : 0f;

        public int CurrentMagazineAmmo => currentMagazineAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;

        private void Awake()
        {
            runtimeStats = GetComponent<WeaponRuntimeStats>();
            ownerHealth = GetComponentInParent<PlayerHealth>();
            fireVisuals = GetComponent<WeaponFireVisualController>();
            if (fireVisuals == null)
            {
                fireVisuals = gameObject.AddComponent<WeaponFireVisualController>();
            }

            fireVisuals.Configure(
                aimCamera,
                GetComponentInChildren<WeaponVisualController>(true));
            ResetAmmo();
        }

        private void Update()
        {
            if (isReloading && Time.time >= reloadCompleteTime)
            {
                CompleteReload();
            }

            if (RuntimeInputGate.IsBlocked ||
                (ownerHealth != null && !ownerHealth.IsAlive))
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                TryReload();
            }

            bool shouldFire = runtimeStats != null &&
                              runtimeStats.Config != null &&
                              runtimeStats.Config.AutomaticFire
                ? Input.GetButton("Fire1")
                : Input.GetButtonDown("Fire1");
            if (shouldFire)
            {
                TryFire();
            }
        }

        private void OnDisable()
        {
            isReloading = false;
        }

        public void SetAimCamera(Camera newAimCamera)
        {
            aimCamera = newAimCamera;
            if (fireVisuals != null)
            {
                fireVisuals.Configure(
                    aimCamera,
                    GetComponentInChildren<WeaponVisualController>(true));
            }
        }

        public bool TryFire()
        {
            float damage = Damage;
            float range = Range;
            float shotsPerSecond = runtimeStats != null
                ? runtimeStats.ShotsPerSecondStat.FinalValue
                : 0f;
            if (aimCamera == null ||
                (ownerHealth != null && !ownerHealth.IsAlive) ||
                damage <= 0f ||
                range <= 0f ||
                shotsPerSecond <= 0f ||
                isReloading ||
                currentMagazineAmmo <= 0)
            {
                return false;
            }

            if (Time.time < nextFireTime)
            {
                return false;
            }

            nextFireTime = Time.time + 1f / shotsPerSecond;
            currentMagazineAmmo--;
            Fired?.Invoke();
            GameplayEventBus.Publish(new GameplayEvent(
                GameplayEventType.WeaponFired,
                gameObject,
                gameObject,
                value: damage));

            int pelletCount = runtimeStats.Config != null
                ? Mathf.Max(1, runtimeStats.Config.PelletsPerShot)
                : 1;
            float spreadDegrees = runtimeStats.Config != null
                ? Mathf.Max(0f, runtimeStats.Config.SpreadDegrees)
                : 0f;
            for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
            {
                FireRay(damage, range, spreadDegrees);
            }

            return true;
        }

        private void FireRay(
            float damage,
            float range,
            float spreadDegrees)
        {
            Vector2 spread = UnityEngine.Random.insideUnitCircle * spreadDegrees;
            Vector3 direction = Quaternion.AngleAxis(
                                    spread.x,
                                    aimCamera.transform.up) *
                                Quaternion.AngleAxis(
                                    -spread.y,
                                    aimCamera.transform.right) *
                                aimCamera.transform.forward;
            Ray ray = new Ray(aimCamera.transform.position, direction.normalized);
            bool didHit = Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    range,
                    runtimeStats.HitMask,
                    QueryTriggerInteraction.Ignore);
            Vector3 endPoint = didHit
                ? hit.point
                : ray.origin + ray.direction * range;
            if (fireVisuals != null)
            {
                fireVisuals.PlayShot(ray.direction, endPoint, didHit);
            }

            if (!didHit)
            {
                return;
            }

            if (!TryFindDamageable(hit.collider, out IDamageable damageable))
            {
                return;
            }

            EnemyHealth enemyHealth = damageable as EnemyHealth;
            float healthBefore = enemyHealth != null
                ? enemyHealth.CurrentHealth
                : 0f;
            bool isHeadshot = enemyHealth != null &&
                              IsHeadshot(hit.collider, hit.point);
            damageable.TakeDamage(new DamageInfo(
                damage,
                hit.point,
                ray.direction,
                gameObject));
            if (enemyHealth != null && enemyHealth.CurrentHealth < healthBefore)
            {
                EnemyRuntimeIdentity identity =
                    enemyHealth.GetComponent<EnemyRuntimeIdentity>();
                string enemyType = identity != null
                    ? identity.EnemyType
                    : "Grunt";
                float appliedDamage = healthBefore - enemyHealth.CurrentHealth;
                GameplayEventBus.Publish(new GameplayEvent(
                    GameplayEventType.EnemyHit,
                    enemyHealth.gameObject,
                    gameObject,
                    enemyType,
                    appliedDamage));
                if (isHeadshot)
                {
                    GameplayEventBus.Publish(new GameplayEvent(
                        GameplayEventType.Headshot,
                        enemyHealth.gameObject,
                        gameObject,
                        enemyType,
                        appliedDamage));
                }
            }
        }

        private bool IsHeadshot(Collider hitCollider, Vector3 hitPoint)
        {
            if (hitCollider == null || hitCollider.bounds.size.y <= 0.001f)
            {
                return false;
            }

            float normalizedHeight = Mathf.InverseLerp(
                hitCollider.bounds.min.y,
                hitCollider.bounds.max.y,
                hitPoint.y);
            return normalizedHeight >= headshotHeightThreshold;
        }

        public bool TryReload()
        {
            int magazineCapacity = GetRoundedStat(
                runtimeStats != null ? runtimeStats.MagazineCapacityStat : null,
                1);
            if ((ownerHealth != null && !ownerHealth.IsAlive) ||
                isReloading ||
                currentMagazineAmmo >= magazineCapacity ||
                reserveAmmo <= 0)
            {
                return false;
            }

            isReloading = true;
            float reloadDuration = runtimeStats != null
                ? runtimeStats.ReloadDurationStat.FinalValue
                : 0f;
            reloadCompleteTime = Time.time + Mathf.Max(0f, reloadDuration);
            GameplayEventBus.Publish(new GameplayEvent(
                GameplayEventType.PlayerReload,
                gameObject,
                gameObject));

            if (reloadDuration <= 0f)
            {
                CompleteReload();
            }

            return true;
        }

        public void AddReserveAmmo(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            reserveAmmo = amount > int.MaxValue - reserveAmmo
                ? int.MaxValue
                : reserveAmmo + amount;
        }

        public void ResetAmmo()
        {
            currentMagazineAmmo = GetRoundedStat(
                runtimeStats != null ? runtimeStats.MagazineCapacityStat : null,
                1);
            reserveAmmo = GetRoundedStat(
                runtimeStats != null ? runtimeStats.StartingReserveAmmoStat : null,
                0);
            isReloading = false;
        }

        private void CompleteReload()
        {
            int magazineCapacity = GetRoundedStat(
                runtimeStats != null ? runtimeStats.MagazineCapacityStat : null,
                1);
            int missingAmmo = Mathf.Max(0, magazineCapacity - currentMagazineAmmo);
            int transferredAmmo = Mathf.Min(missingAmmo, reserveAmmo);
            currentMagazineAmmo += transferredAmmo;
            reserveAmmo -= transferredAmmo;
            isReloading = false;
        }

        private static int GetRoundedStat(
            RuntimeStat stat,
            int minimum)
        {
            return stat != null
                ? Mathf.Max(minimum, Mathf.RoundToInt(stat.FinalValue))
                : minimum;
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
