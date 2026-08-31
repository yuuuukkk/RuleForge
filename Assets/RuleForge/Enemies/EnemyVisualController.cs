using System;
using UnityEngine;

namespace RuleForge.Enemies
{
    [Serializable]
    public struct EnemyVisualSkin
    {
        public string enemyType;
        public Texture2D texture;
    }

    [DisallowMultipleComponent]
    public sealed class EnemyVisualController : MonoBehaviour
    {
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private EnemyVisualSkin[] skins =
            Array.Empty<EnemyVisualSkin>();
        [SerializeField] private Vector3 modelLocalPosition;
        [SerializeField] private Vector3 modelLocalEulerAngles;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;

        [NonSerialized] private MaterialPropertyBlock properties;
        private GameObject activeModel;

        public GameObject ModelPrefab => modelPrefab;
        public EnemyVisualSkin[] Skins => skins;

        public void Configure(
            GameObject prefab,
            EnemyVisualSkin[] skinDefinitions,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            modelPrefab = prefab;
            skins = skinDefinitions ?? Array.Empty<EnemyVisualSkin>();
            modelLocalPosition = localPosition;
            modelLocalEulerAngles = localEulerAngles;
            modelLocalScale = new Vector3(
                Mathf.Max(0.001f, localScale.x),
                Mathf.Max(0.001f, localScale.y),
                Mathf.Max(0.001f, localScale.z));
        }

        public void Apply(string enemyType, Color tint)
        {
            if (modelPrefab == null)
            {
                SetFallbackRenderersVisible(true);
                return;
            }

            EnsureModel();
            if (activeModel == null)
            {
                SetFallbackRenderersVisible(true);
                return;
            }

            SetFallbackRenderersVisible(false);
            if (properties == null)
            {
                properties = new MaterialPropertyBlock();
            }
            Texture2D skin = FindSkin(enemyType);
            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                renderer.GetPropertyBlock(properties);
                if (skin != null)
                {
                    properties.SetTexture("_MainTex", skin);
                    properties.SetTexture("_BaseMap", skin);
                }

                properties.SetColor("_Color", tint);
                properties.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(properties);
            }
        }

        private void EnsureModel()
        {
            if (activeModel != null)
            {
                return;
            }

            UnityEngine.Object instance = Instantiate(
                (UnityEngine.Object)modelPrefab,
                transform);
            activeModel = ResolveModelInstance(instance);
            if (activeModel == null)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }

                Debug.LogError(
                    "Enemy art asset did not instantiate as a GameObject or Component: " +
                    modelPrefab.name,
                    this);
                return;
            }

            activeModel.name = "ArtModel";
            activeModel.transform.localPosition = modelLocalPosition;
            activeModel.transform.localRotation = Quaternion.Euler(
                modelLocalEulerAngles);
            activeModel.transform.localScale = modelLocalScale;
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

        private void SetFallbackRenderersVisible(bool visible)
        {
            Renderer[] renderers = GetComponents<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].enabled = visible;
            }
        }

        private Texture2D FindSkin(string enemyType)
        {
            for (int index = 0; index < skins.Length; index++)
            {
                EnemyVisualSkin skin = skins[index];
                if (skin.texture != null &&
                    string.Equals(
                        skin.enemyType,
                        enemyType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return skin.texture;
                }
            }

            return skins.Length > 0 ? skins[0].texture : null;
        }
    }
}
