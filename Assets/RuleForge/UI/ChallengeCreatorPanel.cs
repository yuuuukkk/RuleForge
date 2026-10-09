using System;
using System.Collections.Generic;
using System.Globalization;
using RuleForge.AI;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Rules;
using RuleForge.Runtime.Stats;
using RuleForge.Validation;
using UnityEngine;
using RuleForge.Weapons;

namespace RuleForge.UI
{
    [DisallowMultipleComponent]
    public sealed class ChallengeCreatorPanel : MonoBehaviour
    {
        [SerializeField] private RuleEngine ruleEngine;
        [SerializeField] private AIGameplayController aiController;
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField] private KeyCode toggleKey = KeyCode.F2;
        [SerializeField, TextArea(2, 5)]
        private string diagnosticSourcePrompt = string.Empty;

        private readonly List<RuleDraft> ruleDrafts = new List<RuleDraft>();
        private Vector2 scrollPosition;
        private bool isOpen;
        private string challengeId = "manual_challenge";
        private string challengeName = string.Empty;
        private string goal = string.Empty;
        private string goalTargetText = string.Empty;
        private string timeLimitText = string.Empty;
        private string timeDamageScaleText = string.Empty;
        private string weapon = string.Empty;
        private float rewardStrength = 1f;
        private float penaltyStrength = 1f;
        private string statusMessage = string.Empty;
        private string validatedDraftSignature = string.Empty;
        private string openDropdownId = string.Empty;
        private int nextRuleNumber = 1;
        private string[] weaponOptions = Array.Empty<string>();
        private string designerPrompt = string.Empty;
        private string lastGeneratedPrompt = string.Empty;
        private string lastModificationPrompt = string.Empty;
        private readonly AIProviderSetupView aiProviderSetupView =
            new AIProviderSetupView();
        private GameplayProposal gameplayProposal;
        private string gameplayProposalPrompt = string.Empty;
        private GameplayModificationProposal modificationProposal;
        private string modificationProposalPrompt = string.Empty;
        private string modificationRequestSignature = string.Empty;
        private string improvementRequestSignature = string.Empty;
        private string patchRequestSignature = string.Empty;
        private GameplayImprovementSet improvementSet;
        private AIChallengePreview pendingGenerationRepair;
        private bool showAdvancedEdit;
        private bool showDeveloperView;
        private bool showAISetup;
        private string lastAIDiagnostic = string.Empty;
        private bool showAIDiagnostic;
        private bool showProposalRevision;
        private ChallengeSpec pendingModification;
        private AIChallengePreview pendingModificationPreview;
        private ChallengeSpec modificationBase;
        private readonly List<string> pendingModificationDiff =
            new List<string>();
        private ValidationResult currentDraftValidation;
        private BalanceEvaluation currentDraftBalance;
        private bool hasGeneratedDraftThisSession;
        private bool hasUnplayedPreview;
        private float proposalPenaltyRewardRatio = 1f;
        private GUIStyle creatorTitleStyle;
        private GUIStyle creatorSubtitleStyle;
        private GUIStyle creatorSectionStyle;
        private GUIStyle creatorMutedStyle;
        private GUIStyle creatorPitchStyle;
        private GUIStyle creatorWarningStyle;
        private GUIStyle creatorSecondaryButtonStyle;
        private GUIStyle creatorSuggestionStyle;
        private GUIStyle creatorPlayButtonStyle;
        private GUIStyle creatorCardStyle;
        private GUIStyle creatorStepStyle;

        private static readonly string[] CreationExampleLabelsEnglish =
        {
            "High Risk",
            "Low HP Build",
            "Reload Gamble",
            "Survival Rush"
        };
        private static readonly string[] CreationExampleLabelsChinese =
        {
            "高风险高收益",
            "残血强化",
            "换弹赌博",
            "极限生存"
        };
        private static readonly string[] CreationExamplesChinese =
        {
            "每次击杀都会提高我的伤害，但敌人的速度增长得更快。",
            "我的血量越低，伤害越高，但移动速度也会降低。",
            "每次换弹有概率生成一个快速敌人，杀掉它会返还弹药。",
            "生存 60 秒；杀敌增加伤害；敌人同时变快；惩罚是奖励的 1.5 倍。"
        };
        private static readonly string[] CreationExamplesEnglish =
        {
            "Every kill increases my damage, but enemy speed grows even faster.",
            "The lower my health, the higher my damage, but my movement speed also drops.",
            "Every reload has a chance to spawn a fast enemy; killing it refunds ammunition.",
            "Survive for 60 seconds. Kills increase damage while enemies get faster; the penalty is 1.5 times the reward."
        };
        private static readonly string[] TriggerOptions =
            GameplayEventCapabilities.GetRuntimeEventNames();
        private static readonly string[] GoalOptions =
            Enum.GetNames(typeof(ChallengeGoalType));
        private static readonly string[] ConditionOptions =
            Enum.GetNames(typeof(RuleConditionType));
        private static readonly string[] StackModeOptions =
            Enum.GetNames(typeof(RuleStackMode));
        private static readonly string[] ScalingSourceOptions =
            Enum.GetNames(typeof(RuntimeValueSource));
        private static readonly string[] ComparisonOptions =
            Enum.GetNames(typeof(RuleComparison));
        private static readonly string[] TextComparisonOptions =
        {
            RuleComparison.Equals.ToString(),
            RuleComparison.NotEquals.ToString()
        };
        private static readonly string[] EnemyTypeOptions =
        {
            "Grunt",
            "Runner",
            "Tank"
        };

        public bool IsOpen => isOpen;

        public void ConfigureDiagnosticSourcePrompt(string prompt)
        {
            diagnosticSourcePrompt = prompt ?? string.Empty;
        }

