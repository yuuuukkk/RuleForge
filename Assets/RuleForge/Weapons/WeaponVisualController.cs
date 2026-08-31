using System;
using UnityEngine;

namespace RuleForge.Weapons
{
    [Serializable]
    public struct WeaponVisualBinding
    {
        public string weaponName;
        public GameObject modelPrefab;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
    }

    [DisallowMultipleComponent]
    public sealed class WeaponVisualController : MonoBehaviour
    {
        [SerializeField] private WeaponVisualBinding[] bindings =
            Array.Empty<WeaponVisualBinding>();

        [NonSerialized] private MaterialPropertyBlock properties;
        private GameObject activeModel;

        public WeaponVisualBinding[] Bindings => bindings;

        public bool TryGetMuzzleWorldPosition(
            Vector3 fireDirection,
            out Vector3 muzzlePosition)
        {
            Vector3 direction = fireDirection.sqrMagnitude > 0.0001f
                ? fireDirection.normalized
                : transform.forward;
            Renderer[] renderers = activeModel != null
                ? activeModel.GetComponentsInChildren<Renderer>()
                : GetComponentsInChildren<Renderer>();
            bool foundRenderer = false;
            float furthestDistance = float.NegativeInfinity;
            muzzlePosition = transform.position + direction * 0.5f;

            for (int rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;
                for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
                {
                    Vector3 corner = center + new Vector3(
                        (cornerIndex & 1) == 0 ? -extents.x : extents.x,
                        (cornerIndex & 2) == 0 ? -extents.y : extents.y,
                        (cornerIndex & 4) == 0 ? -extents.z : extents.z);
                    float distance = Vector3.Dot(corner, direction);
                    if (distance <= furthestDistance)
                    {
                        continue;
                    }

                    foundRenderer = true;
                    furthestDistance = distance;
                    muzzlePosition = corner;
                }
            }

            return foundRenderer;
        }

        public void Configure(WeaponVisualBinding[] visualBindings)
        {
            bindings = visualBindings ?? Array.Empty<WeaponVisualBinding>();
        }

        public bool HasBinding(int index)
        {
            return index >= 0 &&
                   index < bindings.Length &&
                   bindings[index].modelPrefab != null;
        }

        public void Apply(int index, Color tint)
        {
            if (!HasBinding(index))
            {
                SetFallbackRenderersVisible(true);
                return;
            }

            RemoveActiveModel();
            WeaponVisualBinding binding = bindings[index];
            UnityEngine.Object instance = Instantiate(
                (UnityEngine.Object)binding.modelPrefab,
                transform);
            activeModel = ResolveModelInstance(instance);
            if (activeModel == null)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }

                Debug.LogError(
                    "Weapon art asset did not instantiate as a GameObject or Component: " +
                    binding.modelPrefab.name,
                    this);
                SetFallbackRenderersVisible(true);
                return;
            }

            SetFallbackRenderersVisible(false);
            activeModel.name = "ArtModel_" + binding.weaponName;
            activeModel.transform.localPosition = binding.localPosition;
            activeModel.transform.localRotation = Quaternion.Euler(
                binding.localEulerAngles);
            activeModel.transform.localScale = new Vector3(
                Mathf.Max(0.001f, binding.localScale.x),
                Mathf.Max(0.001f, binding.localScale.y),
                Mathf.Max(0.001f, binding.localScale.z));

            if (properties == null)
            {
                properties = new MaterialPropertyBlock();
            }
            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>();
            for (int rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", tint);
                properties.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(properties);
            }
        }

        private static GameObject ResolveModelInstance(
            UnityEngine.Object instance)
        {
            GameObject gameObject = instance as GameObject;
            if (gameObject != null)
            {
                return gameObject;
            }

            Component component = instance as Component;
            return component != null ? component.gameObject : null;
        }

        private void RemoveActiveModel()
        {
            if (activeModel == null)
            {
                return;
            }

            Destroy(activeModel);
            activeModel = null;
        }

        private void SetFallbackRenderersVisible(bool visible)
        {
            Renderer[] renderers = GetComponents<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].enabled = visible;
            }
        }
    }
}
