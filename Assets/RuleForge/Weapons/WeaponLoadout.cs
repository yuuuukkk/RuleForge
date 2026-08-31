using System;
using RuleForge.Config;
using UnityEngine;

namespace RuleForge.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponRuntimeStats))]
    [RequireComponent(typeof(WeaponController))]
    public sealed class WeaponLoadout : MonoBehaviour
    {
        [SerializeField] private WeaponRuntimeStats runtimeStats;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Transform weaponVisual;
        [SerializeField] private WeaponVisualController weaponVisualController;
        [SerializeField] private WeaponConfig[] weapons =
            Array.Empty<WeaponConfig>();
        [SerializeField] private int currentWeaponIndex;

        [NonSerialized] private MaterialPropertyBlock visualProperties;

        public event Action<WeaponConfig> WeaponChanged;

        public WeaponConfig CurrentWeapon =>
            weapons != null &&
            currentWeaponIndex >= 0 &&
            currentWeaponIndex < weapons.Length
                ? weapons[currentWeaponIndex]
                : runtimeStats != null
                    ? runtimeStats.Config
                    : null;

        public string CurrentWeaponName => CurrentWeapon != null
            ? CurrentWeapon.DisplayName
            : "No Weapon";

        public int WeaponCount => weapons != null ? weapons.Length : 0;

        public WeaponConfig GetWeapon(int index)
        {
            return weapons != null && index >= 0 && index < weapons.Length
                ? weapons[index]
                : null;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (weapons != null && weapons.Length > 0)
            {
                Equip(currentWeaponIndex, true);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Equip(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Equip(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Equip(2);
            }
        }

        public void Configure(
            WeaponRuntimeStats stats,
            WeaponController controller,
            Transform visual,
            WeaponConfig[] weaponConfigs)
        {
            runtimeStats = stats;
            weaponController = controller;
            weaponVisual = visual;
            weapons = weaponConfigs ?? Array.Empty<WeaponConfig>();
            currentWeaponIndex = Mathf.Clamp(
                currentWeaponIndex,
                0,
                Mathf.Max(0, weapons.Length - 1));
        }

        public void ConfigureArt(WeaponVisualController artController)
        {
            weaponVisualController = artController;
        }

        public bool EquipByName(string weaponName)
        {
            if (string.Equals(
                    weaponName,
                    "Default Rifle",
                    StringComparison.OrdinalIgnoreCase))
            {
                weaponName = "Assault Rifle";
            }

            if (weapons == null)
            {
                return false;
            }

            for (int index = 0; index < weapons.Length; index++)
            {
                WeaponConfig candidate = weapons[index];
                if (candidate != null &&
                    string.Equals(
                        candidate.DisplayName,
                        weaponName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Equip(index);
                }
            }

            return false;
        }

        public bool Equip(int index, bool force = false)
        {
            ResolveReferences();
            if (weapons == null ||
                index < 0 ||
                index >= weapons.Length ||
                weapons[index] == null ||
                runtimeStats == null ||
                weaponController == null)
            {
                return false;
            }

            if (!force && index == currentWeaponIndex &&
                runtimeStats.Config == weapons[index])
            {
                return true;
            }

            currentWeaponIndex = index;
            runtimeStats.Configure(weapons[index]);
            weaponController.ResetAmmo();
            ApplyVisual(weapons[index]);
            WeaponChanged?.Invoke(weapons[index]);
            return true;
        }

        private void ResolveReferences()
        {
            runtimeStats = runtimeStats != null
                ? runtimeStats
                : GetComponent<WeaponRuntimeStats>();
            weaponController = weaponController != null
                ? weaponController
                : GetComponent<WeaponController>();
            if (weaponVisual == null)
            {
                Transform candidate = transform.Find("WeaponVisual");
                weaponVisual = candidate != null ? candidate : transform;
            }

            weaponVisualController = weaponVisualController != null
                ? weaponVisualController
                : weaponVisual.GetComponent<WeaponVisualController>();
        }

        private void ApplyVisual(WeaponConfig config)
        {
            if (weaponVisual == null || config == null)
            {
                return;
            }

            if (visualProperties == null)
            {
                visualProperties = new MaterialPropertyBlock();
            }

            if (weaponVisualController != null &&
                weaponVisualController.HasBinding(currentWeaponIndex))
            {
                weaponVisual.localScale = Vector3.one;
                weaponVisualController.Apply(
                    currentWeaponIndex,
                    config.VisualColor);
                return;
            }

            weaponVisual.localScale = config.VisualScale;
            Renderer[] renderers = weaponVisual.GetComponentsInChildren<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                renderer.GetPropertyBlock(visualProperties);
                visualProperties.SetColor("_Color", config.VisualColor);
                visualProperties.SetColor("_BaseColor", config.VisualColor);
                renderer.SetPropertyBlock(visualProperties);
            }
        }
    }
}
