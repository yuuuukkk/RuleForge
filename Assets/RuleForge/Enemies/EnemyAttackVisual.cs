using UnityEngine;

namespace RuleForge.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackVisual : MonoBehaviour
    {
        private const int RingSegments = 40;
        private static readonly Color WindupStartColor =
            new Color(1f, 0.72f, 0.08f, 0.9f);
        private static readonly Color WindupEndColor =
            new Color(1f, 0.08f, 0.03f, 1f);

        private LineRenderer ring;
        private TextMesh warningText;
        private Material ringMaterial;
        private float windupStartedAt;
        private float windupEndsAt;
        private float impactEndsAt;
        private Camera viewCamera;

        private void Awake()
        {
            EnsureVisuals();
            SetVisible(false);
        }

        private void Update()
        {
            if (ring == null || warningText == null)
            {
                return;
            }

            float now = Time.time;
            if (now < windupEndsAt)
            {
                float duration = Mathf.Max(0.01f, windupEndsAt - windupStartedAt);
                float progress = Mathf.Clamp01((now - windupStartedAt) / duration);
                Color color = Color.Lerp(WindupStartColor, WindupEndColor, progress);
                SetVisible(true);
                SetColor(color);
                float pulse = 0.86f + progress * 0.38f +
                              Mathf.Sin(progress * Mathf.PI * 6f) * 0.05f;
                ring.transform.localScale = Vector3.one * pulse;
                warningText.transform.localPosition =
                    new Vector3(0f, 2.35f + Mathf.Sin(progress * Mathf.PI) * 0.2f, 0f);
                return;
            }

            if (now < impactEndsAt)
            {
                float progress = 1f - Mathf.Clamp01(
                    (impactEndsAt - now) / 0.16f);
                SetVisible(true);
                SetColor(new Color(1f, 0.04f, 0.02f, 1f - progress));
                ring.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.65f, progress);
                warningText.text = "";
                return;
            }

            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (warningText == null || !warningText.gameObject.activeSelf)
            {
                return;
            }

            viewCamera = viewCamera != null ? viewCamera : Camera.main;
            if (viewCamera == null)
            {
                return;
            }

            Vector3 awayFromCamera =
                warningText.transform.position - viewCamera.transform.position;
            if (awayFromCamera.sqrMagnitude > 0.001f)
            {
                warningText.transform.rotation = Quaternion.LookRotation(awayFromCamera);
            }
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
            {
                Destroy(ringMaterial);
            }
        }

        public void BeginWindup(float duration)
        {
            EnsureVisuals();
            windupStartedAt = Time.time;
            windupEndsAt = windupStartedAt + Mathf.Max(0.05f, duration);
            impactEndsAt = 0f;
            warningText.text = "!";
            SetVisible(true);
        }

        public void ShowImpact()
        {
            EnsureVisuals();
            windupEndsAt = 0f;
            impactEndsAt = Time.time + 0.16f;
            warningText.text = string.Empty;
            SetVisible(true);
        }

        public void Cancel()
        {
            windupEndsAt = 0f;
            impactEndsAt = 0f;
            SetVisible(false);
        }

        private void EnsureVisuals()
        {
            if (ring != null && warningText != null)
            {
                return;
            }

            GameObject ringObject = new GameObject("AttackWarningRing");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.startWidth = 0.07f;
            ring.endWidth = 0.07f;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                ringMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.DontSave
                };
                ring.material = ringMaterial;
            }

            for (int index = 0; index < RingSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / RingSegments;
                ring.SetPosition(index, new Vector3(
                    Mathf.Cos(angle) * 1.05f,
                    0f,
                    Mathf.Sin(angle) * 1.05f));
            }

            GameObject textObject = new GameObject("AttackWarningText");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            warningText = textObject.AddComponent<TextMesh>();
            warningText.text = "!";
            warningText.anchor = TextAnchor.MiddleCenter;
            warningText.alignment = TextAlignment.Center;
            warningText.fontSize = 96;
            warningText.characterSize = 0.055f;
            warningText.fontStyle = FontStyle.Bold;
            warningText.GetComponent<MeshRenderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void SetColor(Color color)
        {
            ring.startColor = color;
            ring.endColor = color;
            warningText.color = color;
        }

        private void SetVisible(bool visible)
        {
            if (ring != null)
            {
                ring.gameObject.SetActive(visible);
            }

            if (warningText != null)
            {
                warningText.gameObject.SetActive(visible);
            }
        }
    }
}
