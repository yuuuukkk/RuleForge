using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RuleForge.DSL;
using RuleForge.Enemies;
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
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ChallengeCreatorPanel creatorPanel;
        [SerializeField, Min(0.5f)] private float feedbackDuration = 2.5f;
        [SerializeField, Min(0.05f)] private float damageFlashDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float damageIndicatorDuration = 0.85f;

        [Header("Placeholder Art")]
        [SerializeField] private Texture2D reticleTexture;
        [SerializeField] private Texture2D objectiveIconTexture;
        [SerializeField] private Texture2D healthIconTexture;
        [SerializeField] private Texture2D healthBarBackgroundTexture;
        [SerializeField] private Texture2D healthBarFillTexture;
        [SerializeField] private Texture2D ammoIconTexture;
        [SerializeField] private Texture2D panelTexture;
        [SerializeField] private Texture2D buttonTexture;
        [SerializeField] private Texture2D buttonHoverTexture;
        [SerializeField] private Texture2D dangerButtonTexture;
        [SerializeField] private Font uiFont;
        [SerializeField, Min(8f)] private float iconSize = 20f;
        [SerializeField, Min(8f)] private float reticleSize = 32f;

        private readonly List<string> feedbackLines = new List<string>();
        private readonly List<HitFeedbackEntry> hitFeedbackEntries =
            new List<HitFeedbackEntry>();
        private float feedbackExpiresAt;
        private float hitMarkerExpiresAt;
        private bool hitMarkerIsHeadshot;
        private float damageFlashExpiresAt;
        private float damageIndicatorExpiresAt;
        private float damageDirectionAngle;
        private bool hasDamageDirection;
        private string damageSource = string.Empty;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle resultStyle;
        private GUIStyle damageStyle;
        private GUIStyle damageArrowStyle;
        private GUIStyle damageNumberStyle;
        private GUIStyle headshotStyle;
        private GUIStyle menuTitleStyle;
        private GUIStyle menuBodyStyle;
        private GUIStyle resultDetailsStyle;
        private GUIStyle dangerButtonStyle;
        private GUIStyle healthBarLabelStyle;
        private bool showStartMenu = true;
        private bool showEndMenu;
        private bool showPauseMenu;
        private bool showSettingsMenu;
        private bool showResultRules;
        private int resultRulesPage;
        private FlowMenuReturn settingsReturn = FlowMenuReturn.Main;
        private readonly DisplaySettingsController displaySettings =
            new DisplaySettingsController();
        private bool challengeStartRequested;
        private bool creatorOpenedFromFlowMenu;
        private ChallengeGoalState endState = ChallengeGoalState.Inactive;
        private int ruleTriggerCount;
        private readonly List<RuleRunSummary> ruleRunSummaries =
            new List<RuleRunSummary>();
        private int hitCount;
        private int headshotCount;
        private float totalDamageDealt;
        private float lastIncomingDamage;
        private string defeatCause = string.Empty;
        private GameplayAudioFeedback audioFeedback;

        private void OnEnable()
        {
            ResolveReferences();
            audioFeedback = audioFeedback != null
                ? audioFeedback
                : GetComponent<GameplayAudioFeedback>();
            if (audioFeedback == null)
            {
                audioFeedback = gameObject.AddComponent<GameplayAudioFeedback>();
            }

            audioFeedback.Configure(ruleEngine, goalController);
            RuleForgeGuiTheme.Configure(
                panelTexture,
                buttonTexture,
                buttonHoverTexture,
                dangerButtonTexture,
                uiFont);
            if (ruleEngine != null)
            {
                ruleEngine.RuleTriggered -= HandleRuleTriggered;
                ruleEngine.RuleTriggered += HandleRuleTriggered;
                ruleEngine.ChallengeRestarted -= HandleChallengeRestarted;
                ruleEngine.ChallengeRestarted += HandleChallengeRestarted;
            }

            if (goalController != null)
            {
                goalController.StateChanged -= HandleGoalStateChanged;
                goalController.StateChanged += HandleGoalStateChanged;
            }

            GameplayEventBus.EventPublished -= HandleGameplayEvent;
            GameplayEventBus.EventPublished += HandleGameplayEvent;
            showStartMenu = true;
            showEndMenu = false;
            showPauseMenu = false;
            showSettingsMenu = false;
            displaySettings.Refresh();
            RuntimeInputGate.SetBlocked(this, true);
        }

        private void OnDisable()
        {
            if (ruleEngine != null)
            {
                ruleEngine.RuleTriggered -= HandleRuleTriggered;
                ruleEngine.ChallengeRestarted -= HandleChallengeRestarted;
            }

            if (goalController != null)
            {
                goalController.StateChanged -= HandleGoalStateChanged;
            }

            GameplayEventBus.EventPublished -= HandleGameplayEvent;
            RuntimeInputGate.SetBlocked(this, false);
        }

        private void Start()
        {
            if (showStartMenu)
            {
                RuntimeInputGate.SetBlocked(this, true);
            }
        }

        private void Update()
        {
            if (creatorOpenedFromFlowMenu &&
                (creatorPanel == null || !creatorPanel.IsOpen))
            {
                creatorOpenedFromFlowMenu = false;
            }

            if (!Input.GetKeyDown(KeyCode.Escape) ||
                RuntimePanelCoordinator.HasActivePanel)
            {
                return;
            }

            if (showSettingsMenu)
            {
                ReturnFromSettings();
            }
            else if (showPauseMenu)
            {
                ResumeGameplay();
            }
            else if (!showStartMenu && !showEndMenu)
            {
                OpenPauseMenu();
            }
        }

        private void OnGUI()
        {
            GUISkin previousSkin = RuleForgeGuiTheme.Begin();
            EnsureStyles();
            bool creatorIsCoveringMenu = creatorPanel != null &&
                                         creatorPanel.IsOpen;
            if (showStartMenu || showEndMenu ||
                showPauseMenu || showSettingsMenu)
            {
                if (!creatorIsCoveringMenu)
                {
                    DrawFlowMenu();
                }

                RuleForgeGuiTheme.End(previousSkin);
                return;
            }

            DrawDamageFeedback();
            DrawArt();
            DrawHitFeedback();
            DrawGoalAndWeapon();
            DrawRuleFeedback();
            DrawRuntimeRuleState();
            DrawResult();
            RuleForgeGuiTheme.End(previousSkin);
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
            Texture2D healthBarBackground,
            Texture2D healthBarFill,
            Texture2D ammoIcon,
            Texture2D panel)
        {
            reticleTexture = reticle;
            objectiveIconTexture = objectiveIcon;
            healthIconTexture = healthIcon;
            healthBarBackgroundTexture = healthBarBackground;
            healthBarFillTexture = healthBarFill;
            ammoIconTexture = ammoIcon;
            panelTexture = panel;
        }

        public void ConfigureThemeArt(
            Texture2D button,
            Texture2D buttonHover,
            Texture2D dangerButton,
            Font font)
        {
            buttonTexture = button;
            buttonHoverTexture = buttonHover;
            dangerButtonTexture = dangerButton;
            uiFont = font;
            RuleForgeGuiTheme.Configure(
                panelTexture,
                buttonTexture,
                buttonHoverTexture,
                dangerButtonTexture,
                uiFont);
        }

        private void DrawFlowMenu()
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.015f, 0.035f, 0.07f, 0.88f);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = previousColor;

            float width = Mathf.Min(600f, Screen.width - 48f);
            float height = showSettingsMenu ? 500f : 540f;
            Rect panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            DrawPanel(panelRect);

            GUI.Label(
                new Rect(panelRect.x + 32f, panelRect.y + 34f,
                    panelRect.width - 64f, 58f),
                "RULEFORGE",
                menuTitleStyle);

            if (showSettingsMenu)
            {
                DrawSettingsMenu(panelRect);
            }
            else if (showStartMenu)
            {
                GUI.Label(
                    new Rect(panelRect.x + 44f, panelRect.y + 96f,
                        panelRect.width - 88f, 72f),
                    "用自然语言创建玩法，然后立即进入战斗。\n" +
                    "你可以先编辑挑战，也可以直接开始当前挑战。",
                    menuBodyStyle);
                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 185f,
                            panelRect.width - 144f, 54f),
                        "开始游戏",
                        GUI.skin.button))
                {
                    StartOrRestartChallenge();
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 251f,
                            panelRect.width - 144f, 54f),
                        "编辑玩法",
                        GUI.skin.button))
                {
                    OpenCreatorFromFlowMenu();
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 317f,
                            panelRect.width - 144f, 54f),
                        "显示设置",
                        GUI.skin.button))
                {
                    OpenSettings(FlowMenuReturn.Main);
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 397f,
                            panelRect.width - 144f, 48f),
                        "退出游戏",
                        dangerButtonStyle))
                {
                    Application.Quit();
                }
            }
            else if (showPauseMenu)
            {
                GUI.Label(
                    new Rect(panelRect.x + 44f, panelRect.y + 104f,
                        panelRect.width - 88f, 48f),
                    "游戏已暂停",
                    resultStyle);
                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 175f,
                            panelRect.width - 144f, 52f),
                        "继续游戏",
                        GUI.skin.button))
                {
                    ResumeGameplay();
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 239f,
                            panelRect.width - 144f, 52f),
                        "编辑玩法",
                        GUI.skin.button))
                {
                    OpenCreatorFromFlowMenu();
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 303f,
                            panelRect.width - 144f, 52f),
                        "显示设置",
                        GUI.skin.button))
                {
                    OpenSettings(FlowMenuReturn.Pause);
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 383f,
                            panelRect.width - 144f, 48f),
                        "返回主菜单",
                        dangerButtonStyle))
                {
                    ReturnToMainMenu();
                }
            }
            else
            {
                bool endlessTrial = goalController != null &&
                    goalController.GoalType == ChallengeGoalType.TimeBankEndless;
                string resultTitle = endState == ChallengeGoalState.Victory
                    ? "挑战完成"
                    : endlessTrial ? "试炼结束" : "挑战失败";
                string resultBody = endState == ChallengeGoalState.Victory
                    ? "你已完成当前玩法。\n" + BuildResultSummary()
                    : endlessTrial
                        ? "时间生命耗尽。\n" + BuildResultSummary()
                        : "你已被击败。\n" + BuildResultSummary();
                GUI.Label(
                    new Rect(panelRect.x + 44f, panelRect.y + 100f,
                        panelRect.width - 210f, 42f),
                    resultTitle,
                    resultStyle);
                if (GUI.Button(
                        new Rect(panelRect.x + panelRect.width - 160f,
                            panelRect.y + 104f, 116f, 32f),
                        showResultRules ? "返回概览" : "规则统计",
                        GUI.skin.button))
                {
                    showResultRules = !showResultRules;
                    resultRulesPage = 0;
                }
                if (showResultRules)
                {
                    DrawResultRuleStats(panelRect);
                }
                else
                {
                    GUI.Label(
                        new Rect(panelRect.x + 44f, panelRect.y + 143f,
                            panelRect.width - 88f, 176f),
                        resultBody,
                        resultDetailsStyle);
                }
                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 326f,
                            panelRect.width - 144f, 52f),
                        "重新开始",
                        GUI.skin.button))
                {
                    StartOrRestartChallenge();
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 386f,
                            panelRect.width - 144f, 48f),
                        "编辑玩法",
                        dangerButtonStyle))
                {
                    OpenCreatorFromFlowMenu();
                }

                if (GUI.Button(
                        new Rect(panelRect.x + 72f, panelRect.y + 446f,
                            panelRect.width - 144f, 44f),
                        "返回主菜单",
                        GUI.skin.button))
                {
                    ReturnToMainMenu();
                }
            }
        }

        private void DrawSettingsMenu(Rect panelRect)
        {
            if (displaySettings.Draw(
                    panelRect,
                    resultStyle,
                    menuBodyStyle,
                    dangerButtonStyle))
            {
                ReturnFromSettings();
            }
        }

        private void OpenCreatorFromFlowMenu()
        {
            ResolveReferences();
            if (creatorPanel == null)
            {
                return;
            }

            creatorOpenedFromFlowMenu = true;
            creatorPanel.OpenCreator();
        }

        private void StartOrRestartChallenge()
        {
            challengeStartRequested = true;
            showStartMenu = false;
            showEndMenu = false;
            showPauseMenu = false;
            showSettingsMenu = false;
            if (ruleEngine != null)
            {
                ruleEngine.RestartChallenge();
            }
            else
            {
                RuntimeInputGate.SetBlocked(this, false);
            }
        }

        private void HandleGoalStateChanged(ChallengeGoalState state)
        {
            if (state != ChallengeGoalState.Victory &&
                state != ChallengeGoalState.Defeat)
            {
                return;
            }

            endState = state;
            showResultRules = false;
            resultRulesPage = 0;
            defeatCause = state == ChallengeGoalState.Defeat
                ? goalController != null && goalController.LastDefeatWasTimeout
                    ? goalController.UsesTimeAsHealth
                        ? "时间生命归零"
                        : "倒计时归零，未在限定时间内完成目标"
                    : lastIncomingDamage > 0f
                    ? $"最后一击：{damageSource}，造成 {lastIncomingDamage:0.#} 点伤害"
                    : "死亡原因：未记录到直接伤害"
                : string.Empty;
            showStartMenu = false;
            showEndMenu = true;
            showPauseMenu = false;
            showSettingsMenu = false;
            RuntimeInputGate.SetBlocked(this, true);
        }

        private void HandleChallengeRestarted(RuleForge.DSL.ChallengeSpec challenge)
        {
            bool keepInitialMenu = showStartMenu &&
                                   !challengeStartRequested &&
                                   !creatorOpenedFromFlowMenu;
            endState = ChallengeGoalState.Inactive;
            ruleTriggerCount = 0;
            ruleRunSummaries.Clear();
            showResultRules = false;
            resultRulesPage = 0;
            hitCount = 0;
            headshotCount = 0;
            totalDamageDealt = 0f;
            lastIncomingDamage = 0f;
            defeatCause = string.Empty;
            damageSource = string.Empty;
            hitFeedbackEntries.Clear();
            hitMarkerExpiresAt = 0f;
            if (keepInitialMenu)
            {
                RuntimeInputGate.SetBlocked(this, true);
                return;
            }

            challengeStartRequested = false;
            creatorOpenedFromFlowMenu = false;
            showStartMenu = false;
            showEndMenu = false;
            showPauseMenu = false;
            showSettingsMenu = false;
            RuntimeInputGate.SetBlocked(this, false);
        }

        private void OpenPauseMenu()
        {
            showStartMenu = false;
            showEndMenu = false;
            showSettingsMenu = false;
            showPauseMenu = true;
            RuntimeInputGate.SetBlocked(this, true);
        }

        private void ResumeGameplay()
        {
            showPauseMenu = false;
            showSettingsMenu = false;
            RuntimeInputGate.SetBlocked(this, false);
        }

        private void ReturnToMainMenu()
        {
            showStartMenu = true;
            showEndMenu = false;
            showPauseMenu = false;
            showSettingsMenu = false;
            challengeStartRequested = false;
            creatorOpenedFromFlowMenu = false;
            RuntimeInputGate.SetBlocked(this, true);
        }

        private void OpenSettings(FlowMenuReturn returnTarget)
        {
            settingsReturn = returnTarget;
            showStartMenu = false;
            showEndMenu = false;
            showPauseMenu = false;
            showSettingsMenu = true;
            displaySettings.Refresh();
            RuntimeInputGate.SetBlocked(this, true);
        }

        private void ReturnFromSettings()
        {
            showSettingsMenu = false;
            showStartMenu = settingsReturn == FlowMenuReturn.Main;
            showPauseMenu = settingsReturn == FlowMenuReturn.Pause;
            showEndMenu = settingsReturn == FlowMenuReturn.Result;
            RuntimeInputGate.SetBlocked(this, true);
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
                string progress = goalController.BuildProgressText();
                if (goalController.TimeLimit > 0f &&
                    goalController.GoalType != ChallengeGoalType.Survive &&
                    !goalController.UsesTimeAsHealth)
                {
                    int separator = progress.IndexOf("  ·  ", StringComparison.Ordinal);
                    if (separator >= 0)
                    {
                        progress = progress.Substring(0, separator);
                    }
                }
                GUI.Label(
                    new Rect(54f, 22f, 260f, 24f),
                    progress,
                    titleStyle);
                if (goalController.TimeLimit > 0f &&
                    goalController.GoalType != ChallengeGoalType.Survive &&
                    !goalController.UsesTimeAsHealth)
                {
                    GUI.Label(new Rect(54f, 46f, 246f, 18f),
                        $"剩余 {goalController.RemainingSeconds:0.0} 秒",
                        bodyStyle);
                }
                DrawIcon(healthIconTexture, 28f, 62f);
                DrawHealthBar(new Rect(54f, 65f, 246f, 18f));
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
            int visibleCount = Mathf.Min(2, feedbackLines.Count);
            float height = 40f + visibleCount * 22f +
                           (feedbackLines.Count > visibleCount ? 18f : 0f);
            float left = (Screen.width - width) * 0.5f;
            float top = 130f;
            DrawPanel(new Rect(left, top, width, height));
            GUI.Label(
                new Rect(left + 12f, top + 6f, width - 24f, 28f),
                "规则已触发",
                titleStyle);
            for (int index = 0; index < visibleCount; index++)
            {
                GUI.Label(
                    new Rect(
                        left + 18f,
                        top + 34f + index * 22f,
                        width - 36f,
                        20f),
                    feedbackLines[index],
                    bodyStyle);
            }
            if (feedbackLines.Count > visibleCount)
            {
                GUI.Label(new Rect(left + 18f,
                        top + 34f + visibleCount * 22f,
                        width - 36f, 18f),
                    $"另有 {feedbackLines.Count - visibleCount} 项效果",
                    bodyStyle);
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

            ruleTriggerCount++;
            RuleRunSummary runSummary = null;
            for (int index = 0; index < ruleRunSummaries.Count; index++)
            {
                if (string.Equals(ruleRunSummaries[index].RuleId,
                        feedback.RuleId, StringComparison.Ordinal))
                {
                    runSummary = ruleRunSummaries[index];
                    break;
                }
            }
            if (runSummary == null)
            {
                runSummary = new RuleRunSummary(feedback.RuleId);
                ruleRunSummaries.Add(runSummary);
            }
            runSummary.TriggerCount++;
            runSummary.Description = BuildRuleDescription(feedback);

            IReadOnlyList<RuleEffectFeedback> effects = feedback.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                feedbackLines.Add(FormatEffect(effects[index]));
            }

            feedbackExpiresAt = Time.unscaledTime + feedbackDuration;
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent.Type == GameplayEventType.EnemyHit)
            {
                RegisterEnemyHit(gameplayEvent);
                return;
            }

            if (gameplayEvent.Type == GameplayEventType.Headshot)
            {
                RegisterHeadshot(gameplayEvent);
                return;
            }

            if (gameplayEvent.Type != GameplayEventType.PlayerHit)
            {
                return;
            }

            damageSource = gameplayEvent.Instigator != null
                ? TranslateEnemyName(gameplayEvent.Instigator.name)
                : "未知来源";
            lastIncomingDamage = Mathf.Max(0f, gameplayEvent.Value);
            damageFlashExpiresAt = Time.unscaledTime + damageFlashDuration;
            damageIndicatorExpiresAt =
                Time.unscaledTime + damageIndicatorDuration;
            hasDamageDirection = gameplayEvent.Instigator != null &&
                                 playerHealth != null;
            if (hasDamageDirection)
            {
                Vector3 direction = gameplayEvent.Instigator.transform.position -
                                    playerHealth.transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Vector3 localDirection = playerHealth.transform
                        .InverseTransformDirection(direction.normalized);
                    damageDirectionAngle = Mathf.Atan2(
                        localDirection.x,
                        localDirection.z) * Mathf.Rad2Deg;
                }
                else
                {
                    hasDamageDirection = false;
                }
            }
        }

        private void RegisterEnemyHit(GameplayEvent gameplayEvent)
        {
            float now = Time.unscaledTime;
            HitFeedbackEntry entry = FindRecentHit(
                gameplayEvent.Source,
                now,
                0.08f);
            if (entry == null)
            {
                Vector3 position = gameplayEvent.Source != null
                    ? gameplayEvent.Source.transform.position + Vector3.up * 1.8f
                    : Vector3.zero;
                entry = new HitFeedbackEntry(
                    gameplayEvent.Source,
                    position,
                    now);
                hitFeedbackEntries.Add(entry);
            }

            entry.Damage += Mathf.Max(0f, gameplayEvent.Value);
            entry.ExpiresAt = now + 0.75f;
            hitCount++;
            totalDamageDealt += Mathf.Max(0f, gameplayEvent.Value);
            if (now >= hitMarkerExpiresAt)
            {
                hitMarkerIsHeadshot = false;
            }

            hitMarkerExpiresAt = Mathf.Max(hitMarkerExpiresAt, now + 0.14f);
        }

        private void RegisterHeadshot(GameplayEvent gameplayEvent)
        {
            float now = Time.unscaledTime;
            HitFeedbackEntry entry = FindRecentHit(
                gameplayEvent.Source,
                now,
                0.12f);
            if (entry != null)
            {
                entry.IsHeadshot = true;
                entry.ExpiresAt = Mathf.Max(entry.ExpiresAt, now + 0.9f);
            }

            headshotCount++;
            hitMarkerExpiresAt = now + 0.22f;
            hitMarkerIsHeadshot = true;
        }

        private HitFeedbackEntry FindRecentHit(
            GameObject target,
            float now,
            float maximumAge)
        {
            for (int index = hitFeedbackEntries.Count - 1; index >= 0; index--)
            {
                HitFeedbackEntry entry = hitFeedbackEntries[index];
                if (entry.Target == target && now - entry.CreatedAt <= maximumAge)
                {
                    return entry;
                }
            }

            return null;
        }

        private void DrawHitFeedback()
        {
            DrawHitMarker();
            Camera viewCamera = Camera.main;
            float now = Time.unscaledTime;
            for (int index = hitFeedbackEntries.Count - 1; index >= 0; index--)
            {
                HitFeedbackEntry entry = hitFeedbackEntries[index];
                if (now >= entry.ExpiresAt)
                {
                    hitFeedbackEntries.RemoveAt(index);
                    continue;
                }

                if (viewCamera == null)
                {
                    continue;
                }

                Vector3 worldPosition = entry.Target != null
                    ? entry.Target.transform.position + Vector3.up * 1.8f
                    : entry.WorldPosition;
                Vector3 screenPoint = viewCamera.WorldToScreenPoint(worldPosition);
                if (screenPoint.z <= 0f)
                {
                    continue;
                }

                float remaining = Mathf.Clamp01(
                    (entry.ExpiresAt - now) / 0.75f);
                float rise = (1f - remaining) * 34f;
                GUIStyle style = entry.IsHeadshot
                    ? headshotStyle
                    : damageNumberStyle;
                string label = entry.IsHeadshot
                    ? $"爆头  {entry.Damage:0}"
                    : entry.Damage.ToString("0", CultureInfo.InvariantCulture);
                GUI.Label(
                    new Rect(
                        screenPoint.x - 70f,
                        Screen.height - screenPoint.y - 24f - rise,
                        140f,
                        36f),
                    label,
                    style);
            }
        }

        private void DrawHitMarker()
        {
            if (Time.unscaledTime >= hitMarkerExpiresAt)
            {
                return;
            }

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            Color previousColor = GUI.color;
            GUI.color = hitMarkerIsHeadshot
                ? new Color(1f, 0.75f, 0.08f, 1f)
                : new Color(0.88f, 1f, 1f, 1f);
            GUI.DrawTexture(new Rect(centerX - 18f, centerY - 1f, 11f, 2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX + 7f, centerY - 1f, 11f, 2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - 1f, centerY - 18f, 2f, 11f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - 1f, centerY + 7f, 2f, 11f),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawRuntimeRuleState()
        {
            if (ruleEngine == null || ruleEngine.ExecutionStates == null)
            {
                return;
            }

            List<string> stateLines = new List<string>();
            if (enemySpawner != null)
            {
                string phase = enemySpawner.IsPreparing
                    ? "准备阶段"
                    : enemySpawner.PressureTier <= 0
                        ? "基础交战"
                        : enemySpawner.PressureTier == 1
                            ? "压力上升"
                            : "最终攻势";
                stateLines.Add(
                    $"战斗阶段：{phase}   " +
                    $"进度 {enemySpawner.ChallengeProgress * 100f:0}%");
            }
            IReadOnlyList<RuleExecutionDebugState> states =
                ruleEngine.ExecutionStates;
            for (int index = 0; index < states.Count; index++)
            {
                RuleExecutionDebugState state = states[index];
                if (state == null || state.AppliedStacks <= 0)
                {
                    continue;
                }

                RuleEffect effect = FindActiveEffect(
                    state.RuleId,
                    state.EffectId);
                if (effect == null || !string.Equals(
                        effect.Kind,
                        RuleEffectKind.StatModifier.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string displayName = state.EffectId;
                if (ruleEngine.TryGetEffectDefinition(
                        state.EffectId,
                        out RuleForge.Config.EffectDefinition definition))
                {
                    string polarity = definition.Polarity ==
                                      RuleForge.Config.EffectPolarity.Reward
                        ? "收益"
                        : definition.Polarity ==
                          RuleForge.Config.EffectPolarity.Penalty
                            ? "风险"
                            : "状态";
                    displayName = RuleForgeLocalization.EffectName(
                        definition.EffectId,
                        definition.DisplayName,
                        RuleForgeLanguage.Chinese);
                    displayName = polarity + " · " + displayName;
                }

                string line = displayName + " " +
                              FormatRuntimeEffectValue(
                                  effect,
                                  state.LastAppliedValue);
                if (state.MaxStacks > 1)
                {
                    line += $"   层数 {state.AppliedStacks}/{state.MaxStacks}";
                }

                stateLines.Add(line);
                if (stateLines.Count >= 3)
                {
                    break;
                }
            }

            if (weaponController != null)
            {
                stateLines.Insert(
                    0,
                    "当前武器伤害 " +
                    weaponController.Damage.ToString(
                        "0.#",
                        CultureInfo.InvariantCulture));
            }

            float height = 34f + stateLines.Count * 19f;
            float top = Screen.height - height - 18f;
            DrawPanel(new Rect(16f, top, 310f, height));
            GUI.Label(new Rect(30f, top + 5f, 280f, 22f),
                "当前规则状态", titleStyle);
            for (int index = 0; index < stateLines.Count; index++)
            {
                GUI.Label(
                    new Rect(30f, top + 28f + index * 19f, 280f, 18f),
                    stateLines[index],
                    bodyStyle);
            }
        }

        private RuleEffect FindActiveEffect(string ruleId, string effectId)
        {
            RuleForge.DSL.ChallengeSpec challenge = ruleEngine.ActiveChallenge;
            if (challenge == null)
            {
                return null;
            }

            RuleForge.DSL.GameplayRule[] rules = challenge.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                RuleForge.DSL.GameplayRule rule = rules[ruleIndex];
                if (rule == null || !string.Equals(
                        rule.Id,
                        ruleId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                RuleEffect[] effects = rule.Effects;
                for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                {
                    if (effects[effectIndex] != null && string.Equals(
                            effects[effectIndex].EffectId,
                            effectId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return effects[effectIndex];
                    }
                }
            }

            return null;
        }

        private static string FormatRuntimeEffectValue(
            RuleEffect effect,
            float value)
        {
            if (string.Equals(
                    effect.Operation,
                    StatModifierOperation.AddPercent.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Signed(value * 100f) + "%";
            }

            if (string.Equals(
                    effect.Operation,
                    StatModifierOperation.Multiply.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return "x" + FormatNumber(value);
            }

            return Signed(value);
        }

        private string BuildResultSummary()
        {
            string progress = goalController != null
                ? goalController.BuildProgressText()
                : "目标数据不可用";
            StringBuilder summary = new StringBuilder(240);
            if (endState == ChallengeGoalState.Defeat)
            {
                summary.AppendLine(defeatCause);
            }
            summary.AppendLine(progress);
            if (goalController != null && goalController.UsesTimeAsHealth)
            {
                summary.Append("本局坚持 ")
                    .Append(goalController.SurvivedSeconds.ToString(
                        "0.0", CultureInfo.InvariantCulture))
                    .Append(" 秒；剩余时间 ")
                    .Append(goalController.RemainingSeconds.ToString(
                        "0.0", CultureInfo.InvariantCulture))
                    .AppendLine(" 秒");
            }
            summary.Append("命中 ").Append(hitCount)
                .Append("   爆头 ").Append(headshotCount)
                .Append("   造成伤害 ")
                .AppendLine(totalDamageDealt.ToString("0", CultureInfo.InvariantCulture));
            summary.Append("规则触发 ").Append(ruleTriggerCount).Append(" 次");

            List<RuleRunSummary> ordered = GetOrderedRuleSummaries();
            int visible = Mathf.Min(2, ordered.Count);
            for (int index = 0; index < visible; index++)
            {
                summary.Append('\n')
                    .Append(ordered[index].Description)
                    .Append("：")
                    .Append(ordered[index].TriggerCount)
                    .Append(" 次");
            }
            if (ordered.Count > visible)
            {
                summary.Append("\n其余 ")
                    .Append(ordered.Count - visible)
                    .Append(" 条规则请点“规则统计”");
            }

            return summary.ToString();
        }

        private void DrawResultRuleStats(Rect panelRect)
        {
            List<RuleRunSummary> ordered = GetOrderedRuleSummaries();
            Rect content = new Rect(panelRect.x + 44f,
                panelRect.y + 148f, panelRect.width - 88f, 155f);
            if (ordered.Count == 0)
            {
                GUI.Label(content, "本局没有规则被触发。", resultDetailsStyle);
                return;
            }

            int pageCount = Mathf.CeilToInt(ordered.Count / 3f);
            resultRulesPage = Mathf.Clamp(resultRulesPage, 0, pageCount - 1);
            int first = resultRulesPage * 3;
            for (int offset = 0; offset < 3 && first + offset < ordered.Count;
                 offset++)
            {
                RuleRunSummary run = ordered[first + offset];
                GUI.Label(new Rect(content.x, content.y + offset * 39f,
                        content.width, 38f),
                    $"{first + offset + 1}. {run.Description}  ·  触发 {run.TriggerCount} 次",
                    resultDetailsStyle);
            }

            if (pageCount > 1)
            {
                GUI.enabled = resultRulesPage > 0;
                if (GUI.Button(new Rect(content.x, content.y + 122f,
                        84f, 28f), "上一页"))
                {
                    resultRulesPage--;
                }
                GUI.enabled = resultRulesPage < pageCount - 1;
                if (GUI.Button(new Rect(content.x + content.width - 84f,
                        content.y + 122f, 84f, 28f), "下一页"))
                {
                    resultRulesPage++;
                }
                GUI.enabled = true;
            }
        }

        private List<RuleRunSummary> GetOrderedRuleSummaries()
        {
            List<RuleRunSummary> ordered =
                new List<RuleRunSummary>(ruleRunSummaries);
            ordered.Sort((left, right) =>
                right.TriggerCount.CompareTo(left.TriggerCount));
            return ordered;
        }

        private static string BuildRuleDescription(RuleTriggerFeedback feedback)
        {
            IReadOnlyList<RuleEffectFeedback> effects = feedback.Effects;
            if (effects.Count == 0)
            {
                return string.IsNullOrWhiteSpace(feedback.RuleId)
                    ? "未命名规则"
                    : feedback.RuleId;
            }

            StringBuilder description = new StringBuilder();
            int visible = Mathf.Min(2, effects.Count);
            for (int index = 0; index < visible; index++)
            {
                if (index > 0)
                {
                    description.Append("、");
                }
                description.Append(RuleForgeLocalization.EffectName(
                    effects[index].EffectId,
                    effects[index].DisplayName,
                    RuleForgeLanguage.Chinese));
            }
            if (effects.Count > visible)
            {
                description.Append("等");
            }
            return description.ToString();
        }

        private void DrawDamageFeedback()
        {
            bool flashVisible = damageFlashExpiresAt > 0f &&
                                Time.unscaledTime < damageFlashExpiresAt;
            bool indicatorVisible = damageIndicatorExpiresAt > 0f &&
                                    Time.unscaledTime < damageIndicatorExpiresAt;
            if (!flashVisible && !indicatorVisible)
            {
                return;
            }

            if (flashVisible)
            {
                float progress = damageFlashDuration > 0f
                ? Mathf.Clamp01(
                    (damageFlashExpiresAt - Time.unscaledTime) /
                    damageFlashDuration)
                : 0f;
                Color previousColor = GUI.color;
                GUI.color = new Color(1f, 0.02f, 0.02f, 0.08f * progress);
                GUI.DrawTexture(
                    new Rect(0f, 0f, Screen.width, Screen.height),
                    Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.01f, 0.01f, 0.42f * progress);
                float edge = Mathf.Max(30f, Mathf.Min(Screen.width, Screen.height) * 0.07f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, edge), Texture2D.whiteTexture);
                GUI.DrawTexture(
                    new Rect(0f, Screen.height - edge, Screen.width, edge),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, 0f, edge, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(
                    new Rect(Screen.width - edge, 0f, edge, Screen.height),
                    Texture2D.whiteTexture);
                GUI.color = previousColor;
            }

            if (indicatorVisible && hasDamageDirection)
            {
                Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                float radius = Mathf.Min(220f, Mathf.Min(Screen.width, Screen.height) * 0.32f);
                float radians = damageDirectionAngle * Mathf.Deg2Rad;
                Vector2 markerCenter = center + new Vector2(
                    Mathf.Sin(radians),
                    -Mathf.Cos(radians)) * radius;
                Rect markerRect = new Rect(
                    markerCenter.x - 28f,
                    markerCenter.y - 28f,
                    56f,
                    56f);
                Matrix4x4 previousMatrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(damageDirectionAngle, markerCenter);
                GUI.Label(markerRect, "▲", damageArrowStyle);
                GUI.matrix = previousMatrix;
            }

            string hitMessage = goalController != null &&
                goalController.UsesTimeAsHealth
                    ? $"{damageSource} 命中 · 时间 -{lastIncomingDamage * goalController.TimeDamageScale:0.#}秒"
                    : "受到 " + damageSource + " 的伤害";
            GUI.Label(
                new Rect(0f, Screen.height * 0.68f, Screen.width, 38f),
                hitMessage,
                damageStyle);
        }

        private string BuildHealthText()
        {
            string health = goalController.UsesTimeAsHealth
                ? $"时间生命 {goalController.RemainingSeconds:0.0}秒"
                : $"生命 {goalController.PlayerCurrentHealth:0} / " +
                  $"{goalController.PlayerMaxHealth:0}";
            if (playerHealth != null &&
                playerHealth.DamageProtectionRemaining > 0f)
            {
                health += $"   保护 {playerHealth.DamageProtectionRemaining:0.0}秒";
            }

            return health;
        }

        private void DrawHealthBar(Rect rect)
        {
            float maximum = goalController != null
                ? goalController.UsesTimeAsHealth
                    ? goalController.GoalType == ChallengeGoalType.TimeBankTarget
                        ? goalController.GoalTarget
                        : goalController.TimeLimit
                    : goalController.PlayerMaxHealth
                : 0f;
            float current = goalController != null
                ? goalController.UsesTimeAsHealth
                    ? goalController.RemainingSeconds
                    : goalController.PlayerCurrentHealth
                : 0f;
            float ratio = maximum > 0f
                ? Mathf.Clamp01(current / maximum)
                : 0f;

            if (healthBarBackgroundTexture != null)
            {
                GUI.DrawTexture(
                    rect,
                    healthBarBackgroundTexture,
                    ScaleMode.StretchToFill,
                    true);
            }
            else
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = previous;
            }

            if (ratio > 0f)
            {
                Rect clippedRect = new Rect(
                    rect.x,
                    rect.y,
                    rect.width * ratio,
                    rect.height);
                GUI.BeginGroup(clippedRect);
                if (healthBarFillTexture != null)
                {
                    GUI.DrawTexture(
                        new Rect(0f, 0f, rect.width, rect.height),
                        healthBarFillTexture,
                        ScaleMode.StretchToFill,
                        true);
                }
                else
                {
                    Color previous = GUI.color;
                    GUI.color = new Color(0.95f, 0.13f, 0.24f, 1f);
                    GUI.DrawTexture(
                        new Rect(0f, 0f, rect.width, rect.height),
                        Texture2D.whiteTexture);
                    GUI.color = previous;
                }
                GUI.EndGroup();
            }

            GUI.Label(rect, BuildHealthText(), healthBarLabelStyle);
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
                         effect.Kind,
                         RuleEffectKind.AddTime.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                value = "+" + FormatNumber(effect.Value) + " 秒";
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
            Color previous = GUI.color;
            GUI.color = new Color(0.015f, 0.035f, 0.06f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            if (panelTexture != null)
            {
                GUI.color = new Color(0.12f, 0.6f, 0.72f, 0.2f);
                GUI.DrawTexture(
                    rect,
                    panelTexture,
                    ScaleMode.StretchToFill,
                    true);
            }
            GUI.color = previous;
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
            creatorPanel = creatorPanel != null
                ? creatorPanel
                : FindObjectOfType<ChallengeCreatorPanel>();
            enemySpawner = enemySpawner != null
                ? enemySpawner
                : FindObjectOfType<EnemySpawner>();
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
            damageArrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            damageArrowStyle.normal.textColor = new Color(1f, 0.12f, 0.06f, 1f);
            damageNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            damageNumberStyle.normal.textColor = Color.white;
            headshotStyle = new GUIStyle(damageNumberStyle);
            headshotStyle.normal.textColor = new Color(1f, 0.76f, 0.08f, 1f);
            menuTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (uiFont != null)
            {
                menuTitleStyle.font = uiFont;
            }
            menuTitleStyle.normal.textColor = new Color(0.3f, 0.9f, 1f, 1f);
            menuBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            menuBodyStyle.normal.textColor = Color.white;
            resultDetailsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            resultDetailsStyle.normal.textColor = Color.white;
            dangerButtonStyle = RuleForgeGuiTheme.CreateDangerButtonStyle();
            healthBarLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            healthBarLabelStyle.normal.textColor = Color.white;
        }

        private enum FlowMenuReturn
        {
            Main,
            Pause,
            Result
        }

        private sealed class RuleRunSummary
        {
            public RuleRunSummary(string ruleId)
            {
                RuleId = ruleId ?? string.Empty;
                Description = "未命名规则";
            }

            public string RuleId { get; }
            public string Description { get; set; }
            public int TriggerCount { get; set; }
        }

        private sealed class HitFeedbackEntry
        {
            public HitFeedbackEntry(
                GameObject target,
                Vector3 worldPosition,
                float createdAt)
            {
                Target = target;
                WorldPosition = worldPosition;
                CreatedAt = createdAt;
            }

            public GameObject Target { get; }
            public Vector3 WorldPosition { get; }
            public float CreatedAt { get; }
            public float Damage { get; set; }
            public float ExpiresAt { get; set; }
            public bool IsHeadshot { get; set; }
        }
    }
}
