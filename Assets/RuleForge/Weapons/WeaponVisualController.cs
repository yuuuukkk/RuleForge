using System;
using RuleForge.UI;
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
        [Header("Fire Feedback")]
        [SerializeField, Min(0f)] private float recoilDistance = 0.035f;
        [SerializeField, Min(0f)] private float recoilAngle = 2.5f;
        [SerializeField, Min(0.1f)] private float recoilReturnSpeed = 18f;
        [Header("First Person Motion")]
        [SerializeField, Min(0f)] private float swayPositionAmount = 0.018f;
        [SerializeField, Min(0f)] private float swayRotationAmount = 2.2f;
        [SerializeField, Min(0.1f)] private float swaySmoothSpeed = 14f;
        [SerializeField, Min(0f)] private float walkBobAmount = 0.012f;
        [SerializeField, Min(0.1f)] private float walkBobFrequency = 8.5f;
        [SerializeField, Min(0f)] private float equipDropDistance = 0.22f;
        [SerializeField, Min(0.1f)] private float equipSpeed = 5.5f;
        [Header("Viewmodel Framing")]
        [SerializeField, Min(0.1f)] private float maximumModelSize = 0.58f;
        [SerializeField] private bool preserveImportedMaterialColors = true;

        [NonSerialized] private MaterialPropertyBlock properties;
        private GameObject activeModel;
        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;
        private float recoilAmount;
        private float bobClock;
        private float equipAmount;
        private float recoilDistanceMultiplier = 1f;
        private float recoilAngleMultiplier = 1f;
        private Vector3 smoothedSwayPosition;
        private Vector3 smoothedSwayRotation;
        private Transform muzzleAnchor;
        private float reloadRemaining;
        private float reloadDuration;

        public WeaponVisualBinding[] Bindings => bindings;

        private void Awake()
        {
            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
        }

        private void LateUpdate()
        {
            recoilAmount = Mathf.MoveTowards(
                recoilAmount,
                0f,
                recoilReturnSpeed * Time.deltaTime);
            equipAmount = Mathf.MoveTowards(
                equipAmount,
                0f,
                equipSpeed * Time.deltaTime);

            float lookX = RuntimeInputGate.IsBlocked
                ? 0f
                : Input.GetAxisRaw("Mouse X");
            float lookY = RuntimeInputGate.IsBlocked
                ? 0f
                : Input.GetAxisRaw("Mouse Y");
            Vector3 swayPositionTarget = new Vector3(
                -lookX,
                -lookY,
                0f) * swayPositionAmount;
            Vector3 swayRotationTarget = new Vector3(
                lookY,
                -lookX,
                -lookX * 0.6f) * swayRotationAmount;
            float smoothing = 1f - Mathf.Exp(
                -swaySmoothSpeed * Time.deltaTime);
            smoothedSwayPosition = Vector3.Lerp(
                smoothedSwayPosition,
                swayPositionTarget,
                smoothing);
            smoothedSwayRotation = Vector3.Lerp(
                smoothedSwayRotation,
                swayRotationTarget,
                smoothing);

            float movement = RuntimeInputGate.IsBlocked
                ? 0f
                : Mathf.Clamp01(new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")).magnitude);
            if (movement > 0.01f)
            {
                bobClock += Time.deltaTime * walkBobFrequency;
            }
            else
            {
                bobClock = Mathf.MoveTowards(
                    bobClock,
                    0f,
                    Time.deltaTime * walkBobFrequency);
            }
            Vector3 bobOffset = new Vector3(
                Mathf.Sin(bobClock) * walkBobAmount,
                Mathf.Abs(Mathf.Cos(bobClock)) * walkBobAmount,
                0f) * movement;
            float reloadPhase = reloadDuration > 0f && reloadRemaining > 0f
                ? 1f - reloadRemaining / reloadDuration
                : 0f;
            if (reloadRemaining > 0f)
            {
                reloadRemaining = Mathf.Max(
                    0f,
                    reloadRemaining - Time.deltaTime);
            }
            float reloadArc = Mathf.Sin(reloadPhase * Mathf.PI);
            Vector3 reloadOffset = new Vector3(
                0.08f * reloadArc,
                -0.16f * reloadArc,
                -0.04f * reloadArc);
            Vector3 reloadRotation = new Vector3(
                12f * reloadArc,
                -8f * reloadArc,
                28f * reloadArc);

            transform.localPosition = restLocalPosition +
                                      smoothedSwayPosition +
                                      bobOffset +
                                      reloadOffset +
                                      Vector3.down *
                                      (equipDropDistance * equipAmount) +
                                      Vector3.back *
                                      (recoilDistance *
                                       recoilDistanceMultiplier *
                                       recoilAmount);
            transform.localRotation = restLocalRotation * Quaternion.Euler(
                smoothedSwayRotation + reloadRotation + new Vector3(
                    -recoilAngle * recoilAngleMultiplier * recoilAmount,
                    0f,
                    recoilAmount * recoilAngleMultiplier * 0.35f));
        }

        public void PlayRecoil()
        {
            recoilAmount = 1f;
        }

        public void PlayReload(float duration)
        {
            reloadDuration = Mathf.Max(0.1f, duration);
            reloadRemaining = reloadDuration;
        }

        public bool TryGetMuzzleWorldPosition(
            Vector3 fireDirection,
            out Vector3 muzzlePosition)
        {
            Vector3 direction = fireDirection.sqrMagnitude > 0.0001f
                ? fireDirection.normalized
                : transform.forward;
            if (muzzleAnchor != null)
            {
                muzzlePosition = muzzleAnchor.position;
                return true;
            }

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
            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
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
            NormalizeOversizedModel();
            ConfigureWeaponMotion(index);
            CreateMuzzleAnchor();

            if (preserveImportedMaterialColors)
            {
                return;
            }

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

        private void NormalizeOversizedModel()
        {
            if (activeModel == null)
            {
                return;
            }

            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;
            Bounds combinedBounds = default;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return;
            }

            float largestDimension = Mathf.Max(
                combinedBounds.size.x,
                combinedBounds.size.y,
                combinedBounds.size.z);
            if (largestDimension <= maximumModelSize ||
                largestDimension <= Mathf.Epsilon)
            {
                return;
            }

            float factor = maximumModelSize / largestDimension;
            activeModel.transform.localScale *= factor;
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
            muzzleAnchor = null;
            if (activeModel == null)
            {
                return;
            }

            Destroy(activeModel);
            activeModel = null;
        }

        private void ConfigureWeaponMotion(int index)
        {
            switch (index)
            {
                case 1:
                    recoilDistanceMultiplier = 1.45f;
                    recoilAngleMultiplier = 1.5f;
                    break;
                case 2:
                    recoilDistanceMultiplier = 0.8f;
                    recoilAngleMultiplier = 0.7f;
                    break;
                default:
                    recoilDistanceMultiplier = 1f;
                    recoilAngleMultiplier = 1f;
                    break;
            }

            equipAmount = 1f;
            recoilAmount = 0f;
            reloadRemaining = 0f;
            smoothedSwayPosition = Vector3.zero;
            smoothedSwayRotation = Vector3.zero;
        }

        private void CreateMuzzleAnchor()
        {
            if (activeModel == null)
            {
                return;
            }

            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>();
            Vector3 direction = transform.forward;
            float furthestDistance = float.NegativeInfinity;
            Vector3 furthestPoint = transform.position + direction * 0.5f;
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
                    if (distance > furthestDistance)
                    {
                        furthestDistance = distance;
                        furthestPoint = corner;
                    }
                }
            }

            GameObject anchor = new GameObject("MuzzlePoint");
            muzzleAnchor = anchor.transform;
            muzzleAnchor.SetParent(activeModel.transform, true);
            muzzleAnchor.position = furthestPoint;
            muzzleAnchor.rotation = transform.rotation;
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