        private void Awake()
        {
            ResolveRuleEngine();
            RefreshWeaponOptions();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetOpen(!isOpen);
            }
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                RuntimeInputGate.SetBlocked(this, false);
                RuntimePanelCoordinator.Close(this);
                isOpen = false;
            }
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            GUISkin previousSkin = RuleForgeGuiTheme.Begin();
            EnsureCreatorStyles();
            Color previousColor = GUI.color;
            GUI.color = new Color(0.005f, 0.012f, 0.025f, 0.72f);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
            float width = Mathf.Max(1f,
                Mathf.Min(820f, Screen.width - 32f));
            float height = Mathf.Max(1f,
                Mathf.Min(520f, Screen.height - 32f));
            Rect panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            GUILayout.BeginArea(panelRect, GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("RULEFORGE", creatorTitleStyle);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI GAMEPLAY DESIGNER",
                "AI 玩法设计器"), creatorSubtitleStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(showAISetup
                    ? RuleForgeLocalization.T("Back", "返回创建")
                    : CanUseRealAI()
                        ? RuleForgeLocalization.T("AI Settings", "AI 设置")
                        : RuleForgeLocalization.T("Set Up AI", "配置 AI"),
                    creatorSecondaryButtonStyle,
                    GUILayout.Width(100f), GUILayout.Height(32f)))
            {
                showAISetup = !showAISetup;
                scrollPosition = Vector2.zero;
                GUI.FocusControl(null);
            }
            if (GUILayout.Button(RuleForgeLocalization.ToggleLabel,
                    creatorSecondaryButtonStyle,
                    GUILayout.Width(86f), GUILayout.Height(32f)))
            {
                RuleForgeLocalization.Toggle();
                statusMessage = string.Empty;
                if (pendingModification != null)
                {
                    BuildChallengeDiff(
                        modificationBase,
                        pendingModification,
                        pendingModificationDiff);
                }
            }
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Close",
                    "关闭"), creatorSecondaryButtonStyle,
                    GUILayout.Width(86f), GUILayout.Height(32f)))
            {
                SetOpen(false);
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                RuleForgeGuiTheme.End(previousSkin);
                return;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
            scrollPosition.x = 0f;
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                false,
                true,
                GUIStyle.none,
                GUI.skin.verticalScrollbar);
            if (showAISetup)
            {
                aiProviderSetupView.Draw(aiController);
            }
            else
            {
                DrawCreator();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            RuleForgeGuiTheme.End(previousSkin);
        }

        public void Configure(RuleEngine engine)
        {
            ruleEngine = engine;
        }

        public void ConfigureAI(AIGameplayController controller)
        {
            aiController = controller;
        }

        public void OpenCreator()
        {
            SetOpen(true);
        }

        private void SetOpen(bool shouldOpen)
        {
            if (shouldOpen)
            {
                RuntimePanelCoordinator.Open(this, () => SetOpen(false));
                ResolveRuleEngine();
                aiController?.RefreshProviderSelection();
                if (!hasUnplayedPreview)
                {
                    RebuildFromActiveChallenge();
                }
                RuntimeInputGate.SetBlocked(this, true);
            }
            else
            {
                openDropdownId = string.Empty;
                RuntimeInputGate.SetBlocked(this, false);
                RuntimePanelCoordinator.Close(this);
            }

            isOpen = shouldOpen;
        }

        private void ResolveRuleEngine()
        {
            if (ruleEngine == null)
            {
                ruleEngine = FindObjectOfType<RuleEngine>();
            }

            if (aiController == null)
            {
                aiController = FindObjectOfType<AIGameplayController>();
            }

            if (weaponLoadout == null)
            {
                weaponLoadout = FindObjectOfType<WeaponLoadout>();
            }
        }

        private void RefreshWeaponOptions()
        {
            if (weaponLoadout == null)
            {
                weaponOptions = Array.Empty<string>();
                return;
            }

            List<string> names = new List<string>();
            for (int index = 0; index < weaponLoadout.WeaponCount; index++)
            {
                WeaponConfig config = weaponLoadout.GetWeapon(index);
                if (config != null && !string.IsNullOrWhiteSpace(config.DisplayName))
                {
                    names.Add(config.DisplayName);
                }
            }

            weaponOptions = names.ToArray();
            if (weaponOptions.Length > 0 &&
                string.IsNullOrWhiteSpace(weapon) &&
                !string.IsNullOrWhiteSpace(weaponOptions[0]))
            {
                weapon = weaponOptions[0];
            }
        }

        private void RebuildFromActiveChallenge()
        {
            hasUnplayedPreview = false;
            ruleDrafts.Clear();
            ClearPendingModification();
            statusMessage = string.Empty;
            validatedDraftSignature = string.Empty;
            currentDraftValidation = null;
            currentDraftBalance = null;
            openDropdownId = string.Empty;
            nextRuleNumber = 1;
            if (ruleEngine == null)
            {
                return;
            }

            rewardStrength = ruleEngine.RewardMultiplier;
            penaltyStrength = ruleEngine.PenaltyMultiplier;
            ChallengeSpec active = ruleEngine.ActiveChallenge;
            if (active == null)
            {
                return;
            }

            LoadChallengeForEditing(active);
            ruleEngine.ValidateChallenge(
                active,
                rewardStrength,
                penaltyStrength);
            CaptureCurrentValidation();
            MarkCurrentDraftValidated();
        }

        private void LoadChallengeForEditing(ChallengeSpec challenge)
        {
            if (challenge == null)
            {
                return;
            }

            improvementSet = null;
            modificationProposal = null;
            modificationProposalPrompt = string.Empty;
            ruleDrafts.Clear();
            openDropdownId = string.Empty;
            nextRuleNumber = 1;
            ChallengeSpec editableCopy = JsonUtility.FromJson<ChallengeSpec>(
                JsonUtility.ToJson(challenge));
            if (editableCopy == null)
            {
                return;
            }

            challengeId = string.IsNullOrWhiteSpace(editableCopy.Id)
                ? "manual_challenge"
                : editableCopy.Id;
            challengeName = editableCopy.DisplayName;
            goal = editableCopy.Goal;
            goalTargetText = editableCopy.GoalTarget.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
            timeLimitText = editableCopy.TimeLimit.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
            timeDamageScaleText = editableCopy.TimeDamageScale.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
            weapon = editableCopy.Weapon;

            GameplayRule[] rules = editableCopy.Rules;
            for (int index = 0; index < rules.Length; index++)
            {
                GameplayRule rule = rules[index];
                if (rule != null)
                {
                    ruleDrafts.Add(new RuleDraft(rule, index + 1));
                }
            }

            nextRuleNumber = ruleDrafts.Count + 1;
        }

        private void DrawCreator()
        {
            if (ruleEngine == null)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "RuleEngine is not available.", "RuleEngine 不可用。"));
                return;
            }

            RefreshWeaponOptions();
            bool showCurrentChallenge = hasGeneratedDraftThisSession &&
                                        HasEditableChallenge() &&
                                        gameplayProposal == null &&
                                        pendingGenerationRepair == null;
            DrawCreatorProgress(showCurrentChallenge);
            if (pendingGenerationRepair != null)
            {
                DrawGenerationRepair();
                return;
            }

            if (gameplayProposal != null)
            {
                if (showProposalRevision)
                {
                    if (GUILayout.Button(RuleForgeLocalization.T(
                            "Back to proposal", "返回提案"),
                            creatorSecondaryButtonStyle,
                            GUILayout.Height(34f)))
                    {
                        showProposalRevision = false;
                    }
                    DrawUnifiedDesigner();
                }
                else
                {
                    DrawGameplayProposal();
                    if (GUILayout.Button(RuleForgeLocalization.T(
                            "Change this proposal in one sentence",
                            "用一句话调整这个方案"),
                            creatorSecondaryButtonStyle,
                            GUILayout.Height(36f)))
                    {
                        showProposalRevision = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(statusMessage))
                {
                    GUILayout.Label(statusMessage, creatorMutedStyle);
                }
                if (!string.IsNullOrWhiteSpace(lastAIDiagnostic))
                {
                    showAIDiagnostic = GUILayout.Toggle(
                        showAIDiagnostic,
                        RuleForgeLocalization.T(
                            "Show technical error", "查看错误详情"),
                        creatorSecondaryButtonStyle,
                        GUILayout.Height(30f));
                    if (showAIDiagnostic)
                    {
                        DrawReadOnlyDiagnosticText(
                            RuleForgeLocalization.T(
                                "Validator details", "验证器详情"),
                            lastAIDiagnostic);
                    }
                }
                if (!CanUseRealAI())
                {
                    GUILayout.Label(BuildAIAvailabilityMessage(),
                        creatorWarningStyle);
                }
                return;
            }

            if (showCurrentChallenge)
            {
                DrawHumanReadableSummary();
                DrawPrimaryPlayButton();
            }

            DrawUnifiedDesigner();

            if (showCurrentChallenge)
            {
                DrawModificationFlow();
            }

            GUILayout.Space(10f);
            bool hasPendingAIReview = pendingModification != null ||
                                      modificationProposal != null ||
                                      improvementSet != null ||
                                      pendingGenerationRepair != null ||
                                      (aiController != null && aiController.IsBusy);
            GUI.enabled = !hasPendingAIReview;
            showAdvancedEdit = GUILayout.Toggle(
                showAdvancedEdit,
                RuleForgeLocalization.T(
                    "Advanced Edit — manual parameters",
                    "高级编辑 — 手动参数"),
                creatorSecondaryButtonStyle,
                GUILayout.Height(30f));
            GUI.enabled = true;
            if (showAdvancedEdit && !hasPendingAIReview)
            {
                DrawImproveWithAI();
                DrawAdvancedEditor();
                DrawBalanceDetails();
            }
            else if (showAdvancedEdit)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Close, apply, or cancel the current AI review before manual editing.",
                    "请先收起、应用或取消当前 AI 建议，再进行手动编辑。"));
            }

            showDeveloperView = GUILayout.Toggle(
                showDeveloperView,
                RuleForgeLocalization.T(
                    "Developer View — provider, validation and raw DSL",
                    "开发者视图 — Provider、验证与原始 DSL"),
                creatorSecondaryButtonStyle,
                GUILayout.Height(28f));
            if (showDeveloperView)
            {
                DrawDeveloperView();
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUILayout.Label(statusMessage, GUI.skin.box);
            }
        }

        private void EnsureCreatorStyles()
        {
            if (creatorTitleStyle != null)
            {
                return;
            }

            creatorTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            creatorTitleStyle.normal.textColor =
                new Color(0.23f, 0.9f, 1f, 1f);

            creatorSubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            creatorSubtitleStyle.normal.textColor =
                new Color(0.5f, 0.62f, 0.73f, 1f);

            creatorSectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            creatorSectionStyle.normal.textColor = Color.white;

            creatorMutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true
            };
            creatorMutedStyle.normal.textColor =
                new Color(0.62f, 0.72f, 0.8f, 1f);

            creatorPitchStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                padding = new RectOffset(2, 2, 8, 10)
            };
            creatorPitchStyle.normal.textColor =
                new Color(0.9f, 0.97f, 1f, 1f);

            creatorWarningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true
            };
            creatorWarningStyle.normal.textColor =
                new Color(1f, 0.72f, 0.28f, 1f);

            creatorSecondaryButtonStyle =
                RuleForgeGuiTheme.CreateSecondaryButtonStyle();
            creatorSuggestionStyle = new GUIStyle(
                creatorSecondaryButtonStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(14, 14, 8, 8)
            };
            creatorPlayButtonStyle =
                RuleForgeGuiTheme.CreatePlayButtonStyle();
            creatorCardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(22, 22, 18, 18)
            };
            creatorStepStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            creatorStepStyle.normal.textColor =
                new Color(0.6f, 0.8f, 0.88f, 1f);
        }

        private void DrawCreatorProgress(bool showCurrentChallenge)
        {
            string activeStep = showCurrentChallenge
                ? RuleForgeLocalization.T("3  REVIEW & PLAY", "3  查看并开始")
                : gameplayProposal != null
                    ? RuleForgeLocalization.T("2  REVIEW PROPOSAL", "2  确认方案")
                    : RuleForgeLocalization.T("1  DESCRIBE", "1  描述想法");
            GUILayout.Label(activeStep, creatorStepStyle);
            GUILayout.Space(10f);
        }

        private void DrawUnifiedDesigner()
        {
            bool refiningProposal = gameplayProposal != null;
            bool modifyingChallenge = !refiningProposal &&
                                      hasGeneratedDraftThisSession &&
                                      HasEditableChallenge();

            GUILayout.Space(12f);
            GUILayout.BeginVertical(creatorCardStyle);
            GUILayout.Label(RuleForgeLocalization.T(
                modifyingChallenge
                    ? "What would you like to change?"
                    : refiningProposal
                        ? "Refine this proposal"
                        : "What do you want to play?",
                modifyingChallenge
                    ? "你想怎么修改这个玩法？"
                    : refiningProposal
                        ? "调整这个方案"
                        : "你想做什么玩法？"), creatorSectionStyle);
            GUILayout.Label(RuleForgeLocalization.T(
                modifyingChallenge
                    ? "Tell AI what should feel different. It will propose the smallest change."
                    : refiningProposal
                        ? "Tell AI how to revise the proposal, or generate it as shown."
                        : "Describe the experience in one sentence. AI will turn it into an editable proposal.",
                modifyingChallenge
                    ? "直接说哪里需要改变，AI 会提出最小修改方案。"
                    : refiningProposal
                        ? "继续说你想怎么调整，或者按当前提案生成。"
                        : "用一句话描述体验，AI 会给出一份可以继续编辑的方案。"),
                creatorMutedStyle);
            DrawPromptArea(
                ref designerPrompt,
                RuleForgeLocalization.T(
                    modifyingChallenge
                        ? "For example: The late game is too chaotic. Slow down enemy speed growth and leave everything else unchanged."
                        : refiningProposal
                            ? "For example: Keep the core idea, but make the risk grow faster."
                            : "For example: Every kill makes me stronger, but enemies grow faster too. Survive for 60 seconds.",
                    modifyingChallenge
                        ? "例如：后期太疯了，降低敌人速度成长，其他内容不要改。"
                        : refiningProposal
                            ? "例如：保留核心想法，但让风险成长得更快。"
                            : "例如：每次击杀让我变强，但敌人也会更快；坚持生存 60 秒。"),
                modifyingChallenge ? 76f : refiningProposal ? 86f : 118f);

            if (!refiningProposal && !modifyingChallenge)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Try an example",
                    "试试示例"), creatorMutedStyle);
                DrawPromptChips(
                    CreationExampleLabelsEnglish,
                    CreationExampleLabelsChinese,
                    CreationExamplesEnglish,
                    CreationExamplesChinese,
                    value => designerPrompt = value,
                    2);
            }

            bool canSend = CanUseRealAI() &&
                           !aiController.IsBusy &&
                           !string.IsNullOrWhiteSpace(designerPrompt) &&
                           pendingModification == null &&
                           pendingGenerationRepair == null;
            GUI.enabled = canSend;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    modifyingChallenge
                        ? "PROPOSE CHANGES"
                        : refiningProposal
                            ? "REVISE PROPOSAL"
                            : "DESIGN MY CHALLENGE",
                    modifyingChallenge
                        ? "让 AI 提出修改"
                        : refiningProposal
                            ? "调整当前提案"
                            : "让 AI 设计玩法"), GUILayout.Height(50f)))
            {
                if (modifyingChallenge)
                {
                    BeginUnifiedModification(designerPrompt);
                }
                else if (refiningProposal)
                {
                    RefineGameplayProposal(designerPrompt);
                }
                else
                {
                    BeginNewGameplayProposal(designerPrompt);
                }
            }

            GUI.enabled = true;
            if (!CanUseRealAI())
            {
                GUILayout.Space(8f);
                GUILayout.Label(BuildAIAvailabilityMessage(),
                    creatorWarningStyle);
            }

            GUILayout.EndVertical();

        }

        private void BeginNewGameplayProposal(string prompt)
        {
            ClearPendingModification();
            gameplayProposal = null;
            showProposalRevision = false;
            pendingGenerationRepair = null;
            gameplayProposalPrompt = prompt;
            statusMessage = RuleForgeLocalization.T(
                "AI is turning your idea into a gameplay proposal...",
                "AI 正在把你的想法整理成玩法提案……");
            aiController.AnalyzeGameplay(
                prompt,
                null,
                string.Empty,
                HandleGameplayProposal);
        }

        private void BeginUnifiedModification(string prompt)
        {
            if (!TryBuildCandidate(
                    out ChallengeSpec current,
                    out string error))
            {
                statusMessage = error;
                return;
            }

            modificationBase = JsonUtility.FromJson<ChallengeSpec>(
                JsonUtility.ToJson(current));
            pendingModificationDiff.Clear();
            modificationProposal = null;
            modificationProposalPrompt = prompt;
            modificationRequestSignature = BuildDraftSignature();
            statusMessage = RuleForgeLocalization.T(
                "AI is identifying the smallest relevant change...",
                "AI 正在判断最相关的最小修改……");
            aiController.AnalyzeModification(
                prompt,
                current,
                HandleModificationProposal);
        }

        private void DrawGameplayProposal()
        {
            if (gameplayProposal == null)
            {
                return;
            }

            GUILayout.Space(10f);
            GUILayout.BeginVertical(creatorCardStyle);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI PROPOSAL", "AI 玩法提案"), creatorSectionStyle);
            GUILayout.Label(gameplayProposal.Summary, creatorPitchStyle);
            if (gameplayProposal.Warnings.Length > 0 &&
                !string.IsNullOrWhiteSpace(gameplayProposal.Warnings[0]))
            {
                GUILayout.Label(gameplayProposal.Warnings[0],
                    creatorWarningStyle);
            }

            if (!string.IsNullOrWhiteSpace(
                    gameplayProposal.ClarificationQuestion))
            {
                GUILayout.Label(gameplayProposal.ClarificationQuestion,
                    GUI.skin.box);
            }

            string[] suggestions = gameplayProposal.ActionSuggestions;
            if (suggestions.Length > 0)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Choose a direction", "或者选择一个方向"),
                    creatorMutedStyle);
                for (int index = 0; index < suggestions.Length; index++)
                {
                    string suggestion = suggestions[index];
                    if (!string.IsNullOrWhiteSpace(suggestion))
                    {
                        DrawProposalIntentButton(suggestion, suggestion);
                    }
                }
            }

            if (gameplayProposal.CanGenerate &&
                gameplayProposal.WithinVocabulary)
            {
                DrawProposalRatioControl();

                GUI.enabled = CanUseRealAI() && !aiController.IsBusy;
                if (GUILayout.Button(RuleForgeLocalization.T(
                        "GENERATE THIS", "按这个方案生成"),
                        GUILayout.Height(42f)))
                {
                    pendingGenerationRepair = null;
                    statusMessage = RuleForgeLocalization.T(
                        "Generating the confirmed proposal...",
                        "正在根据已确认的策划方案生成玩法……");
                    aiController.GenerateChallenge(
                        gameplayProposalPrompt,
                        gameplayProposal,
                        HandleAIGenerationPreview);
                }
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        private void DrawProposalRatioControl()
        {
            GUILayout.Space(6f);
            GUILayout.Label(RuleForgeLocalization.T(
                "Reward / risk preference",
                "奖惩比例"), creatorMutedStyle);
            float updated = GUILayout.HorizontalSlider(
                proposalPenaltyRewardRatio,
                0f,
                3f);
            proposalPenaltyRewardRatio = Mathf.Round(updated * 10f) / 10f;
            GUILayout.Label(RuleForgeLocalization.T(
                "Reward 1 : Risk ",
                "奖励 1 : 惩罚 ") +
                proposalPenaltyRewardRatio.ToString(
                    "0.0", CultureInfo.InvariantCulture) +
                "  ·  " + DescribeRatio(proposalPenaltyRewardRatio),
                creatorMutedStyle);

            if (Mathf.Abs(
                    proposalPenaltyRewardRatio -
                    gameplayProposal.PenaltyRewardRatio) < 0.05f)
            {
                return;
            }

            if (GUILayout.Button(RuleForgeLocalization.T(
                    "UPDATE PROPOSAL TO THIS RATIO",
                    "按这个奖惩比例调整方案"), GUILayout.Height(34f)))
            {
                string ratio = proposalPenaltyRewardRatio.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture);
                RefineGameplayProposal(RuleForgeLocalization.T(
                    "Adjust the proposal so penalty strength is " + ratio +
                    " times reward strength. Keep the core idea unchanged.",
                    "把方案调整为惩罚强度是奖励强度的 " + ratio +
                    " 倍，核心想法保持不变。"));
            }
        }

        private static string DescribeRatio(float ratio)
        {
            if (ratio < 0.05f)
            {
                return RuleForgeLocalization.T("No risk", "无惩罚");
            }
            if (ratio < 0.8f)
            {
                return RuleForgeLocalization.T("Reward focused", "奖励优先");
            }
            if (ratio <= 1.2f)
            {
                return RuleForgeLocalization.T("Balanced", "相对平衡");
            }
            if (ratio <= 2f)
            {
                return RuleForgeLocalization.T("High risk", "高风险");
            }

            return RuleForgeLocalization.T("Extreme risk", "极限风险");
        }

        private void DrawProposalIntentButton(string label, string intent)
        {
            GUI.enabled = !aiController.IsBusy;
            if (GUILayout.Button(label, creatorSuggestionStyle,
                    GUILayout.MinHeight(38f)))
            {
                RefineGameplayProposal(intent);
            }
            GUI.enabled = true;
        }

        private void RefineGameplayProposal(string intent)
        {
            statusMessage = RuleForgeLocalization.T(
                "AI is revising the gameplay proposal...",
                "AI 正在调整玩法提案……");
            designerPrompt = string.Empty;
            aiController.AnalyzeGameplay(
                gameplayProposalPrompt,
                gameplayProposal,
                intent,
                HandleGameplayProposal);
        }

        private static void DrawStringList(string[] values)
        {
            if (values == null)
            {
                return;
            }
            for (int index = 0; index < values.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(values[index]))
                {
                    GUILayout.Label("• " + values[index]);
                }
            }
        }

        private static void DrawPromptArea(
            ref string value,
            string placeholder,
            float height)
        {
            value = GUILayout.TextArea(
                value ?? string.Empty,
                GUILayout.MinHeight(height));
            Rect textRect = GUILayoutUtility.GetLastRect();
            if (!string.IsNullOrEmpty(value) ||
                Event.current.type != EventType.Repaint)
            {
                return;
            }

            GUIStyle placeholderStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.UpperLeft
            };
            placeholderStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            GUI.Label(
                new Rect(
                    textRect.x + 16f,
                    textRect.y + 13f,
                    textRect.width - 32f,
                    textRect.height - 26f),
                placeholder,
                placeholderStyle);
        }

        private void DrawPromptChips(
            string[] englishLabels,
            string[] chineseLabels,
            string[] englishPrompts,
            string[] chinesePrompts,
            Action<string> onSelected,
            int columns)
        {
            int safeColumns = Mathf.Max(1, columns);
            for (int index = 0; index < englishLabels.Length; index++)
            {
                if (index % safeColumns == 0)
                {
                    GUILayout.BeginHorizontal();
                }

                string label = RuleForgeLocalization.Current ==
                               RuleForgeLanguage.Chinese
                    ? chineseLabels[index]
                    : englishLabels[index];
                if (GUILayout.Button(label, creatorSecondaryButtonStyle,
                        GUILayout.Height(32f)))
                {
                    string prompt = RuleForgeLocalization.Current ==
                                    RuleForgeLanguage.Chinese
                        ? chinesePrompts[index]
                        : englishPrompts[index];
                    onSelected(prompt);
                }

                bool rowFinished = index % safeColumns == safeColumns - 1 ||
                                   index == englishLabels.Length - 1;
                if (rowFinished)
                {
                    GUILayout.EndHorizontal();
                }
            }
        }

        private bool CanUseRealAI()
        {
            return aiController != null &&
                   aiController.ActiveProviderKind == AIProviderKind.Real &&
                   aiController.ActiveProviderIsConfigured;
        }

        private bool HasEditableChallenge()
        {
            return !string.IsNullOrWhiteSpace(challengeName) &&
                   !string.IsNullOrWhiteSpace(goal) &&
                   ruleDrafts.Count > 0;
        }

        private void DrawHumanReadableSummary()
        {
            if (!TryBuildCandidate(out ChallengeSpec candidate, out _))
            {
                return;
            }

            GUILayout.BeginVertical(creatorCardStyle);
            GUILayout.Label(RuleForgeLocalization.T(
                "CURRENT PLAN",
                "当前方案"), creatorMutedStyle);
            GUILayout.Label(candidate.DisplayName.ToUpperInvariant(),
                creatorSectionStyle);

            List<string> rewards = new List<string>();
            List<string> penalties = new List<string>();
            List<string> neutral = new List<string>();
            GameplayRule[] rules = candidate.Rules;
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null)
                {
                    continue;
                }

                RuleEffect[] effects = rule.Effects;
                for (int effectIndex = 0;
                     effectIndex < effects.Length;
                     effectIndex++)
                {
                    RuleEffect effect = effects[effectIndex];
                    if (effect == null)
                    {
                        continue;
                    }

                    string summary = FormatEffectSummary(rule, effect);
                    if (ruleEngine.TryGetEffectDefinition(
                            effect.EffectId,
                            out EffectDefinition definition))
                    {
                        if (definition.Polarity == EffectPolarity.Reward)
                        {
                            rewards.Add(summary);
                        }
                        else if (definition.Polarity == EffectPolarity.Penalty)
                        {
                            penalties.Add(summary);
                        }
                        else
                        {
                            neutral.Add(summary);
                        }
                    }
                    else
                    {
                        neutral.Add(summary);
                    }
                }
            }

            GUILayout.Label(BuildOneSentenceSummary(
                candidate,
                rewards,
                penalties,
                neutral), creatorPitchStyle);

            GUILayout.EndVertical();
        }

        private string BuildOneSentenceSummary(
            ChallengeSpec candidate,
            List<string> rewards,
            List<string> penalties,
            List<string> neutral)
        {
            string sentence = FormatGoal(candidate);
            if (rewards.Count > 0)
            {
                sentence += RuleForgeLocalization.T(
                    "; your advantage is ",
                    "；你的优势是") + rewards[0];
            }
            if (penalties.Count > 0)
            {
                sentence += RuleForgeLocalization.T(
                    "; the cost is ",
                    "；代价是") + penalties[0];
            }
            if (neutral.Count > 0 && rewards.Count == 0)
            {
                sentence += RuleForgeLocalization.T(
                    "; meanwhile ",
                    "；同时") + neutral[0];
            }

            return sentence.TrimEnd('。', '.') +
                   RuleForgeLocalization.T(".", "。");
        }

        private void DrawBalanceDetails()
        {
            BalanceEvaluation balance = IsCurrentDraftValidated()
                ? currentDraftBalance
                : null;
            if (balance == null)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "Balance details",
                "平衡详情"), creatorSectionStyle);
            string ratio = float.IsPositiveInfinity(
                    balance.RewardPenaltyRatio)
                ? "∞"
                : balance.RewardPenaltyRatio.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);
            GUILayout.Label(RuleForgeLocalization.T(
                $"Reward {balance.RewardScore:0.#}  ·  Risk {balance.PenaltyScore:0.#}  ·  Ratio {ratio}",
                $"奖励 {balance.RewardScore:0.#}  ·  风险 {balance.PenaltyScore:0.#}  ·  比例 {ratio}"));
            GUILayout.Label(RuleForgeLocalization.T(
                "Difficulty  ",
                "难度  ") +
                RuleForgeLocalization.DataValue(balance.Difficulty.ToString()) +
                RuleForgeLocalization.T("  ·  Growth  ", "  ·  成长  ") +
                RuleForgeLocalization.DataValue(balance.Growth.ToString()));
            if (balance.GameplayTags.Count > 0)
            {
                List<string> localizedTags = new List<string>();
                for (int index = 0;
                     index < balance.GameplayTags.Count;
                     index++)
                {
                    localizedTags.Add(RuleForgeLocalization.DataValue(
                        balance.GameplayTags[index]));
                }

                GUILayout.Label(string.Join(" · ", localizedTags),
                    creatorMutedStyle);
            }
            GUILayout.EndVertical();
        }

        private static void DrawSummaryGroup(
            string heading,
            List<string> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            GUILayout.Space(5f);
            GUILayout.Label(heading);
            for (int index = 0; index < entries.Count; index++)
            {
                GUILayout.Label("• " + entries[index]);
            }
        }

        private string FormatGoal(ChallengeSpec candidate)
        {
            string target = candidate.GoalTarget.ToString(
                "0.##",
                CultureInfo.InvariantCulture);
            if (string.Equals(candidate.Goal, "Survive",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Survive for " + target + " seconds",
                    "生存 " + target + " 秒");
            }

            if (string.Equals(candidate.Goal, "KillCount",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Defeat " + target + " enemies" +
                    (candidate.TimeLimit > 0f
                        ? " within " + candidate.TimeLimit.ToString("0.#") + " seconds"
                        : string.Empty),
                    "击败 " + target + " 个敌人" +
                    (candidate.TimeLimit > 0f
                        ? "，限时 " + candidate.TimeLimit.ToString("0.#") + " 秒"
                        : string.Empty));
            }

            if (string.Equals(candidate.Goal, "TimeBankTarget",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Reach a " + target + "-second time bank; hits remove time",
                    "把时间生命积累到 " + target + " 秒；受击会扣时间");
            }

            if (string.Equals(candidate.Goal, "TimeBankSurvive",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Survive " + target + " seconds with time as health",
                    "用时间作为生命，坚持 " + target + " 秒");
            }

            if (string.Equals(candidate.Goal, "TimeBankEndless",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Endless trial until the time bank runs out",
                    "无尽试炼：时间生命耗尽时结束");
            }

            return RuleForgeLocalization.T(
                "Reach " + target + " score" +
                (candidate.TimeLimit > 0f
                    ? " within " + candidate.TimeLimit.ToString("0.#") + " seconds"
                    : string.Empty),
                "达到 " + target + " 分" +
                (candidate.TimeLimit > 0f
                    ? "，限时 " + candidate.TimeLimit.ToString("0.#") + " 秒"
                    : string.Empty));
        }

        private string FormatEffectSummary(GameplayRule rule, RuleEffect effect)
        {
            string trigger = rule.Trigger != null
                ? RuleForgeLocalization.DataValue(rule.Trigger.Type)
                : RuleForgeLocalization.T("Event", "事件");
            string effectName = effect.EffectId;
            if (ruleEngine.TryGetEffectDefinition(
                    effect.EffectId,
                    out EffectDefinition definition))
            {
                effectName = RuleForgeLocalization.EffectName(
                    definition.EffectId,
                    definition.DisplayName);
            }

            string value = FormatEffectValue(effect);
            string conditions = FormatConditionSummary(rule);
            string duration = effect.Duration > 0f
                ? RuleForgeLocalization.T(
                    $", lasts {effect.Duration.ToString("0.##", CultureInfo.InvariantCulture)} sec",
                    $"，持续 {effect.Duration.ToString("0.##", CultureInfo.InvariantCulture)} 秒")
                : string.Empty;
            string stacks = effect.MaxStacks > 1
                ? RuleForgeLocalization.T(
                    $", up to {effect.MaxStacks} stacks",
                    $"，最多 {effect.MaxStacks} 层")
                : string.Empty;
            return RuleForgeLocalization.T(
                trigger + conditions + ": " + effectName + " " + value +
                duration + stacks,
                trigger + conditions + "：" + effectName + " " + value +
                duration + stacks);
        }

        private static string FormatConditionSummary(GameplayRule rule)
        {
            List<string> summaries = new List<string>();
            RuleCondition[] conditions = rule.Conditions;
            for (int index = 0; index < conditions.Length; index++)
            {
                RuleCondition condition = conditions[index];
                if (condition == null || string.Equals(
                        condition.Type,
                        RuleConditionType.Always.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(
                        condition.Type,
                        RuleConditionType.RandomChance.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    summaries.Add(RuleForgeLocalization.T(
                        (condition.Value * 100f).ToString(
                            "0.#",
                            CultureInfo.InvariantCulture) + "% chance",
                        (condition.Value * 100f).ToString(
                            "0.#",
                            CultureInfo.InvariantCulture) + "% 概率"));
                    continue;
                }

                string comparison = string.IsNullOrWhiteSpace(condition.Comparison)
                    ? RuleComparison.Equals.ToString()
                    : condition.Comparison;
                string comparedValue = !string.IsNullOrWhiteSpace(
                        condition.StringValue)
                    ? RuleForgeLocalization.DataValue(condition.StringValue)
                    : condition.Value.ToString(
                        "0.##",
                        CultureInfo.InvariantCulture);
                summaries.Add(
                    RuleForgeLocalization.DataValue(condition.Type) + " " +
                    RuleForgeLocalization.DataValue(comparison) + " " +
                    comparedValue);
            }

            return summaries.Count > 0
                ? " (" + string.Join(", ", summaries) + ")"
                : string.Empty;
        }

        private static string FormatEffectValue(RuleEffect effect)
        {
            RuleScaling scaling = effect.Scaling;
            if (scaling != null &&
                !string.IsNullOrWhiteSpace(scaling.Source))
            {
                string minimum = FormatEffectNumericValue(
                    effect,
                    scaling.EffectMin);
                string maximum = FormatEffectNumericValue(
                    effect,
                    scaling.EffectMax);
                return RuleForgeLocalization.T(
                    $"scales with {RuleForgeLocalization.DataValue(scaling.Source)} " +
                    $"from {minimum} to {maximum}",
                    $"随{RuleForgeLocalization.DataValue(scaling.Source)}" +
                    $"从 {minimum} 变化至 {maximum}");
            }

            return FormatEffectNumericValue(effect, effect.Value);
        }

        private static string FormatEffectNumericValue(
            RuleEffect effect,
            float value)
        {
            if (string.Equals(effect.Kind, RuleEffectKind.AddTime.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "+" + value.ToString("0.##", CultureInfo.InvariantCulture) +
                    " seconds",
                    "+" + value.ToString("0.##", CultureInfo.InvariantCulture) +
                    " 秒");
            }

            if (string.Equals(
                    effect.Operation,
                    StatModifierOperation.AddPercent.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                float percentage = value * 100f;
                return (percentage >= 0f ? "+" : string.Empty) +
                       percentage.ToString("0.##", CultureInfo.InvariantCulture) + "%";
            }

            if (!string.IsNullOrWhiteSpace(effect.StringValue))
            {
                return RuleForgeLocalization.DataValue(effect.StringValue);
            }

            return (value >= 0f ? "+" : string.Empty) +
                   value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void DrawPrimaryPlayButton()
        {
            GUILayout.Space(8f);
            bool canPlay = IsCurrentDraftValidated() &&
                           currentDraftValidation != null &&
                           currentDraftValidation.IsValid &&
                           pendingModification == null &&
                           pendingGenerationRepair == null &&
                           modificationProposal == null &&
                           (aiController == null || !aiController.IsBusy);
            GUI.enabled = canPlay;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "PLAY THIS CHALLENGE",
                    "开始这个玩法"), creatorPlayButtonStyle,
                    GUILayout.Height(56f)))
            {
                ValidateAndPlay();
            }

            GUI.enabled = true;
            if (!canPlay)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Generate or validate a valid preview before playing.",
                    "生成玩法或验证当前修改后即可开始。"));
            }
        }

        private void DrawModificationFlow()
        {
            DrawModificationProposal();
            DrawPendingModification();
        }

        private void DrawImproveWithAI()
        {
            GUILayout.Space(10f);
            GUI.enabled = CanUseRealAI() && !aiController.IsBusy &&
                          pendingModification == null &&
                          IsCurrentDraftValidated() &&
                          currentDraftValidation != null &&
                          currentDraftValidation.IsValid;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "IMPROVE WITH AI", "让 AI 改进玩法"),
                    GUILayout.Height(38f)))
            {
                if (TryBuildCandidate(
                        out ChallengeSpec current,
                        out string error))
                {
                    improvementSet = null;
                    improvementRequestSignature = BuildDraftSignature();
                    statusMessage = RuleForgeLocalization.T(
                        "AI is reviewing the current ChallengeSpec...",
                        "AI 正在审查当前真实 ChallengeSpec……");
                    aiController.AnalyzeImprovements(
                        current,
                        HandleImprovementSet);
                }
                else
                {
                    statusMessage = error;
                }
            }
            GUI.enabled = true;

            if (improvementSet == null)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI GAMEPLAY CRITIC", "AI 玩法评审"), GUI.skin.box);
            GUILayout.Label(improvementSet.Summary);
            GameplayImprovementSuggestion[] suggestions =
                improvementSet.Suggestions;
            for (int index = 0; index < suggestions.Length; index++)
            {
                GameplayImprovementSuggestion suggestion = suggestions[index];
                if (suggestion == null ||
                    string.IsNullOrWhiteSpace(suggestion.Intent))
                {
                    continue;
                }

                GUILayout.Label(suggestion.Title);
                GUILayout.Label(suggestion.Reasoning);
                GUI.enabled = !aiController.IsBusy &&
                              pendingModification == null;
                if (GUILayout.Button(RuleForgeLocalization.T(
                        "EXPLORE: ", "进一步分析：") + suggestion.Title))
                {
                    BeginSuggestedImprovement(suggestion.Intent);
                }
                GUI.enabled = true;
            }
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "HIDE SUGGESTIONS", "收起建议")))
            {
                improvementSet = null;
                improvementRequestSignature = string.Empty;
            }
            GUILayout.EndVertical();
        }

        private void BeginSuggestedImprovement(string intent)
        {
            if (!TryBuildCandidate(
                    out ChallengeSpec current,
                    out string error))
            {
                statusMessage = error;
                return;
            }

            designerPrompt = intent;
            modificationProposalPrompt = intent;
            modificationBase = JsonUtility.FromJson<ChallengeSpec>(
                JsonUtility.ToJson(current));
            modificationProposal = null;
            modificationRequestSignature = BuildDraftSignature();
            pendingModificationDiff.Clear();
            statusMessage = RuleForgeLocalization.T(
                "AI is turning the selected idea into a minimal change proposal...",
                "AI 正在把所选建议转换为最小修改提案……");
            aiController.AnalyzeModification(
                intent,
                current,
                HandleModificationProposal);
        }

        private void DrawModificationProposal()
        {
            if (modificationProposal == null || pendingModification != null)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI CHANGE PROPOSAL", "AI 修改提案"), creatorSectionStyle);
            GUILayout.Label(modificationProposal.Summary, creatorPitchStyle);
            if (modificationProposal.Warnings.Length > 0 &&
                !string.IsNullOrWhiteSpace(modificationProposal.Warnings[0]))
            {
                GUILayout.Label(modificationProposal.Warnings[0],
                    creatorWarningStyle);
            }
            if (!string.IsNullOrWhiteSpace(
                    modificationProposal.ClarificationQuestion))
            {
                GUILayout.Label(modificationProposal.ClarificationQuestion,
                    GUI.skin.box);
                GUILayout.Label(RuleForgeLocalization.T(
                    "Rewrite your feedback above and analyze again.",
                    "请在上方补充说明后重新分析。"));
            }

            GUI.enabled = modificationProposal.CanModify &&
                          !aiController.IsBusy &&
                          !string.IsNullOrWhiteSpace(
                              modificationProposal.PatchInstruction);
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "CONFIRM AND BUILD PATCH",
                    "确认并生成修改"), GUILayout.Height(38f)))
            {
                if (!string.Equals(
                        modificationRequestSignature,
                        BuildDraftSignature(),
                        StringComparison.Ordinal))
                {
                    modificationProposal = null;
                    statusMessage = RuleForgeLocalization.T(
                        "The challenge changed. Ask AI to analyze the modification again.",
                        "玩法已经变化，请让 AI 重新分析修改要求。" );
                    GUI.enabled = true;
                    GUILayout.EndVertical();
                    return;
                }
                patchRequestSignature = modificationRequestSignature;
                statusMessage = RuleForgeLocalization.T(
                    "AI is building the confirmed minimal ChallengePatch...",
                    "AI 正在生成已确认的最小修改……");
                aiController.ModifyChallenge(
                    modificationProposal.PatchInstruction,
                    modificationBase,
                    HandleAIModificationPreview);
            }
            GUI.enabled = true;
            if (GUILayout.Button(RuleForgeLocalization.T("CANCEL", "取消")))
            {
                modificationProposal = null;
                modificationProposalPrompt = string.Empty;
            }
            GUILayout.EndVertical();
        }

        private void DrawPendingModification()
        {
            if (pendingModification == null)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "Proposed Changes",
                "建议修改"), GUI.skin.box);
            if (pendingModificationPreview != null &&
                pendingModificationPreview.RequiresRepairConfirmation)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "The original patch exceeded Validator limits:",
                    "原始修改超出验证器限制："));
                DrawStringList(pendingModificationPreview.RepairSourceErrors);
                GUILayout.Label(pendingModificationPreview.RepairSummary);
                DrawStringList(pendingModificationPreview.RepairChanges);
            }
            if (pendingModificationDiff.Count == 0)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "No visible changes were returned.",
                    "没有返回可见修改。"));
            }
            else
            {
                for (int index = 0;
                     index < pendingModificationDiff.Count;
                     index++)
                {
                    GUILayout.Label("• " + pendingModificationDiff[index]);
                }

                GUILayout.Label(RuleForgeLocalization.T(
                    "Everything else unchanged",
                    "其他内容保持不变"));
            }

            GUILayout.BeginHorizontal();
            GUI.enabled = pendingModificationDiff.Count > 0;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "APPLY CHANGES",
                    "应用修改"), GUILayout.Height(38f)))
            {
                ChallengeSpec accepted = pendingModification;
                ClearPendingModification();
                LoadChallengeForEditing(accepted);
                hasUnplayedPreview = true;
                designerPrompt = string.Empty;
                ValidationResult validation = ruleEngine.ValidateChallenge(
                    accepted,
                    rewardStrength,
                    penaltyStrength);
                CaptureCurrentValidation();
                MarkCurrentDraftValidated();
                statusMessage = validation != null && validation.IsValid
                    ? RuleForgeLocalization.T(
                        "Changes applied to the preview. Press Play when ready.",
                        "修改已应用到预览。确认后可直接开始。")
                    : RuleForgeLocalization.T(
                        "Validator rejected the applied preview.",
                        "验证器拒绝了修改后的预览。" );
            }

            GUI.enabled = true;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Cancel",
                    "取消"), GUILayout.Height(38f)))
            {
                ClearPendingModification();
                statusMessage = RuleForgeLocalization.T(
                    "Proposed changes cancelled.",
                    "已取消建议修改。" );
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawAdvancedEditor()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "Manual Parameter Editing",
                "手动参数编辑"), GUI.skin.box);
            DrawTextField(RuleForgeLocalization.T(
                "Challenge Name", "挑战名称"), ref challengeName);
            DrawDropdown(
                "challenge-goal",
                RuleForgeLocalization.T("Goal", "目标"),
                goal,
                GoalOptions,
                value => goal = value);
            DrawTextField(GetGoalTargetLabel(), ref goalTargetText);
            DrawTextField(RuleForgeLocalization.T(
                "Time Limit (0 = none)", "限时秒数（0 为不限时）"),
                ref timeLimitText);
            if (goal.StartsWith("TimeBank", StringComparison.OrdinalIgnoreCase))
            {
                DrawTextField(RuleForgeLocalization.T(
                    "Seconds lost per damage point",
                    "每点伤害扣除秒数"), ref timeDamageScaleText);
            }
            DrawDropdown(
                "challenge-weapon",
                RuleForgeLocalization.T("Weapon", "武器"),
                weapon,
                weaponOptions,
                value => weapon = value);
            if (weaponOptions.Length == 0)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "No configured weapons are available.",
                    "没有可用的已配置武器。"));
            }

            DrawStrengthSliders();
            GUILayout.Label(RuleForgeLocalization.T(
                "Rules and parameters",
                "规则与参数"), GUI.skin.box);
            for (int ruleIndex = 0; ruleIndex < ruleDrafts.Count; ruleIndex++)
            {
                if (DrawRule(ruleDrafts[ruleIndex], ruleIndex))
                {
                    ruleDrafts.RemoveAt(ruleIndex);
                    openDropdownId = string.Empty;
                    break;
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Add Rule", "添加规则"), GUILayout.Height(30f)))
            {
                AddRule();
            }

            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Reload Active", "重新载入当前挑战"), GUILayout.Height(30f)))
            {
                RebuildFromActiveChallenge();
            }

            GUILayout.EndHorizontal();
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Validate Manual Changes",
                    "验证手动修改"), GUILayout.Height(34f)))
            {
                ValidatePreview();
            }

            GUILayout.EndVertical();
        }

        private void DrawDeveloperView()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI / Validation Diagnostics",
                "AI / 验证诊断"), GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI status: ", "AI 状态：") + GetAIStatus());
            GUILayout.Label(RuleForgeLocalization.T(
                "Provider: ", "Provider：") +
                (aiController != null
                    ? aiController.ActiveProviderName
                    : RuleForgeLocalization.T("None", "无")));
            GUI.enabled = aiController != null && !aiController.IsBusy;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Refresh Provider",
                    "刷新 Provider"), GUILayout.Width(180f)))
            {
                aiController.RefreshProviderSelection();
                statusMessage = RuleForgeLocalization.T(
                    "Current provider: ",
                    "当前 Provider：") + aiController.ActiveProviderName;
            }

            GUI.enabled = true;
            GUILayout.Label(RuleForgeLocalization.T(
                "AI connection settings are available from the button at the top of this panel.",
                "AI 连接配置请使用面板顶部的“配置 AI / AI 设置”按钮。"));
            DrawReadOnlyDiagnosticText(
                RuleForgeLocalization.T(
                    "Original Generate Prompt",
                    "原始生成 Prompt"),
                !string.IsNullOrWhiteSpace(lastGeneratedPrompt)
                    ? lastGeneratedPrompt
                    : diagnosticSourcePrompt);
            DrawReadOnlyDiagnosticText(
                RuleForgeLocalization.T(
                    "Latest Modify Request",
                    "最近修改请求"),
                lastModificationPrompt);
            DrawValidationFeedback();
            DrawReadOnlyDiagnosticText(
                RuleForgeLocalization.T("Latest AI error", "最近的 AI 错误"),
                lastAIDiagnostic);
            if (TryBuildCandidate(out ChallengeSpec candidate, out _))
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Raw ChallengeSpec (read-only)",
                    "原始 ChallengeSpec（只读）"));
                GUI.enabled = false;
                GUILayout.TextArea(
                    JsonUtility.ToJson(candidate, true),
                    GUILayout.MinHeight(180f));
                GUI.enabled = true;
            }

            GUILayout.EndVertical();
        }

        private static void DrawReadOnlyDiagnosticText(
            string label,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            GUILayout.Label(label);
            GUI.enabled = false;
            GUILayout.TextArea(value, GUILayout.MinHeight(45f));
            GUI.enabled = true;
        }

        private bool IsCurrentDraftValidated()
        {
            string currentSignature = BuildDraftSignature();
            return !string.IsNullOrEmpty(currentSignature) &&
                   string.Equals(
                       currentSignature,
                       validatedDraftSignature,
                       StringComparison.Ordinal);
        }

        private void ClearPendingModification()
        {
            pendingModification = null;
            pendingModificationPreview = null;
            modificationBase = null;
            modificationProposal = null;
            modificationProposalPrompt = string.Empty;
            modificationRequestSignature = string.Empty;
            patchRequestSignature = string.Empty;
            pendingModificationDiff.Clear();
        }

        private void BuildChallengeDiff(
            ChallengeSpec before,
            ChallengeSpec after,
            List<string> output)
        {
            output.Clear();
            if (before == null || after == null)
            {
                return;
            }

            AddTextDiff(
                output,
                RuleForgeLocalization.T("Challenge name", "玩法名称"),
                before.DisplayName,
                after.DisplayName);
            AddTextDiff(
                output,
                RuleForgeLocalization.T("Goal", "目标"),
                FormatGoal(before),
                FormatGoal(after));
            AddTextDiff(
                output,
                RuleForgeLocalization.T("Weapon", "武器"),
                RuleForgeLocalization.DataValue(before.Weapon),
                RuleForgeLocalization.DataValue(after.Weapon));

            Dictionary<string, GameplayRule> beforeRules =
                IndexRules(before.Rules);
            Dictionary<string, GameplayRule> afterRules =
                IndexRules(after.Rules);
            foreach (KeyValuePair<string, GameplayRule> pair in beforeRules)
            {
                if (!afterRules.TryGetValue(
                        pair.Key,
                        out GameplayRule afterRule))
                {
                    output.Add(RuleForgeLocalization.T(
                        "Removed rule: ",
                        "移除玩法效果：") + DescribeRule(pair.Value));
                    continue;
                }

                GameplayRule beforeRule = pair.Value;
                string beforeTrigger = beforeRule.Trigger != null
                    ? RuleForgeLocalization.DataValue(beforeRule.Trigger.Type)
                    : string.Empty;
                string afterTrigger = afterRule.Trigger != null
                    ? RuleForgeLocalization.DataValue(afterRule.Trigger.Type)
                    : string.Empty;
                AddTextDiff(
                    output,
                    RuleForgeLocalization.T(
                        "When " + GetRuleEffectNames(beforeRule) + " activates",
                        GetRuleEffectNames(beforeRule) + " 的触发时机"),
                    beforeTrigger,
                    afterTrigger);
                CompareConditions(
                    GetRuleEffectNames(beforeRule),
                    beforeRule.Conditions,
                    afterRule.Conditions,
                    output);
                CompareEffects(
                    beforeRule.Effects,
                    afterRule.Effects,
                    output);
            }

            foreach (KeyValuePair<string, GameplayRule> pair in afterRules)
            {
                if (!beforeRules.ContainsKey(pair.Key))
                {
                    output.Add(RuleForgeLocalization.T(
                        "Added rule: ",
                        "新增玩法效果：") + DescribeRule(pair.Value));
                }
            }
        }

        private string DescribeRule(GameplayRule rule)
        {
            if (rule == null || rule.Effects.Length == 0)
            {
                return RuleForgeLocalization.T("Gameplay rule", "玩法规则");
            }

            string result = string.Empty;
            RuleEffect[] effects = rule.Effects;
            for (int index = 0; index < effects.Length; index++)
            {
                if (index > 0)
                {
                    result += RuleForgeLocalization.T("; ", "；");
                }

                result += FormatEffectSummary(rule, effects[index]);
            }

            return result;
        }

        private string GetRuleEffectNames(GameplayRule rule)
        {
            if (rule == null || rule.Effects.Length == 0)
            {
                return RuleForgeLocalization.T("Gameplay effect", "玩法效果");
            }

            string result = string.Empty;
            RuleEffect[] effects = rule.Effects;
            for (int index = 0; index < effects.Length; index++)
            {
                if (index > 0)
                {
                    result += RuleForgeLocalization.T(" + ", " + ");
                }

                result += GetEffectDisplayName(effects[index]);
            }

            return result;
        }

        private static Dictionary<string, GameplayRule> IndexRules(
            GameplayRule[] rules)
        {
            Dictionary<string, GameplayRule> indexed =
                new Dictionary<string, GameplayRule>(
                    StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < rules.Length; index++)
            {
                GameplayRule rule = rules[index];
                if (rule != null)
                {
                    string key = string.IsNullOrWhiteSpace(rule.Id)
                        ? "Rule " + (index + 1)
                        : rule.Id;
                    indexed[key] = rule;
                }
            }

            return indexed;
        }

        private void CompareConditions(
            string ruleLabel,
            RuleCondition[] before,
            RuleCondition[] after,
            List<string> output)
        {
            int sharedCount = Mathf.Min(before.Length, after.Length);
            for (int index = 0; index < sharedCount; index++)
            {
                RuleCondition oldCondition = before[index];
                RuleCondition newCondition = after[index];
                if (oldCondition == null || newCondition == null)
                {
                    continue;
                }

                string label = RuleForgeLocalization.T(
                    "Condition " + (index + 1) + " for " + ruleLabel,
                    ruleLabel + " 的条件 " + (index + 1));
                AddTextDiff(
                    output,
                    label,
                    FormatCondition(oldCondition),
                    FormatCondition(newCondition));
            }

            if (before.Length != after.Length)
            {
                AddTextDiff(
                    output,
                    RuleForgeLocalization.T(
                        "Condition count for " + ruleLabel,
                        ruleLabel + " 的条件数量"),
                    before.Length.ToString(CultureInfo.InvariantCulture),
                    after.Length.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static string FormatCondition(RuleCondition condition)
        {
            if (condition == null)
            {
                return RuleForgeLocalization.T("None", "无");
            }

            if (string.Equals(
                    condition.Type,
                    RuleConditionType.RandomChance.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T("Probability ", "概率 ") +
                       (condition.Value * 100f).ToString(
                           "0.##",
                       CultureInfo.InvariantCulture) + "%";
            }

            if (string.Equals(
                    condition.Type,
                    RuleConditionType.EventValue.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.DataValue(condition.Type) + " " +
                       RuleForgeLocalization.DataValue(condition.Comparison) +
                       " " + condition.Value.ToString(
                           "0.##",
                           CultureInfo.InvariantCulture);
            }

            string suffix = !string.IsNullOrWhiteSpace(condition.StringValue)
                ? " " + RuleForgeLocalization.DataValue(condition.StringValue)
                : " " + condition.Value.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);
            return RuleForgeLocalization.DataValue(condition.Type) + suffix;
        }

        private void CompareEffects(
            RuleEffect[] before,
            RuleEffect[] after,
            List<string> output)
        {
            Dictionary<string, RuleEffect> beforeEffects = IndexEffects(before);
            Dictionary<string, RuleEffect> afterEffects = IndexEffects(after);
            foreach (KeyValuePair<string, RuleEffect> pair in beforeEffects)
            {
                string effectLabel = GetEffectDisplayName(pair.Value);
                if (!afterEffects.TryGetValue(
                        pair.Key,
                        out RuleEffect afterEffect))
                {
                    output.Add(RuleForgeLocalization.T(
                        "Removed effect: ",
                        "移除效果：") + effectLabel);
                    continue;
                }

                RuleEffect beforeEffect = pair.Value;
                AddTextDiff(
                    output,
                    effectLabel + RuleForgeLocalization.T(
                        " per trigger",
                        "（每次触发）"),
                    FormatEffectValue(beforeEffect),
                    FormatEffectValue(afterEffect));
                AddNumberDiff(
                    output,
                    effectLabel + RuleForgeLocalization.T(
                        " maximum stacks",
                        "最大层数"),
                    beforeEffect.MaxStacks,
                    afterEffect.MaxStacks);
                AddFloatDiff(
                    output,
                    effectLabel + RuleForgeLocalization.T(
                        " duration",
                        "持续时间"),
                    beforeEffect.Duration,
                    afterEffect.Duration,
                    RuleForgeLocalization.T(" sec", " 秒"));
                AddTextDiff(
                    output,
                    effectLabel + RuleForgeLocalization.T(
                        " target",
                        "目标"),
                    RuleForgeLocalization.DataValue(beforeEffect.StringValue),
                    RuleForgeLocalization.DataValue(afterEffect.StringValue));
            }

            foreach (KeyValuePair<string, RuleEffect> pair in afterEffects)
            {
                if (!beforeEffects.ContainsKey(pair.Key))
                {
                    output.Add(RuleForgeLocalization.T(
                        "Added effect: ",
                        "新增效果：") + GetEffectDisplayName(pair.Value));
                }
            }
        }

        private static Dictionary<string, RuleEffect> IndexEffects(
            RuleEffect[] effects)
        {
            Dictionary<string, RuleEffect> indexed =
                new Dictionary<string, RuleEffect>(
                    StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < effects.Length; index++)
            {
                RuleEffect effect = effects[index];
                if (effect != null)
                {
                    string key = string.IsNullOrWhiteSpace(effect.EffectId)
                        ? "Effect " + (index + 1)
                        : effect.EffectId;
                    indexed[key] = effect;
                }
            }

            return indexed;
        }

        private string GetEffectDisplayName(RuleEffect effect)
        {
            if (effect != null && ruleEngine.TryGetEffectDefinition(
                    effect.EffectId,
                    out EffectDefinition definition))
            {
                return RuleForgeLocalization.EffectName(
                    definition.EffectId,
                    definition.DisplayName);
            }

            return effect != null ? effect.EffectId : string.Empty;
        }

        private static void AddTextDiff(
            List<string> output,
            string label,
            string before,
            string after)
        {
            string oldValue = before ?? string.Empty;
            string newValue = after ?? string.Empty;
            if (!string.Equals(
                    oldValue,
                    newValue,
                    StringComparison.Ordinal))
            {
                output.Add(label + ": " + oldValue + " → " + newValue);
            }
        }

        private static void AddNumberDiff(
            List<string> output,
            string label,
            int before,
            int after)
        {
            if (before != after)
            {
                output.Add(label + ": " + before + " → " + after);
            }
        }

        private static void AddFloatDiff(
            List<string> output,
            string label,
            float before,
            float after,
            string suffix)
        {
            if (!Mathf.Approximately(before, after))
            {
                output.Add(
                    label + ": " +
                    before.ToString("0.##", CultureInfo.InvariantCulture) +
                    suffix + " → " +
                    after.ToString("0.##", CultureInfo.InvariantCulture) +
                    suffix);
            }
        }

        private string GetAIStatus()
        {
            if (aiController == null ||
                aiController.ActiveProviderKind == AIProviderKind.Unknown)
            {
                return RuleForgeLocalization.T("AI Not Connected", "AI 未连接");
            }

            switch (aiController.ActiveProviderKind)
            {
                case AIProviderKind.Mock:
                    return RuleForgeLocalization.T(
                        "Mock Provider Active", "模拟 Provider 已启用");
                case AIProviderKind.Real:
                    switch (aiController.ActiveProviderConnectionState)
                    {
                        case AIProviderConnectionState.Verified:
                            return RuleForgeLocalization.T(
                                "Real AI Connection Verified",
                                "真实 AI 连接已验证");
                        case AIProviderConnectionState.Verifying:
                            return RuleForgeLocalization.T(
                                "Verifying AI Connection",
                                "正在验证 AI 连接");
                        case AIProviderConnectionState.Failed:
                            return RuleForgeLocalization.T(
                                "AI Connection Failed",
                                "AI 连接失败");
                        case AIProviderConnectionState.Unverified:
                            return RuleForgeLocalization.T(
                                "Credentials Present — Not Verified",
                                "已有凭据 — 尚未验证");
                        default:
                            return RuleForgeLocalization.T(
                                "Real AI Provider Not Configured",
                                "真实 AI Provider 未配置");
                    }
                default:
                    return RuleForgeLocalization.T("AI Not Connected", "AI 未连接");
            }
        }

        private string BuildAIAvailabilityMessage()
        {
            if (aiController == null ||
                aiController.ActiveProviderKind == AIProviderKind.Unknown)
            {
                return RuleForgeLocalization.T(
                    "AI component is missing. Check the Arena scene setup.",
                    "场景中缺少 AI 组件，请检查 Arena 的接线。");
            }

            if (aiController.ActiveProviderKind == AIProviderKind.Mock)
            {
                return RuleForgeLocalization.T(
                    "Offline Mock is selected. Use AI Settings at the top to configure a real provider.",
                    "当前选中离线 Mock。请点顶部“AI 设置”配置真实 Provider。");
            }

            return RuleForgeLocalization.T(
                "The selected provider is not configured: ",
                "当前 Provider 尚未配置：") +
                aiController.ActiveProviderConnectionMessage;
        }

        private void HandleAIGenerationPreview(AIChallengePreview preview)
        {
            if (preview == null || !preview.Success)
            {
                SetPreviewFailure(preview);
                return;
            }

            lastAIDiagnostic = string.Empty;
            showAIDiagnostic = false;

            if (preview.RequiresRepairConfirmation)
            {
                pendingGenerationRepair = preview;
                statusMessage = RuleForgeLocalization.T(
                    "Validator rejected the first result. AI prepared a legal repair for your confirmation.",
                    "验证器拒绝了初始结果。AI 已根据真实错误准备合法修正版，请确认。" );
                return;
            }

            ClearPendingModification();
            lastGeneratedPrompt = gameplayProposalPrompt;
            lastModificationPrompt = string.Empty;
            LoadChallengeForEditing(preview.Challenge);
            hasGeneratedDraftThisSession = true;
            hasUnplayedPreview = true;
            scrollPosition = Vector2.zero;
            gameplayProposal = null;
            designerPrompt = string.Empty;
            ValidationResult currentValidation = ruleEngine.ValidateChallenge(
                preview.Challenge,
                rewardStrength,
                penaltyStrength);
            CaptureCurrentValidation();
            MarkCurrentDraftValidated();
            bool isValid = currentValidation != null &&
                           currentValidation.IsValid;
            statusMessage = isValid
                ? preview.Source + RuleForgeLocalization.T(
                    " — Validator PASS. Review/edit before Play.",
                    " — 验证器通过。请在开始前审核/编辑。")
                : preview.Source + RuleForgeLocalization.T(
                    " — Validator REJECTED. Open Advanced Edit to correct it.",
                    " — 验证器拒绝。请打开“高级编辑”修正。" );
        }

        private void SetPreviewFailure(AIChallengePreview preview)
        {
            lastAIDiagnostic = preview != null
                ? preview.Error
                : string.Empty;
            showAIDiagnostic = false;
            bool validationFailure = lastAIDiagnostic.StartsWith(
                "AI repair", StringComparison.OrdinalIgnoreCase) ||
                lastAIDiagnostic.StartsWith(
                    "Validator rejected", StringComparison.OrdinalIgnoreCase);
            bool intentFailure = lastAIDiagnostic.StartsWith(
                "AI intent mismatch", StringComparison.OrdinalIgnoreCase);
            statusMessage = intentFailure
                ? RuleForgeLocalization.T(
                    "The AI result changed the confirmed core gameplay, so it was not applied. Refine the proposal or try generating again; see error details below.",
                    "AI 结果改掉了你确认的核心玩法，已阻止应用。请调整提案或重新生成；具体差异可在错误详情查看。")
                : validationFailure
                ? RuleForgeLocalization.T(
                    "AI could not produce a legal version of this idea. The current challenge was not changed. Revise the proposal or expand the error details below.",
                    "AI 还没能把这个想法修成合法玩法，当前挑战未改变。请调整提案后重试，或展开下方错误详情。")
                : preview != null
                    ? preview.Error
                    : RuleForgeLocalization.T(
                        "AI provider returned no preview.",
                        "AI Provider 没有返回预览。");
        }

        private void HandleAIModificationPreview(AIChallengePreview preview)
        {
            if (!IsExpectedDraft(patchRequestSignature))
            {
                ClearPendingModification();
                statusMessage = BuildStaleAIResultMessage();
                return;
            }

            if (preview == null || !preview.Success)
            {
                SetPreviewFailure(preview);
                return;
            }

            lastAIDiagnostic = string.Empty;
            showAIDiagnostic = false;

            if (preview.Validation == null || !preview.Validation.IsValid)
            {
                pendingModification = null;
                pendingModificationDiff.Clear();
                statusMessage = RuleForgeLocalization.T(
                    "AI proposed a change, but Validator rejected it. Nothing was applied.",
                    "AI 提出了修改，但验证器没有通过。当前玩法未被更改。" );
                return;
            }

            pendingModification = preview.Challenge;
            pendingModificationPreview = preview;
            lastModificationPrompt = modificationProposal != null
                ? modificationProposal.PatchInstruction
                : designerPrompt;
            BuildChallengeDiff(
                modificationBase,
                pendingModification,
                pendingModificationDiff);
            statusMessage = preview.RequiresRepairConfirmation
                ? RuleForgeLocalization.T(
                    "Validator rejected the first patch. Review the AI-repaired legal version before applying it.",
                    "验证器拒绝了初始修改。请审核 AI 修复后的合法版本再应用。")
                : pendingModificationDiff.Count > 0
                ? RuleForgeLocalization.T(
                    "Review the proposed changes before applying them.",
                    "请先查看修改前后的差异，再决定是否应用。")
                : RuleForgeLocalization.T(
                    "AI returned no visible change. Nothing was applied.",
                    "AI 没有返回可见修改。当前玩法未被更改。" );
        }

        private void HandleGameplayProposal(
            AIGameplayResult<GameplayProposal> result)
        {
            if (result == null || !result.Success)
            {
                statusMessage = result != null
                    ? result.Error
                    : RuleForgeLocalization.T(
                        "AI returned no gameplay proposal.",
                        "AI 没有返回玩法提案。");
                return;
            }

            gameplayProposal = result.Value;
            showProposalRevision = false;
            proposalPenaltyRewardRatio = gameplayProposal.PenaltyRewardRatio;
            designerPrompt = string.Empty;
            statusMessage = gameplayProposal.CanGenerate &&
                            gameplayProposal.WithinVocabulary
                ? RuleForgeLocalization.T(
                    "Review the proposal, then choose Generate This.",
                    "请先审核 AI 的玩法提案，再选择“按这个方案生成”。")
                : RuleForgeLocalization.T(
                    "The designer needs one clarification before generation.",
                    "AI 策划需要你补充一个关键说明。" );
        }

        private void HandleModificationProposal(
            AIGameplayResult<GameplayModificationProposal> result)
        {
            if (!IsExpectedDraft(modificationRequestSignature))
            {
                modificationProposal = null;
                statusMessage = BuildStaleAIResultMessage();
                return;
            }

            if (result == null || !result.Success)
            {
                statusMessage = result != null
                    ? result.Error
                    : RuleForgeLocalization.T(
                        "AI returned no modification proposal.",
                        "AI 没有返回修改提案。");
                return;
            }

            modificationProposal = result.Value;
            if (modificationProposal.CanModify)
            {
                designerPrompt = string.Empty;
            }
            statusMessage = modificationProposal.CanModify
                ? RuleForgeLocalization.T(
                    "Review the smallest proposed change before building a patch.",
                    "请审核最小修改建议，再确认生成 Patch。")
                : RuleForgeLocalization.T(
                    "The request is ambiguous. Clarify it and analyze again.",
                    "这个修改存在歧义，请补充说明后重新分析。" );
        }

        private void HandleImprovementSet(
            AIGameplayResult<GameplayImprovementSet> result)
        {
            if (!IsExpectedDraft(improvementRequestSignature))
            {
                improvementSet = null;
                statusMessage = BuildStaleAIResultMessage();
                return;
            }

            if (result == null || !result.Success)
            {
                statusMessage = result != null
                    ? result.Error
                    : RuleForgeLocalization.T(
                        "AI returned no improvement suggestions.",
                        "AI 没有返回改进建议。");
                return;
            }

            improvementSet = result.Value;
            statusMessage = RuleForgeLocalization.T(
                "Choose an idea to explore. Nothing has been changed yet.",
                "选择一个方向继续分析；当前玩法尚未发生任何修改。" );
        }

        private bool IsExpectedDraft(string signature)
        {
            return !string.IsNullOrWhiteSpace(signature) &&
                   string.Equals(
                       signature,
                       BuildDraftSignature(),
                       StringComparison.Ordinal);
        }

        private static string BuildStaleAIResultMessage()
        {
            return RuleForgeLocalization.T(
                "The challenge changed while AI was working. The outdated result was discarded; analyze again.",
                "AI 处理期间玩法已发生变化。过期结果已丢弃，请重新分析。" );
        }

        private void DrawGenerationRepair()
        {
            if (pendingGenerationRepair == null)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "A VALID VERSION IS READY", "已有可用的修正版"), GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "The first version failed validation. This revised version passed; review it before applying.",
                "初版未通过验证；下面的修正版已通过，请确认后再应用。"));
            GUILayout.Label(pendingGenerationRepair.RepairSummary);
            DrawStringList(pendingGenerationRepair.RepairChanges);
            if (!string.IsNullOrWhiteSpace(
                    pendingGenerationRepair.RepairReasoning))
            {
                GUILayout.Label(pendingGenerationRepair.RepairReasoning);
            }

            showAIDiagnostic = GUILayout.Toggle(
                showAIDiagnostic,
                RuleForgeLocalization.T(
                    "Show original validation errors", "查看初版验证错误"),
                creatorSecondaryButtonStyle,
                GUILayout.Height(30f));
            if (showAIDiagnostic)
            {
                DrawStringList(pendingGenerationRepair.RepairSourceErrors);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "USE REPAIRED VERSION", "使用修正版"),
                    GUILayout.Height(38f)))
            {
                AIChallengePreview accepted = pendingGenerationRepair;
                pendingGenerationRepair = null;
                ClearPendingModification();
                lastGeneratedPrompt = gameplayProposalPrompt;
                LoadChallengeForEditing(accepted.Challenge);
                hasGeneratedDraftThisSession = true;
                hasUnplayedPreview = true;
                scrollPosition = Vector2.zero;
                gameplayProposal = null;
                designerPrompt = string.Empty;
                ruleEngine.ValidateChallenge(
                    accepted.Challenge, rewardStrength, penaltyStrength);
                CaptureCurrentValidation();
                MarkCurrentDraftValidated();
                statusMessage = RuleForgeLocalization.T(
                    "Repaired version accepted — Validator PASS.",
                    "已接受修正版 — 验证器通过。" );
            }
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "ADJUST AGAIN", "继续调整")))
            {
                pendingGenerationRepair = null;
                showProposalRevision = true;
                designerPrompt = RuleForgeLocalization.T(
                    "Propose another legal version while preserving the core idea.",
                    "保留核心想法，再提出一个符合系统限制的方案。" );
                statusMessage = RuleForgeLocalization.T(
                    "Edit the refinement above, then send it to the AI Designer.",
                    "请在上方编辑调整要求，再发送给 AI 策划。" );
            }
            if (GUILayout.Button(RuleForgeLocalization.T("CANCEL", "取消")))
            {
                pendingGenerationRepair = null;
                statusMessage = RuleForgeLocalization.T(
                    "Repair cancelled. Nothing was applied.",
                    "已取消修复，未应用任何内容。" );
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawStrengthSliders()
        {
            GameplayBalanceConfig config = ruleEngine.BalanceConfig;
            float minimum = config != null
                ? config.MinimumStrengthMultiplier
                : 0f;
            float maximum = config != null
                ? config.MaximumStrengthMultiplier
                : 2f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(RuleForgeLocalization.T(
                "Reward Strength", "奖励强度"), GUILayout.Width(150f));
            rewardStrength = GUILayout.HorizontalSlider(
                rewardStrength,
                minimum,
                maximum,
                GUILayout.Width(260f));
            GUILayout.Label(rewardStrength.ToString("0.00"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(RuleForgeLocalization.T(
                "Penalty Strength", "风险强度"), GUILayout.Width(150f));
            penaltyStrength = GUILayout.HorizontalSlider(
                penaltyStrength,
                minimum,
                maximum,
                GUILayout.Width(260f));
            GUILayout.Label(penaltyStrength.ToString("0.00"));
            GUILayout.EndHorizontal();
        }

        private string GetGoalTargetLabel()
        {
            if (string.Equals(goal, ChallengeGoalType.Survive.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Survive Seconds", "生存时间（秒）");
            }

            if (string.Equals(goal, ChallengeGoalType.KillCount.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Required Kills", "目标击杀数");
            }

            if (string.Equals(goal, ChallengeGoalType.Score.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return RuleForgeLocalization.T(
                    "Required Score", "目标分数");
            }

            return RuleForgeLocalization.T("Goal Target", "目标数值");
        }

        private bool DrawRule(RuleDraft draft, int ruleIndex)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(RuleForgeLocalization.T(
                $"RULE {ruleIndex + 1:00}", $"规则 {ruleIndex + 1:00}"),
                GUILayout.Width(90f));
            draft.Id = GUILayout.TextField(draft.Id ?? string.Empty);
            bool deleteRule = GUILayout.Button(RuleForgeLocalization.T(
                "Delete Rule", "删除规则"), GUILayout.Width(110f));
            GUILayout.EndHorizontal();

            DrawDropdown(
                $"rule-{ruleIndex}-trigger",
                RuleForgeLocalization.T("WHEN", "当"),
                draft.TriggerType,
                TriggerOptions,
                value => draft.TriggerType = value);

            for (int index = 0; index < draft.Conditions.Count; index++)
            {
                if (DrawCondition(draft.Conditions[index], ruleIndex, index))
                {
                    draft.Conditions.RemoveAt(index);
                    openDropdownId = string.Empty;
                    break;
                }
            }

            GameplayBalanceConfig balance = ruleEngine.BalanceConfig;
            bool canAddCondition = balance == null ||
                                   draft.Conditions.Count < balance.MaxConditionsPerRule;
            GUI.enabled = canAddCondition;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Add Condition", "添加条件"), GUILayout.Width(140f)))
            {
                draft.Conditions.Add(new ConditionDraft());
            }
            GUI.enabled = true;
            if (!canAddCondition && balance != null)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    $"Maximum Conditions per Rule is {balance.MaxConditionsPerRule}.",
                    $"每条规则最多 {balance.MaxConditionsPerRule} 个条件。"));
            }

            GUILayout.Label(RuleForgeLocalization.T("THEN", "则"));
            for (int effectIndex = 0;
                 effectIndex < draft.Effects.Count;
                 effectIndex++)
            {
                if (DrawEffect(
                        draft.Effects[effectIndex],
                        ruleIndex,
                        effectIndex))
                {
                    draft.Effects.RemoveAt(effectIndex);
                    openDropdownId = string.Empty;
                    break;
                }
            }

            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Add Effect", "添加效果"), GUILayout.Width(120f)))
            {
                AddEffect(draft);
            }

            GUILayout.EndVertical();
            return deleteRule;
        }

        private bool DrawCondition(ConditionDraft draft, int ruleIndex, int conditionIndex)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(RuleForgeLocalization.T("IF", "如果"), GUILayout.Width(100f));
            bool delete = GUILayout.Button(RuleForgeLocalization.T(
                "Delete Condition", "删除条件"), GUILayout.Width(140f));
            GUILayout.EndHorizontal();

            DrawDropdown(
                $"rule-{ruleIndex}-condition-{conditionIndex}-type",
                RuleForgeLocalization.T("Condition", "条件"),
                draft.Type,
                ConditionOptions,
                value => draft.SetType(value));

            if (string.Equals(draft.Type, RuleConditionType.EnemyType.ToString(),
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(draft.Type, RuleConditionType.EventValue.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                DrawDropdown(
                    $"rule-{ruleIndex}-condition-{conditionIndex}-comparison",
                    RuleForgeLocalization.T("Comparison", "比较"),
                    draft.Comparison,
                    string.Equals(
                        draft.Type,
                        RuleConditionType.EnemyType.ToString(),
                        StringComparison.OrdinalIgnoreCase)
                        ? TextComparisonOptions
                        : ComparisonOptions,
                    value => draft.Comparison = value);
            }

            if (string.Equals(draft.Type, RuleConditionType.RandomChance.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                DrawTextField(RuleForgeLocalization.T(
                    "Chance %", "概率 %"), ref draft.ValueText);
            }
            else if (!string.Equals(draft.Type, RuleConditionType.Always.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                DrawTextField(RuleForgeLocalization.T(
                    "Value", "数值"), ref draft.ValueText);
            }

            if (string.Equals(draft.Type, RuleConditionType.EnemyType.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                DrawDropdown(
                    $"rule-{ruleIndex}-condition-{conditionIndex}-enemy-type",
                    RuleForgeLocalization.T("Enemy Type", "敌人类型"),
                    draft.StringValue,
                    EnemyTypeOptions,
                    value => draft.StringValue = value);
            }

            GUILayout.EndVertical();
            return delete;
        }

        private bool DrawEffect(
            EffectDraft draft,
            int ruleIndex,
            int effectIndex)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            List<EffectDefinition> definitions = GetCreatorDefinitions();
            string[] optionIds = new string[definitions.Count];
            string[] optionLabels = new string[definitions.Count];
            for (int index = 0; index < definitions.Count; index++)
            {
                optionIds[index] = definitions[index].EffectId;
                optionLabels[index] =
                    RuleForgeLocalization.EffectName(
                        definitions[index].EffectId,
                        definitions[index].DisplayName) +
                    " (" +
                    RuleForgeLocalization.DataValue(
                        definitions[index].Polarity.ToString()) +
                    ")";
            }

            string currentLabel = draft.EffectId;
            if (ruleEngine.TryGetEffectDefinition(
                    draft.EffectId,
                    out EffectDefinition currentDefinition))
            {
                currentLabel =
                    RuleForgeLocalization.EffectName(
                        currentDefinition.EffectId,
                        currentDefinition.DisplayName) +
                    " (" +
                    RuleForgeLocalization.DataValue(
                        currentDefinition.Polarity.ToString()) +
                    ")";
            }

            DrawDropdown(
                $"rule-{ruleIndex}-effect-{effectIndex}",
                effectIndex == 0
                    ? RuleForgeLocalization.T("EFFECT", "效果")
                    : RuleForgeLocalization.T("AND", "并且"),
                currentLabel,
                optionLabels,
                selectedLabel =>
                {
                    int selectedIndex = Array.IndexOf(optionLabels, selectedLabel);
                    if (selectedIndex >= 0 &&
                        selectedIndex < optionIds.Length &&
                        ruleEngine.TryGetEffectDefinition(
                            optionIds[selectedIndex],
                            out EffectDefinition selectedDefinition))
                    {
                        draft.ApplyTemplate(selectedDefinition);
                    }
                });

            if (!draft.IsSpawnEnemy)
            {
                DrawTextField(
                    draft.IsPercent
                        ? RuleForgeLocalization.T("Value %", "数值 %")
                        : RuleForgeLocalization.T("Value", "数值"),
                    ref draft.ValueText);
            }
            if (draft.IsStatModifier)
            {
                bool scalingEnabled = GUILayout.Toggle(
                    draft.HasScaling,
                    RuleForgeLocalization.T(
                        "Scale with a runtime value", "按运行时数据动态变化"));
                if (scalingEnabled != draft.HasScaling)
                {
                    draft.SetScalingEnabled(scalingEnabled);
                }

                if (draft.HasScaling)
                {
                    DrawDropdown(
                        $"rule-{ruleIndex}-effect-{effectIndex}-scale-source",
                        RuleForgeLocalization.T("Scaling Source", "变化依据"),
                        draft.ScalingSource,
                        ScalingSourceOptions,
                        draft.SetScalingSource);
                    DrawTextField(RuleForgeLocalization.T(
                        "Source Minimum", "依据最小值"),
                        ref draft.ScalingSourceMinText);
                    DrawTextField(RuleForgeLocalization.T(
                        "Source Maximum", "依据最大值"),
                        ref draft.ScalingSourceMaxText);
                    DrawTextField(draft.IsPercent
                            ? RuleForgeLocalization.T(
                                "Effect Minimum %", "效果最小值 %")
                            : RuleForgeLocalization.T(
                                "Effect Minimum", "效果最小值"),
                        ref draft.ScalingMinText);
                    DrawTextField(draft.IsPercent
                            ? RuleForgeLocalization.T(
                                "Effect Maximum %", "效果最大值 %")
                            : RuleForgeLocalization.T(
                                "Effect Maximum", "效果最大值"),
                        ref draft.ScalingMaxText);
                }
                else
                {
                    DrawDropdown(
                        $"rule-{ruleIndex}-effect-{effectIndex}-stack",
                        RuleForgeLocalization.T("Stacking", "叠加方式"),
                        draft.StackMode,
                        StackModeOptions,
                        draft.SetStackMode);
                    if (string.Equals(draft.StackMode,
                            RuleStackMode.Stack.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        DrawTextField(RuleForgeLocalization.T(
                            "Max Stack", "最大层数"),
                            ref draft.MaxStacksText);
                    }
                }

                DrawTextField(RuleForgeLocalization.T(
                    "Duration sec (0 = persistent)",
                    "持续秒数（0 为永久）"), ref draft.DurationText);
            }

            bool delete = GUILayout.Button(RuleForgeLocalization.T(
                "Delete Effect", "删除效果"), GUILayout.Width(120f));
            GUILayout.EndVertical();
            return delete;
        }

        private void AddRule()
        {
            GameplayBalanceConfig config = ruleEngine.BalanceConfig;
            if (config != null && ruleDrafts.Count >= config.MaxRules)
            {
                statusMessage = RuleForgeLocalization.T(
                    $"Maximum Rule count is {config.MaxRules}.",
                    $"最多只能有 {config.MaxRules} 条规则。" );
                return;
            }

            List<EffectDefinition> definitions = GetCreatorDefinitions();
            if (definitions.Count == 0)
            {
                statusMessage = RuleForgeLocalization.T(
                    "No creator-enabled EffectDefinition is available.",
                    "没有可用于创建器的效果定义。" );
                return;
            }

            RuleDraft draft = new RuleDraft(
                CreateNextRuleId(),
                TriggerOptions.Length > 0
                    ? TriggerOptions[0]
                    : string.Empty);
            draft.Effects.Add(new EffectDraft(definitions[0]));
            ruleDrafts.Add(draft);
            statusMessage = string.Empty;
        }

        private string CreateNextRuleId()
        {
            while (true)
            {
                string candidate = $"manual_rule_{nextRuleNumber++:00}";
                bool exists = false;
                for (int index = 0; index < ruleDrafts.Count; index++)
                {
                    if (string.Equals(
                            ruleDrafts[index].Id,
                            candidate,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    return candidate;
                }
            }
        }

        private void AddEffect(RuleDraft draft)
        {
            GameplayBalanceConfig config = ruleEngine.BalanceConfig;
            if (config != null && draft.Effects.Count >= config.MaxEffectsPerRule)
            {
                statusMessage = RuleForgeLocalization.T(
                    $"Maximum Effects per Rule is {config.MaxEffectsPerRule}.",
                    $"每条规则最多 {config.MaxEffectsPerRule} 个效果。" );
                return;
            }

            List<EffectDefinition> definitions = GetCreatorDefinitions();
            if (definitions.Count == 0)
            {
                statusMessage = RuleForgeLocalization.T(
                    "No creator-enabled EffectDefinition is available.",
                    "没有可用于创建器的效果定义。" );
                return;
            }

            draft.Effects.Add(new EffectDraft(definitions[0]));
            statusMessage = string.Empty;
        }

        private List<EffectDefinition> GetCreatorDefinitions()
        {
            List<EffectDefinition> definitions = new List<EffectDefinition>();
            EffectCatalog catalog = ruleEngine != null
                ? ruleEngine.EffectCatalog
                : null;
            if (catalog == null)
            {
                return definitions;
            }

            IReadOnlyList<EffectDefinition> effects = catalog.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                EffectDefinition definition = effects[index];
                if (definition != null && definition.CreatorAvailable)
                {
                    definitions.Add(definition);
                }
            }

            return definitions;
        }

        private void ValidateAndPlay()
        {
            if (!TryBuildCandidate(out ChallengeSpec candidate, out string error))
            {
                statusMessage = error;
                return;
            }

            if (!ruleEngine.TryApplyRuntimeChallenge(
                    candidate,
                    rewardStrength,
                    penaltyStrength))
            {
                ValidationResult validation = ruleEngine.LastValidationResult;
                CaptureCurrentValidation();
                MarkCurrentDraftValidated();
                statusMessage = validation != null
                    ? RuleForgeLocalization.ValidationSummary(validation)
                    : RuleForgeLocalization.T(
                        "Challenge rejected by Validator.", "挑战被验证器拒绝。" );
                return;
            }

            ruleEngine.RestartChallenge();
            hasUnplayedPreview = false;
            RebuildFromActiveChallenge();
            SetOpen(false);
        }

        private void ValidatePreview()
        {
            if (!TryBuildCandidate(out ChallengeSpec candidate, out string error))
            {
                statusMessage = error;
                return;
            }

            ValidationResult validation = ruleEngine.ValidateChallenge(
                candidate,
                rewardStrength,
                penaltyStrength);
            CaptureCurrentValidation();
            MarkCurrentDraftValidated();
            statusMessage = validation != null && validation.IsValid
                ? RuleForgeLocalization.T(
                    "Preview validated. Review Reward/Risk, then use Validate & Play.",
                    "预览验证通过。请查看奖励/风险，再点击“验证并开始”。")
                : validation != null
                    ? RuleForgeLocalization.ValidationSummary(validation)
                    : RuleForgeLocalization.T(
                        "Preview could not be validated.",
                        "预览无法验证。" );
        }

        private bool TryBuildCandidate(
            out ChallengeSpec candidate,
            out string error)
        {
            candidate = null;
            if (string.IsNullOrWhiteSpace(challengeName) ||
                string.IsNullOrWhiteSpace(goal) ||
                string.IsNullOrWhiteSpace(weapon))
            {
                error = RuleForgeLocalization.T(
                    "Challenge Name, Goal, and Weapon are required.",
                    "挑战名称、目标和武器不能为空。" );
                return false;
            }

            if (!ChallengeCreatorDraftParser.TryParseFloat(
                    goalTargetText, out float goalTarget))
            {
                error = RuleForgeLocalization.T(
                    "Goal Target must be a number.", "目标数值必须是数字。" );
                return false;
            }

            if (!ChallengeCreatorDraftParser.TryParseFloat(
                    timeLimitText, out float timeLimit))
            {
                error = RuleForgeLocalization.T(
                    "Time Limit must be a number.", "限时秒数必须是数字。");
                return false;
            }

            float timeDamageScale = 0f;
            if (goal.StartsWith("TimeBank", StringComparison.OrdinalIgnoreCase) &&
                !ChallengeCreatorDraftParser.TryParseFloat(
                    timeDamageScaleText, out timeDamageScale))
            {
                error = RuleForgeLocalization.T(
                    "Seconds lost per damage point must be a number.",
                    "每点伤害扣除秒数必须是数字。");
                return false;
            }

            GameplayRule[] rules = new GameplayRule[ruleDrafts.Count];
            for (int index = 0; index < ruleDrafts.Count; index++)
            {
                if (!ruleDrafts[index].TryBuild(out rules[index], out error))
                {
                    return false;
                }
            }

            candidate = ChallengeSpec.Create(
                challengeId,
                challengeName.Trim(),
                goal.Trim(),
                goalTarget,
                weapon.Trim(),
                rules,
                timeLimit,
                timeDamageScale);
            error = string.Empty;
            return true;
        }

        private void DrawValidationFeedback()
        {
            string currentSignature = BuildDraftSignature();
            if (string.IsNullOrEmpty(currentSignature) ||
                !string.Equals(
                    currentSignature,
                    validatedDraftSignature,
                    StringComparison.Ordinal))
            {
                GUILayout.Space(8f);
                GUILayout.Label(RuleForgeLocalization.T(
                    "Validation: current edits not checked",
                    "验证：当前修改尚未检查"), GUI.skin.box);
                return;
            }

            ValidationResult validation = currentDraftValidation;
            if (validation == null)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.Label(
                validation.IsValid
                    ? RuleForgeLocalization.T("Validation: PASS", "验证：通过")
                    : RuleForgeLocalization.T("Validation: REJECTED", "验证：拒绝"),
                GUI.skin.box);
            for (int index = 0; index < validation.Errors.Count; index++)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    $"ERROR: {validation.Errors[index]}",
                    $"错误：{RuleForgeLocalization.ValidationMessage(validation.Errors[index])}"));
            }
            for (int index = 0; index < validation.Warnings.Count; index++)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    $"WARNING: {validation.Warnings[index]}",
                    $"警告：{RuleForgeLocalization.ValidationMessage(validation.Warnings[index])}"));
            }

            BalanceEvaluation balance = currentDraftBalance;
            if (balance != null)
            {
                string ratio = float.IsPositiveInfinity(balance.RewardPenaltyRatio)
                    ? "∞"
                    : balance.RewardPenaltyRatio.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);
                GUILayout.Label(RuleForgeLocalization.T(
                    $"Reward {balance.RewardScore:0.#}  Risk " +
                    $"{balance.PenaltyScore:0.#}  Ratio {ratio}  " +
                    $"{balance.Result}",
                    $"奖励 {balance.RewardScore:0.#}  风险 " +
                    $"{balance.PenaltyScore:0.#}  比例 {ratio}  " +
                    $"{RuleForgeLocalization.DataValue(balance.Result.ToString())}"));
            }
        }

        private void MarkCurrentDraftValidated()
        {
            validatedDraftSignature = BuildDraftSignature();
        }

        private void CaptureCurrentValidation()
        {
            currentDraftValidation = ruleEngine != null
                ? ruleEngine.LastValidationResult
                : null;
            currentDraftBalance = ruleEngine != null
                ? ruleEngine.LastBalanceEvaluation
                : null;
        }

        private string BuildDraftSignature()
        {
            if (!TryBuildCandidate(out ChallengeSpec candidate, out _))
            {
                return string.Empty;
            }

            return JsonUtility.ToJson(candidate) + "|" +
                   rewardStrength.ToString("R", CultureInfo.InvariantCulture) + "|" +
                   penaltyStrength.ToString("R", CultureInfo.InvariantCulture);
        }

        private void DrawDropdown(
            string dropdownId,
            string label,
            string currentValue,
            IReadOnlyList<string> options,
            Action<string> onSelected)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100f));
            if (GUILayout.Button(
                    string.IsNullOrWhiteSpace(currentValue)
                        ? RuleForgeLocalization.T("Select...", "请选择……")
                        : RuleForgeLocalization.DataValue(currentValue),
                    GUILayout.Width(300f)))
            {
                openDropdownId = openDropdownId == dropdownId
                    ? string.Empty
                    : dropdownId;
            }

            GUILayout.EndHorizontal();

            if (openDropdownId != dropdownId)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            for (int index = 0; index < options.Count; index++)
            {
                string option = options[index];
                if (GUILayout.Button(RuleForgeLocalization.DataValue(option)))
                {
                    onSelected(option);
                    openDropdownId = string.Empty;
                }
            }

            GUILayout.EndVertical();
        }

        private static void DrawTextField(string label, ref string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150f));
            value = GUILayout.TextField(value ?? string.Empty);
            GUILayout.EndHorizontal();
        }

    }
}
