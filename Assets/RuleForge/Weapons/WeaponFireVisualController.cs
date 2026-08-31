using System;
using UnityEngine;

namespace RuleForge.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponFireVisualController : MonoBehaviour
    {
        private const int TracerPoolSize = 16;
        private const int FlashPoolSize = 8;

        [Header("Tracer")]
        [SerializeField, Min(0.005f)] private float tracerWidth = 0.025f;
        [SerializeField, Min(0.01f)] private float tracerLifetime = 0.065f;
        [SerializeField] private Color tracerColor =
            new Color(0.15f, 0.85f, 1f, 1f);

        [Header("Muzzle / Impact")]
        [SerializeField, Min(0.01f)] private float muzzleSize = 0.09f;
        [SerializeField, Min(0.01f)] private float impactSize = 0.065f;
        [SerializeField, Min(0.01f)] private float flashLifetime = 0.08f;
        [SerializeField] private Color muzzleColor =
            new Color(1f, 0.72f, 0.15f, 1f);
        [SerializeField] private Color impactColor =
            new Color(0.35f, 0.9f, 1f, 1f);

        private Camera aimCamera;
        private WeaponVisualController weaponVisual;
        private Transform effectRoot;
        private LineRenderer[] tracers;
        private float[] tracerExpiry;
        private GameObject[] flashes;
        private float[] flashExpiry;
        private Material tracerMaterial;
        private Material muzzleMaterial;
        private Material impactMaterial;

        public void Configure(
            Camera cameraReference,
            WeaponVisualController visualController)
        {
            aimCamera = cameraReference;
            weaponVisual = visualController;
        }

        private void Awake()
        {
            BuildPool();
        }

        private void Update()
        {
            for (int index = 0; index < tracers.Length; index++)
            {
                if (tracers[index].gameObject.activeSelf &&
                    Time.time >= tracerExpiry[index])
                {
                    tracers[index].gameObject.SetActive(false);
                }
            }

            for (int index = 0; index < flashes.Length; index++)
            {
                if (flashes[index].activeSelf &&
                    Time.time >= flashExpiry[index])
                {
                    flashes[index].SetActive(false);
                }
            }
        }

        public void PlayShot(
            Vector3 fireDirection,
            Vector3 hitPoint,
            bool didHit)
        {
            if (effectRoot == null)
            {
                BuildPool();
            }

            Vector3 muzzlePosition = ResolveMuzzlePosition(fireDirection);
            ShowTracer(muzzlePosition, hitPoint);
            ShowFlash(muzzlePosition, muzzleSize, muzzleMaterial);
            if (didHit)
            {
                ShowFlash(hitPoint, impactSize, impactMaterial);
            }
        }

        private Vector3 ResolveMuzzlePosition(Vector3 fireDirection)
        {
            if (weaponVisual != null &&
                weaponVisual.TryGetMuzzleWorldPosition(
                    fireDirection,
                    out Vector3 modelMuzzle))
            {
                return modelMuzzle;
            }

            Transform cameraTransform = aimCamera != null
                ? aimCamera.transform
                : transform;
            return cameraTransform.position +
                   cameraTransform.forward * 0.55f +
                   cameraTransform.right * 0.18f -
                   cameraTransform.up * 0.16f;
        }

        private void BuildPool()
        {
            if (effectRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("WeaponFireVisuals");
            effectRoot = root.transform;
            tracerMaterial = CreateUnlitMaterial(tracerColor);
            muzzleMaterial = CreateUnlitMaterial(muzzleColor);
            impactMaterial = CreateUnlitMaterial(impactColor);

            tracers = new LineRenderer[TracerPoolSize];
            tracerExpiry = new float[TracerPoolSize];
            for (int index = 0; index < tracers.Length; index++)
            {
                GameObject tracerObject = new GameObject("Tracer_" + index);
                tracerObject.transform.SetParent(effectRoot, false);
                LineRenderer tracer = tracerObject.AddComponent<LineRenderer>();
                tracer.useWorldSpace = true;
                tracer.positionCount = 2;
                tracer.startWidth = tracerWidth;
                tracer.endWidth = tracerWidth * 0.3f;
                tracer.numCapVertices = 2;
                tracer.material = tracerMaterial;
                tracer.startColor = tracerColor;
                tracer.endColor = new Color(
                    tracerColor.r,
                    tracerColor.g,
                    tracerColor.b,
                    0.15f);
                tracerObject.SetActive(false);
                tracers[index] = tracer;
            }

            flashes = new GameObject[FlashPoolSize];
            flashExpiry = new float[FlashPoolSize];
            for (int index = 0; index < flashes.Length; index++)
            {
                GameObject flash = GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
                flash.name = "Flash_" + index;
                flash.transform.SetParent(effectRoot, false);
                Collider collider = flash.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                flash.SetActive(false);
                flashes[index] = flash;
            }
        }

        private void ShowTracer(Vector3 start, Vector3 end)
        {
            int index = FindFreeTracer();
            LineRenderer tracer = tracers[index];
            tracer.startWidth = tracerWidth;
            tracer.endWidth = tracerWidth * 0.3f;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);
            tracerExpiry[index] = Time.time + tracerLifetime;
            tracer.gameObject.SetActive(true);
        }

        private void ShowFlash(
            Vector3 position,
            float size,
            Material material)
        {
            int index = FindFreeFlash();
            GameObject flash = flashes[index];
            flash.transform.position = position;
            flash.transform.localScale = Vector3.one * size;
            Renderer renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            flashExpiry[index] = Time.time + flashLifetime;
            flash.SetActive(true);
        }

        private int FindFreeTracer()
        {
            int oldestIndex = 0;
            for (int index = 0; index < tracers.Length; index++)
            {
                if (!tracers[index].gameObject.activeSelf)
                {
                    return index;
                }

                if (tracerExpiry[index] < tracerExpiry[oldestIndex])
                {
                    oldestIndex = index;
                }
            }

            return oldestIndex;
        }

        private int FindFreeFlash()
        {
            int oldestIndex = 0;
            for (int index = 0; index < flashes.Length; index++)
            {
                if (!flashes[index].activeSelf)
                {
                    return index;
                }

                if (flashExpiry[index] < flashExpiry[oldestIndex])
                {
                    oldestIndex = index;
                }
            }

            return oldestIndex;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                color = color
            };
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        private void OnDestroy()
        {
            if (effectRoot != null)
            {
                Destroy(effectRoot.gameObject);
            }

            DestroyMaterial(tracerMaterial);
            DestroyMaterial(muzzleMaterial);
            DestroyMaterial(impactMaterial);
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
