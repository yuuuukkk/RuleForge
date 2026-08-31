using System;
using System.Collections.Generic;
using System.Globalization;
using RuleForge.Rules;
using RuleForge.Player;
using RuleForge.Runtime.Goals;
using RuleForge.Runtime.Stats;
using RuleForge.Weapons;
using UnityEngine;

namespace RuleForge.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private RuleEngine ruleEngine;
        [SerializeField] private ChallengeGoalController goalController;
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField, Min(0.5f)] private float feedbackDuration = 2.5f;
        [SerializeField, Min(0.05f)] private float damageFlashDuration = 0.2f;

        [Header("Placeholder Art")]
        [SerializeField] private Texture2D reticleTexture;
        [SerializeField] private Texture2D objectiveIconTexture;
        [SerializeField] private Texture2D healthIconTexture;
        [SerializeField] private Texture2D ammoIconTexture;
        [SerializeField] private Texture2D panelTexture;
        [SerializeField, Min(8f)] private float iconSize = 20f;
        [SerializeField, Min(8f)] private float reticleSize = 32f;

        private readonly List<string> feedbackLines = new List<string>();
        private float feedbackExpiresAt;
        private float damageFlashExpiresAt;
        private string damageSource = string.Empty;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle resultStyle;
        private GUIStyle damageStyle;

        private void OnEnable()
        {
            ResolveReferences();
            if (ruleEngine != null)
            {
                ruleEngine.RuleTriggered -= HandleRuleTriggered;
                ruleEngine.RuleTriggered += HandleRuleTriggered;
            }

            GameplayEventBus.EventPublished -= HandleGameplayEvent;
            GameplayEventBus.EventPublished += HandleGameplayEvent;
        }

        private void OnDisable()
        {
            if (ruleEngine != null)
            {
                ruleEngine.RuleTriggered -= HandleRuleTriggered;
            }

            GameplayEventBus.EventPublished -= HandleGameplayEvent;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawDamageFeedback();
            DrawArt();
            DrawGoalAndWeapon();
            DrawRuleFeedback();
            DrawResult();
        }

        public void Configure(
            RuleEngine engine,
            ChallengeGoalController goals,
            WeaponLoadout loadout,
            WeaponController controller)
        {
            ruleEngine = engine;
            goalController = goals;
            weaponLoadout = loadout;
            weaponController = controller;
        }

        public void ConfigureArt(
            Texture2D reticle,
            Texture2D objectiveIcon,
            Texture2D healthIcon,
            Texture2D ammoIcon,
            Texture2D panel)
        {
            reticleTexture = reticle;
            objectiveIconTexture = objectiveIcon;
            healthIconTexture = healthIcon;
            ammoIconTexture = ammoIcon;
            panelTexture = panel;
        }

        private void DrawArt()
        {
            if (reticleTexture != null)
            {
                DrawTextureCentered(reticleTexture, reticleSize);
            }
        }

        private void DrawGoalAndWeapon()
        {
            if (goalController != null)
            {
                DrawPanel(new Rect(16f, 16f, 310f, 88f));
                DrawIcon(objectiveIconTexture, 28f, 22f);
                GUI.Label(
                    new Rect(54f, 22f, 260f, 24f),
                    goalController.BuildProgressText(),
                    titleStyle);
                DrawIcon(healthIconTexture, 28f, 72f);
                GUI.Label(
                    new Rect(54f, 48f, 260f, 22f),
                    "武器：" + TranslateDataValue(
                        goalController.ActiveWeaponName) +
                    "   [1/2/3 切换]",
                    bodyStyle);
                GUI.Label(
                    new Rect(54f, 72f, 260f, 22f),
                    BuildHealthText(),
                    bodyStyle);
            }

            if (weaponController != null)
            {
                DrawPanel(new Rect(Screen.width - 230f, 16f, 214f, 64f));
                DrawIcon(ammoIconTexture, Screen.width - 218f, 48f);
                GUI.Label(
                    new Rect(Screen.width - 190f, 22f, 162f, 24f),
                    weaponLoadout != null
                        ? TranslateDataValue(weaponLoadout.CurrentWeaponName)
                        : "武器",
                    titleStyle);
                GUI.Label(
                    new Rect(Screen.width - 190f, 48f, 162f, 22f),
                    $"弹药 {weaponController.CurrentMagazineAmmo} / " +
                    weaponController.ReserveAmmo,
                    bodyStyle);
            }
        }

        private void DrawRuleFeedback()
        {
            if (feedbackLines.Count == 0 ||
                Time.unscaledTime > feedbackExpiresAt)
            {
                return;
            }

            float width = Mathf.Min(440f, Screen.width - 40f);
            float height = 54f + feedbackLines.Count * 24f;
            float left = (Screen.width - width) * 0.5f;
            DrawPanel(new Rect(left, 100f, width, height));
            GUI.Label(
                new Rect(left + 12f, 108f, width - 24f, 28f),
                "规则已触发",
                resultStyle);
            for (int index = 0; index < feedbackLines.Count; index++)
            {
                GUI.Label(
                    new Rect(
                        left + 18f,
                        138f + index * 24f,
                        width - 36f,
                        22f),
                    feedbackLines[index],
                    titleStyle);
            }
        }

        private void DrawResult()
        {
            if (goalController == null ||
                (goalController.State != ChallengeGoalState.Victory &&
                 goalController.State != ChallengeGoalState.Defeat))
            {
                return;
            }

            string text = goalController.State == ChallengeGoalState.Victory
                ? "胜利"
                : "失败";
            GUI.Label(
                new Rect(0f, Screen.height * 0.38f, Screen.width, 70f),
                text,
                resultStyle);
        }

        private void HandleRuleTriggered(RuleTriggerFeedback feedback)
        {
            feedbackLines.Clear();
            if (feedback == null)
            {
                return;
            }

            IReadOnlyList<RuleEffectFeedback> effects = feedback.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                feedbackLines.Add(FormatEffect(effects[index]));
            }

            feedbackExpiresAt = Time.unscaledTime + feedbackDuration;
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent.Type != GameplayEventType.PlayerHit)
            {
                return;
            }

            damageSource = gameplayEvent.Instigator != null
                ? TranslateEnemyName(gameplayEvent.Instigator.name)
                : "敌人";
            damageFlashExpiresAt = Time.unscaledTime + damageFlashDuration;
        }

        private void DrawDamageFeedback()
        {
            if (damageFlashExpiresAt <= 0f ||
                Time.unscaledTime >= damageFlashExpiresAt)
            {
                return;
            }

            float progress = damageFlashDuration > 0f
                ? Mathf.Clamp01(
                    (damageFlashExpiresAt - Time.unscaledTime) /
                    damageFlashDuration)
                : 0f;
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.05f, 0.05f, 0.16f * progress);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(
                new Rect(0f, Screen.height * 0.68f, Screen.width, 38f),
                "受到 " + damageSource + " 的伤害",
                damageStyle);
        }

        private string BuildHealthText()
        {
            string health =
                $"生命 {goalController.PlayerCurrentHealth:0} / " +
                $"{goalController.PlayerMaxHealth:0}";
            if (playerHealth != null &&
                playerHealth.DamageProtectionRemaining > 0f)
            {
                health += $"   保护 {playerHealth.DamageProtectionRemaining:0.0}秒";
            }

            return health;
        }

        private static string FormatEffect(RuleEffectFeedback effect)
        {
            string value;
            if (string.Equals(
                    effect.Kind,
                    RuleEffectKind.SpawnEnemy.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                value = "生成" + TranslateDataValue(effect.StringValue);
            }
            else if (string.Equals(
                         effect.Kind,
                         RuleEffectKind.GiveAmmo.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                value = "+" + FormatNumber(effect.Value) + " 弹药";
            }
            else if (string.Equals(
                         effect.Operation,
                         StatModifierOperation.AddPercent.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                value = Signed(effect.Value * 100f) + "%";
            }
            else if (string.Equals(
                         effect.Operation,
                         StatModifierOperation.Multiply.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                value = "x" + FormatNumber(effect.Value);
            }
            else
            {
                value = Signed(effect.Value);
            }

            string stack = effect.MaxStacks > 1
                ? $"  层数 {effect.AppliedStacks}/{effect.MaxStacks}"
                : string.Empty;
            return RuleForgeLocalization.EffectName(
                       effect.EffectId,
                       effect.DisplayName,
                       RuleForgeLanguage.Chinese) +
                   " " + value + stack;
        }

        private static string TranslateDataValue(string value)
        {
            return RuleForgeLocalization.DataValue(
                value,
                RuleForgeLanguage.Chinese);
        }

        private static string TranslateEnemyName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "敌人";
            }

            const string prefix = "Enemy [";
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                value.EndsWith("]", StringComparison.Ordinal))
            {
                string enemyType = value.Substring(
                    prefix.Length,
                    value.Length - prefix.Length - 1);
                return TranslateDataValue(enemyType);
            }

            return value;
        }

        private static string Signed(float value)
        {
            return (value >= 0f ? "+" : string.Empty) + FormatNumber(value);
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void DrawIcon(Texture2D texture, float left, float top)
        {
            if (texture == null)
            {
                return;
            }

            GUI.DrawTexture(
                new Rect(left, top, iconSize, iconSize),
                texture,
                ScaleMode.ScaleToFit,
                true);
        }

        private void DrawPanel(Rect rect)
        {
            if (panelTexture == null)
            {
                GUI.Box(rect, string.Empty);
                return;
            }

            GUI.DrawTexture(rect, panelTexture, ScaleMode.StretchToFill, true);
        }

        private static void DrawTextureCentered(Texture2D texture, float size)
        {
            GUI.DrawTexture(
                new Rect(
                    (Screen.width - size) * 0.5f,
                    (Screen.height - size) * 0.5f,
                    size,
                    size),
                texture,
                ScaleMode.ScaleToFit,
                true);
        }

        private void ResolveReferences()
        {
            ruleEngine = ruleEngine != null
                ? ruleEngine
                : FindObjectOfType<RuleEngine>();
            goalController = goalController != null
                ? goalController
                : FindObjectOfType<ChallengeGoalController>();
            weaponLoadout = weaponLoadout != null
                ? weaponLoadout
                : FindObjectOfType<WeaponLoadout>();
            weaponController = weaponController != null
                ? weaponController
                : FindObjectOfType<WeaponController>();
            playerHealth = playerHealth != null
                ? playerHealth
                : FindObjectOfType<PlayerHealth>();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            resultStyle.normal.textColor = Color.yellow;
            damageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            damageStyle.normal.textColor = Color.white;
        }
    }
}
