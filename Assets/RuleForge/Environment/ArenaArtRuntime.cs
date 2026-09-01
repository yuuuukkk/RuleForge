using UnityEngine;

namespace RuleForge.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ArenaArtRuntime : MonoBehaviour
    {
        private const string VisualRootName = "ArtIntegrationVisuals";

        [Header("Space Station Kit")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject wallPillarPrefab;
        [SerializeField] private GameObject containerPrefab;
        [SerializeField] private GameObject containerTallPrefab;
        [SerializeField] private GameObject containerWidePrefab;
        [SerializeField] private GameObject structurePrefab;
        [SerializeField] private GameObject stairsRampPrefab;
        [SerializeField] private GameObject balconyFloorPrefab;
        [SerializeField] private GameObject railPrefab;
        [SerializeField] private GameObject computerPrefab;

        [Header("Presentation")]
        [SerializeField] private Color directionalLightColor =
            new Color(0.82f, 0.9f, 1f, 1f);
        [SerializeField, Min(0f)] private float directionalLightIntensity = 1.15f;

        private Transform visualRoot;
        private int createdVisualCount;

        private void Awake()
        {
            BuildArenaVisuals();
            if (createdVisualCount > 0)
            {
                SetBlockoutRenderersVisible(false);
            }

            ApplyLighting();
        }

        private void BuildArenaVisuals()
        {
            Transform existing = transform.Find(VisualRootName);
            if (existing != null)
            {
                visualRoot = existing;
                createdVisualCount = existing.childCount;
                return;
            }

            GameObject root = new GameObject(VisualRootName);
            visualRoot = root.transform;
            visualRoot.SetParent(transform, false);

            BuildFloor();
            BuildPerimeter();
            BuildCover();
            BuildRaisedDeck();
            BuildSetDressing();
        }

        private void BuildFloor()
        {
            for (int x = -7; x <= 7; x++)
            {
                for (int z = -7; z <= 7; z++)
                {
                    Spawn(
                        floorPrefab,
                        "Floor_" + x + "_" + z,
                        new Vector3(x * 2f, 0.01f, z * 2f),
                        Vector3.zero,
                        Vector3.one);
                }
            }
        }

        private void BuildPerimeter()
        {
            for (int index = -7; index <= 7; index++)
            {
                float offset = index * 2f;
                Spawn(
                    wallPrefab,
                    "NorthWall_" + index,
                    new Vector3(offset, 0f, 15.35f),
                    Vector3.zero,
                    Vector3.one);
                Spawn(
                    wallPrefab,
                    "SouthWall_" + index,
                    new Vector3(-offset, 0f, -15.35f),
                    new Vector3(0f, 180f, 0f),
                    Vector3.one);
                Spawn(
                    wallPrefab,
                    "EastWall_" + index,
                    new Vector3(15.35f, 0f, -offset),
                    new Vector3(0f, 90f, 0f),
                    Vector3.one);
                Spawn(
                    wallPrefab,
                    "WestWall_" + index,
                    new Vector3(-15.35f, 0f, offset),
                    new Vector3(0f, -90f, 0f),
                    Vector3.one);
            }

            Vector3[] corners =
            {
                new Vector3(-15.35f, 0f, -15.35f),
                new Vector3(-15.35f, 0f, 15.35f),
                new Vector3(15.35f, 0f, -15.35f),
                new Vector3(15.35f, 0f, 15.35f)
            };
            for (int index = 0; index < corners.Length; index++)
            {
                Spawn(
                    wallPillarPrefab,
                    "CornerPillar_" + index,
                    corners[index],
                    Vector3.zero,
                    Vector3.one);
            }
        }

        private void BuildCover()
        {
            Spawn(
                containerWidePrefab,
                "CoverVisual_Left",
                new Vector3(-6f, 0f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(1.4f, 1.35f, 1.2f));
            Spawn(
                containerTallPrefab,
                "CoverVisual_Center",
                new Vector3(0f, 0f, 5f),
                Vector3.zero,
                new Vector3(1.25f, 1.35f, 1.25f));
            Spawn(
                containerWidePrefab,
                "CoverVisual_Right",
                new Vector3(6f, 0f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(1.4f, 1.35f, 1.2f));
            Spawn(
                containerPrefab,
                "SpawnShield_North",
                new Vector3(0f, 0f, 10f),
                Vector3.zero,
                new Vector3(1.3f, 1.3f, 1.3f));
            AddGameplayBox(
                "SpawnShield_North_Collider",
                new Vector3(0f, 0.75f, 10f),
                Vector3.zero,
                new Vector3(1.7f, 1.5f, 1.7f));
            Spawn(
                containerPrefab,
                "SpawnShield_East",
                new Vector3(10f, 0f, 0f),
                new Vector3(0f, 90f, 0f),
                new Vector3(1.3f, 1.3f, 1.3f));
            AddGameplayBox(
                "SpawnShield_East_Collider",
                new Vector3(10f, 0.75f, 0f),
                Vector3.zero,
                new Vector3(1.7f, 1.5f, 1.7f));
        }

        private void BuildRaisedDeck()
        {
            Spawn(
                structurePrefab,
                "RaisedDeckStructure",
                new Vector3(0f, 0f, -9f),
                Vector3.zero,
                new Vector3(2.2f, 1.2f, 1.5f));
            Spawn(
                balconyFloorPrefab,
                "RaisedDeckFloor",
                new Vector3(0f, 1.2f, -9f),
                Vector3.zero,
                new Vector3(2.2f, 1f, 1.5f));
            Spawn(
                stairsRampPrefab,
                "RaisedDeckRamp",
                new Vector3(0f, 0f, -6.85f),
                Vector3.zero,
                new Vector3(1.5f, 1.2f, 1.5f));
            Spawn(
                railPrefab,
                "RaisedDeckRail_Left",
                new Vector3(-2.15f, 1.2f, -9f),
                new Vector3(0f, 90f, 0f),
                new Vector3(1.5f, 1f, 1f));
            Spawn(
                railPrefab,
                "RaisedDeckRail_Right",
                new Vector3(2.15f, 1.2f, -9f),
                new Vector3(0f, 90f, 0f),
                new Vector3(1.5f, 1f, 1f));
            AddGameplayBox(
                "RaisedDeck_Collider",
                new Vector3(0f, 0.6f, -9f),
                Vector3.zero,
                new Vector3(4.4f, 1.2f, 3f));
            AddGameplayBox(
                "RaisedDeckRamp_Collider",
                new Vector3(0f, 0.35f, -6.85f),
                new Vector3(20f, 0f, 0f),
                new Vector3(2.5f, 0.25f, 2.5f));
        }

        private void BuildSetDressing()
        {
            Spawn(
                computerPrefab,
                "ControlStation_West",
                new Vector3(-14f, 0f, 7f),
                new Vector3(0f, 90f, 0f),
                Vector3.one);
            Spawn(
                computerPrefab,
                "ControlStation_East",
                new Vector3(14f, 0f, -7f),
                new Vector3(0f, -90f, 0f),
                Vector3.one);
        }

        private void Spawn(
            GameObject prefab,
            string instanceName,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(prefab, visualRoot);
            if (instance == null)
            {
                return;
            }

            instance.name = instanceName;
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = localPosition;
            instanceTransform.localRotation = Quaternion.Euler(localEulerAngles);
            instanceTransform.localScale = localScale;
            Collider[] importedColliders = instance.GetComponentsInChildren<Collider>();
            for (int index = 0; index < importedColliders.Length; index++)
            {
                importedColliders[index].enabled = false;
            }

            createdVisualCount++;
        }

        private void AddGameplayBox(
            string objectName,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            GameObject colliderObject = new GameObject(objectName);
            Transform colliderTransform = colliderObject.transform;
            colliderTransform.SetParent(visualRoot, false);
            colliderTransform.localPosition = localPosition;
            colliderTransform.localRotation = Quaternion.Euler(localEulerAngles);
            colliderTransform.localScale = localScale;
            colliderObject.AddComponent<BoxCollider>();
        }

        private void SetBlockoutRenderersVisible(bool visible)
        {
            SetChildRenderersVisible("Ground", visible);
            SetChildRenderersVisible("Walls", visible);
            SetChildRenderersVisible("Cover", visible);
        }

        private void SetChildRenderersVisible(string childName, bool visible)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                return;
            }

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].enabled = visible;
            }
        }

        private void ApplyLighting()
        {
            Light[] lights = FindObjectsOfType<Light>();
            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index].type != LightType.Directional)
                {
                    continue;
                }

                lights[index].color = directionalLightColor;
                lights[index].intensity = directionalLightIntensity;
                break;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.34f, 0.42f, 1f);
        }
    }
}
