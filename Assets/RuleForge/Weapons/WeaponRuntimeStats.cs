using RuleForge.Config;
using RuleForge.Runtime.Stats;
using UnityEngine;

namespace RuleForge.Weapons
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class WeaponRuntimeStats : MonoBehaviour, IRuntimeStatProvider
    {
        [SerializeField] private WeaponConfig config;

        [Header("Runtime Stat Debug")]
        [SerializeField] private RuntimeStat damage = new RuntimeStat();
        [SerializeField] private RuntimeStat range = new RuntimeStat();
        [SerializeField] private RuntimeStat shotsPerSecond = new RuntimeStat();
        [SerializeField] private RuntimeStat magazineCapacity = new RuntimeStat();
        [SerializeField] private RuntimeStat startingReserveAmmo = new RuntimeStat();
        [SerializeField] private RuntimeStat reloadDuration = new RuntimeStat();

        public WeaponConfig Config => config;
        public bool IsConfigured => config != null;
        public LayerMask HitMask => config != null ? config.HitMask : 0;
        public RuntimeStat DamageStat => damage;
        public RuntimeStat RangeStat => range;
        public RuntimeStat ShotsPerSecondStat => shotsPerSecond;
        public RuntimeStat MagazineCapacityStat => magazineCapacity;
        public RuntimeStat StartingReserveAmmoStat => startingReserveAmmo;
        public RuntimeStat ReloadDurationStat => reloadDuration;

        private void Awake()
        {
            RefreshBaseValues();
        }

        private void Start()
        {
            if (!IsConfigured)
            {
                Debug.LogError("WeaponRuntimeStats requires a WeaponConfig.", this);
            }
        }

        private void OnValidate()
        {
            RefreshBaseValues();
        }

        public void Configure(WeaponConfig newConfig)
        {
            config = newConfig;
            RefreshBaseValues();
        }

        public void RefreshBaseValues()
        {
            if (config == null)
            {
                return;
            }

            damage.Initialize(RuntimeStatId.WeaponDamage, config.Damage);
            range.Initialize(RuntimeStatId.WeaponRange, config.Range, 0.01f);
            shotsPerSecond.Initialize(
                RuntimeStatId.WeaponShotsPerSecond,
                config.ShotsPerSecond,
                0.01f);
            magazineCapacity.Initialize(
                RuntimeStatId.WeaponMagazineCapacity,
                config.MagazineCapacity,
                1f);
            startingReserveAmmo.Initialize(
                RuntimeStatId.WeaponStartingReserveAmmo,
                config.StartingReserveAmmo);
            reloadDuration.Initialize(
                RuntimeStatId.WeaponReloadDuration,
                config.ReloadDuration);
        }

        public bool TryGetStat(RuntimeStatId statId, out RuntimeStat runtimeStat)
        {
            switch (statId)
            {
                case RuntimeStatId.WeaponDamage:
                    runtimeStat = damage;
                    return true;
                case RuntimeStatId.WeaponRange:
                    runtimeStat = range;
                    return true;
                case RuntimeStatId.WeaponShotsPerSecond:
                    runtimeStat = shotsPerSecond;
                    return true;
                case RuntimeStatId.WeaponMagazineCapacity:
                    runtimeStat = magazineCapacity;
                    return true;
                case RuntimeStatId.WeaponStartingReserveAmmo:
                    runtimeStat = startingReserveAmmo;
                    return true;
                case RuntimeStatId.WeaponReloadDuration:
                    runtimeStat = reloadDuration;
                    return true;
                default:
                    runtimeStat = null;
                    return false;
            }
        }

        public void ClearAllModifiers()
        {
            damage.ClearModifiers();
            range.ClearModifiers();
            shotsPerSecond.ClearModifiers();
            magazineCapacity.ClearModifiers();
            startingReserveAmmo.ClearModifiers();
            reloadDuration.ClearModifiers();
        }
    }
}
