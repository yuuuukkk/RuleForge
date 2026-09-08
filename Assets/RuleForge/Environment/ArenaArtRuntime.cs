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
        [SerializeField, Min(0f)] private float directionalLightIntensity = 1.05f;
        [SerializeField] private Color playerAccentColor =
            new Color(0.05f, 0.75f, 1f, 1f);
        [SerializeField] private Color dangerAccentColor =
            new Color(1f, 0.18f, 0.08f, 1f);
        [SerializeField] private Color ambientColor =
            new Color(0.2f, 0.25f, 0.34f, 1f);

        private Transform visualRoot;
        private int createdVisualCount;
        private Material playerAccentMaterial;
        private Material dangerAccentMaterial;

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
            BuildVisualIdentity();
        }

        private void BuildFloor()
        {
            // The imported Space Station tile is roughly one metre wide while
            // the gameplay grid uses two-metre centres. Slightly overlap the
            // visuals so the arena reads as one continuous floor instead of a
            // field of disconnected stepping stones. Gameplay collision still
            // comes from the original graybox ground.
            Vector3 floorScale = new Vector3(2.02f, 1f, 2.02f);
            for (int x = -7; x <= 7; x++)
            {
                for (int z = -7; z <= 7; z++)
                {
                    GameObject tile = Spawn(
                        floorPrefab,
                        "Floor_" + x + "_" + z,
                        new Vector3(x * 2f, 0.01f, z * 2f),
                        Vector3.zero,
                        floorScale);
                    bool isCentralLane = Mathf.Abs(x) <= 1;
                    bool alternate = ((x + z) & 1) == 0;
                    Color tint = isCentralLane
                        ? new Color(0.42f, 0.52f, 0.68f, 1f)
                        : alternate
                            ? new Color(0.62f, 0.7f, 0.82f, 1f)
                            : new Color(0.54f, 0.63f, 0.76f, 1f);
                    TintVisual(tile, tint);
                }
            }
        }

        private void BuildPerimeter()
        {
            Vector3 wallScale = new Vector3(2.02f, 2.35f, 1f);
            for (int index = -7; index <= 7; index++)
            {
                float offset = index * 2f;
                Spawn(
                    wallPrefab,
                    "NorthWall_" + index,
                    new Vector3(offset, 0f, 15.35f),
                    Vector3.zero,
                    wallScale);
                Spawn(
                    wallPrefab,
                    "SouthWall_" + index,
                    new Vector3(-offset, 0f, -15.35f),
                    new Vector3(0f, 180f, 0f),
                    wallScale);
                Spawn(
                    wallPrefab,
                    "EastWall_" + index,
                    new Vector3(15.35f, 0f, -offset),
                    new Vector3(0f, 90f, 0f),
                    wallScale);
                Spawn(
                    wallPrefab,
                    "WestWall_" + index,
                    new Vector3(-15.35f, 0f, offset),
                    new Vector3(0f, -90f, 0f),
                    wallScale);
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

            for (int index = -6; index <= 6; index += 3)
            {
                float offset = index * 2f;
                Spawn(wallPillarPrefab, "NorthRhythm_" + index,
                    new Vector3(offset, 0f, 15.05f), Vector3.zero, Vector3.one);
                Spawn(wallPillarPrefab, "SouthRhythm_" + index,
                    new Vector3(offset, 0f, -15.05f),
                    new Vector3(0f, 180f, 0f), Vector3.one);
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

            Spawn(
                containerPrefab,
                "FlankCover_SouthWest",
                new Vector3(-9f, 0f, -5.5f),
                new Vector3(0f, 35f, 0f),
                new Vector3(1.15f, 1.15f, 1.15f));
            AddGameplayBox(
                "FlankCover_SouthWest_Collider",
                new Vector3(-9f, 0.7f, -5.5f),
                new Vector3(0f, 35f, 0f),
                new Vector3(1.45f, 1.4f, 1.45f));
            Spawn(
                containerPrefab,
                "FlankCover_NorthEast",
                new Vector3(9f, 0f, 5.5f),
                new Vector3(0f, -35f, 0f),
                new Vector3(1.15f, 1.15f, 1.15f));
            AddGameplayBox(
                "FlankCover_NorthEast_Collider",
                new Vector3(9f, 0.7f, 5.5f),
                new Vector3(0f, -35f, 0f),
                new Vector3(1.45f, 1.4f, 1.45f));
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

        private void BuildVisualIdentity()
        {
            playerAccentMaterial = CreateAccentMaterial(playerAccentColor);
            dangerAccentMaterial = CreateAccentMaterial(dangerAccentColor);

            CreateRing(
                "PlayerStartRing",
                Vector3.up * 0.045f,
                3.25f,
                playerAccentColor,
                playerAccentMaterial);
            CreateLaneStrip(
                "WestLaneGuide",
                new Vector3(-7.5f, 0.05f, -11f),
                new Vector3(-7.5f, 0.05f, 11f),
                playerAccentColor,
                playerAccentMaterial);
            CreateLaneStrip(
                "EastLaneGuide",
                new Vector3(7.5f, 0.05f, -11f),
                new Vector3(7.5f, 0.05f, 11f),
                dangerAccentColor,
                dangerAccentMaterial);

            Spawn(
                structurePrefab,
                "RuleForgeCommandFrame",
                new Vector3(0f, 0f, 13.7f),
                new Vector3(0f, 180f, 0f),
                new Vector3(2.6f, 1.55f, 0.8f));
            Spawn(
                computerPrefab,
                "RuleForgeCommandConsole",
                new Vector3(0f, 0f, 12.1f),
                new Vector3(0f, 180f, 0f),
                new Vector3(1.35f, 1.35f, 1.35f));
            CreateArenaTitle();
            CreateBeacon("BlueBeacon", new Vector3(-13.7f, 2.2f, 0f),
                playerAccentColor);
            CreateBeacon("RedBeacon", new Vector3(13.7f, 2.2f, 0f),
                dangerAccentColor);
        }

        private void CreateRing(
            string objectName,
            Vector3 center,
            float radius,
            Color color,
            Material material)
        {
            GameObject ringObject = new GameObject(objectName);
            ringObject.transform.SetParent(visualRoot, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            ring.startWidth = 0.055f;
            ring.endWidth = 0.055f;
            ring.material = material;
            ring.startColor = color;
            ring.endColor = color;
            for (int index = 0; index < ring.positionCount; index++)
            {
                float angle = index / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(index, center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius));
            }
        }

        private void CreateLaneStrip(
            string objectName,
            Vector3 start,
            Vector3 end,
            Color color,
            Material material)
        {
            GameObject stripObject = new GameObject(objectName);
            stripObject.transform.SetParent(visualRoot, false);
            LineRenderer strip = stripObject.AddComponent<LineRenderer>();
            strip.useWorldSpace = false;
            strip.positionCount = 2;
            strip.startWidth = 0.075f;
            strip.endWidth = 0.025f;
            strip.material = material;
            strip.startColor = color;
            strip.endColor = new Color(color.r, color.g, color.b, 0.25f);
            strip.SetPosition(0, start);
            strip.SetPosition(1, end);
        }

        private void CreateArenaTitle()
        {
            GameObject titleObject = new GameObject("ArenaTitle");
            titleObject.transform.SetParent(visualRoot, false);
            titleObject.transform.localPosition = new Vector3(0f, 2.65f, 14.72f);
            titleObject.transform.localRotation = Quaternion.identity;
            TextMesh title = titleObject.AddComponent<TextMesh>();
            title.text = "RULEFORGE // AI COMBAT LAB";
            title.anchor = TextAnchor.MiddleCenter;
            title.alignment = TextAlignment.Center;
            title.fontSize = 48;
            title.characterSize = 0.15f;
            title.color = playerAccentColor;
        }

        private static void TintVisual(GameObject target, Color tint)
        {
            if (target == null)
            {
                return;
            }

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", tint);
                properties.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
        }

        private void CreateBeacon(string objectName, Vector3 position, Color color)
        {
            GameObject beaconObject = new GameObject(objectName);
            beaconObject.transform.SetParent(visualRoot, false);
            beaconObject.transform.localPosition = position;
            Light beacon = beaconObject.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.color = color;
            beacon.intensity = 2.1f;
            beacon.range = 7f;
            beacon.shadows = LightShadows.None;
        }

        private static Material CreateAccentMaterial(Color color)
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

            Material material = new Material(shader);
            material.color = color;
            material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * 1.6f);
                material.EnableKeyword("_EMISSION");
            }
            return material;
        }

        private GameObject Spawn(
            GameObject prefab,
            string instanceName,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(prefab, visualRoot);
            if (instance == null)
            {
                return null;
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
            return instance;
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
                lights[index].shadows = LightShadows.Soft;
                break;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.075f, 0.1f, 0.16f, 1f);
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 45f;
        }

        private void OnDestroy()
        {
            if (playerAccentMaterial != null)
            {
                Destroy(playerAccentMaterial);
            }
            if (dangerAccentMaterial != null)
            {
                Destroy(dangerAccentMaterial);
            }
        }
    }
}
