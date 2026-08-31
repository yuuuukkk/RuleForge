using UnityEngine;

namespace RuleForge.Config
{
    public enum WeaponArchetype
    {
        AssaultRifle,
        Shotgun,
        Sniper
    }

    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "RuleForge/Config/Weapon")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Assault Rifle";
        [SerializeField] private WeaponArchetype archetype =
            WeaponArchetype.AssaultRifle;

        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0.01f)] private float range = 100f;
        [SerializeField, Min(0.01f)] private float shotsPerSecond = 4f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Ammo")]
        [SerializeField, Min(1)] private int magazineCapacity = 10;
        [SerializeField, Min(0)] private int startingReserveAmmo = 30;
        [SerializeField, Min(0f)] private float reloadDuration = 1.5f;

        [Header("Firing Pattern")]
        [SerializeField, Min(1)] private int pelletsPerShot = 1;
        [SerializeField, Min(0f)] private float spreadDegrees;
        [SerializeField] private bool automaticFire = true;

        [Header("Simple Runtime Visual")]
        [SerializeField] private Color visualColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Vector3 visualScale = new Vector3(0.18f, 0.12f, 0.7f);

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? archetype.ToString()
            : displayName;
        public WeaponArchetype Archetype => archetype;
        public float Damage => damage;
        public float Range => range;
        public float ShotsPerSecond => shotsPerSecond;
        public LayerMask HitMask => hitMask;
        public int MagazineCapacity => magazineCapacity;
        public int StartingReserveAmmo => startingReserveAmmo;
        public float ReloadDuration => reloadDuration;
        public int PelletsPerShot => pelletsPerShot;
        public float SpreadDegrees => spreadDegrees;
        public bool AutomaticFire => automaticFire;
        public Color VisualColor => visualColor;
        public Vector3 VisualScale => visualScale;

        public void ConfigureProfile(
            string name,
            WeaponArchetype weaponArchetype,
            float damageValue,
            float rangeValue,
            float fireRate,
            int magazine,
            int reserveAmmo,
            float reloadSeconds,
            int pellets,
            float spread,
            bool automatic,
            Color color,
            Vector3 scale)
        {
            displayName = name ?? string.Empty;
            archetype = weaponArchetype;
            damage = Mathf.Max(0f, damageValue);
            range = Mathf.Max(0.01f, rangeValue);
            shotsPerSecond = Mathf.Max(0.01f, fireRate);
            magazineCapacity = Mathf.Max(1, magazine);
            startingReserveAmmo = Mathf.Max(0, reserveAmmo);
            reloadDuration = Mathf.Max(0f, reloadSeconds);
            pelletsPerShot = Mathf.Max(1, pellets);
            spreadDegrees = Mathf.Max(0f, spread);
            automaticFire = automatic;
            visualColor = color;
            visualScale = new Vector3(
                Mathf.Max(0.01f, scale.x),
                Mathf.Max(0.01f, scale.y),
                Mathf.Max(0.01f, scale.z));
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            range = Mathf.Max(0.01f, range);
            shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
            magazineCapacity = Mathf.Max(1, magazineCapacity);
            startingReserveAmmo = Mathf.Max(0, startingReserveAmmo);
            reloadDuration = Mathf.Max(0f, reloadDuration);
            pelletsPerShot = Mathf.Max(1, pelletsPerShot);
            spreadDegrees = Mathf.Max(0f, spreadDegrees);
            visualScale.x = Mathf.Max(0.01f, visualScale.x);
            visualScale.y = Mathf.Max(0.01f, visualScale.y);
            visualScale.z = Mathf.Max(0.01f, visualScale.z);
        }
    }
}
