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
        private string weapon = string.Empty;
        private float rewardStrength = 1f;
        private float penaltyStrength = 1f;
        private string statusMessage = string.Empty;
        private string validatedDraftSignature = string.Empty;
        private string openDropdownId = string.Empty;
        private int nextRuleNumber = 1;
        private string[] weaponOptions = Array.Empty<string>();
        private string creationPrompt = string.Empty;
        private string modificationPrompt = string.Empty;
        private string lastGeneratedPrompt = string.Empty;
        private string lastModificationPrompt = string.Empty;
        private string apiKeyDraft = string.Empty;
        private bool rememberApiKeyOnThisComputer;
        private string apiKeyStatus = string.Empty;
        private GameplayProposal gameplayProposal;
        private string gameplayProposalPrompt = string.Empty;
        private string proposalRefinement = string.Empty;
        private GameplayModificationProposal modificationProposal;
        private string modificationProposalPrompt = string.Empty;
        private string modificationRequestSignature = string.Empty;
        private string improvementRequestSignature = string.Empty;
        private string patchRequestSignature = string.Empty;
        private GameplayImprovementSet improvementSet;
        private AIChallengePreview pendingGenerationRepair;
        private bool showAdvancedEdit;
        private bool showDeveloperView;
        private ChallengeSpec pendingModification;
        private AIChallengePreview pendingModificationPreview;
        private ChallengeSpec modificationBase;
        private readonly List<string> pendingModificationDiff =
            new List<string>();
        private ValidationResult currentDraftValidation;
        private BalanceEvaluation currentDraftBalance;

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
        private static readonly string[] ModifySuggestionLabelsEnglish =
        {
            "Make it harder",
            "Increase reward",
            "Increase penalty",
            "Faster risk growth",
            "Reduce max stacks",
            "More randomness"
        };
        private static readonly string[] ModifySuggestionLabelsChinese =
        {
            "更难",
            "奖励更高",
            "惩罚更高",
            "风险成长更快",
            "减少最大层数",
            "增加随机性"
        };
        private static readonly string[] ModifySuggestionsChinese =
        {
            "让这个玩法更难，但保持核心玩法不变。",
            "提高奖励强度，其他内容不要改。",
            "提高惩罚强度，其他内容不要改。",
            "让风险随叠加层数增长得更快。",
            "减少所有可叠加效果的最大层数。",
            "在不改变核心目标的情况下增加一些随机性。"
        };
        private static readonly string[] ModifySuggestionsEnglish =
        {
            "Make this challenge harder without changing its core idea.",
            "Increase the reward and leave everything else unchanged.",
            "Increase the penalty and leave everything else unchanged.",
            "Make the risk grow faster as stacks increase.",
            "Reduce the maximum stacks of stackable effects.",
            "Add more randomness without changing the core goal."
        };

        private static readonly string[] TriggerOptions =
            GameplayEventCapabilities.GetRuntimeEventNames();
        private static readonly string[] GoalOptions =
            Enum.GetNames(typeof(ChallengeGoalType));
        private static readonly string[] ConditionOptions =
            Enum.GetNames(typeof(RuleConditionType));
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
            float width = Mathf.Min(780f, Screen.width - 40f);
            float height = Mathf.Max(280f, Screen.height - 40f);
            GUILayout.BeginArea(
                new Rect(20f, 20f, width, height),
                RuleForgeLocalization.T(
                    "RuleForge Challenge Creator — F2 to close",
                    "RuleForge 挑战创建器 — F2 关闭"),
                GUI.skin.window);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Back / Close",
                    "返回 / 关闭"), GUILayout.Width(120f)))
            {
                SetOpen(false);
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                RuleForgeGuiTheme.End(previousSkin);
                return;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(RuleForgeLocalization.ToggleLabel,
                    GUILayout.Width(90f)))
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
            GUILayout.EndHorizontal();
            DrawCreator();
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
                RebuildFromActiveChallenge();
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
            DrawAIGenerator();

            if (HasEditableChallenge())
            {
                GUILayout.Space(12f);
                DrawHumanReadableSummary();
                DrawPrimaryPlayButton();
                DrawImproveWithAI();
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
                GUI.skin.button,
                GUILayout.Height(30f));
            GUI.enabled = true;
            if (showAdvancedEdit && !hasPendingAIReview)
            {
                DrawAdvancedEditor();
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
                GUI.skin.button,
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

        private void DrawAIGenerator()
        {
            DrawFirstUseGuide();
            GUILayout.Space(8f);
            GUILayout.Label(RuleForgeLocalization.T(
                "Describe your challenge",
                "描述你想玩的玩法"), GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "You can describe: goal + reward + penalty + scaling.",
                "你可以描述：目标 + 奖励 + 惩罚 + 成长方式。"));
            DrawPromptArea(
                ref creationPrompt,
                RuleForgeLocalization.T(
                    "For example: Create a high-risk, high-reward survival mode. Every kill increases my damage by 5%, but enemy speed increases by 8%, up to 10 stacks.",
                    "例如：做一个高风险高收益的生存模式。每杀一个敌人，我的伤害提高 5%，但敌人的速度提高 8%，最多叠加 10 次。"),
                105f);

            GUILayout.Label(RuleForgeLocalization.T(
                "Try an example — clicking only fills the text box",
                "试试示例 — 点击只会填入输入框"));
            DrawPromptChips(
                CreationExampleLabelsEnglish,
                CreationExampleLabelsChinese,
                CreationExamplesEnglish,
                CreationExamplesChinese,
                value => creationPrompt = value,
                2);

            bool canGenerate = CanUseRealAI() &&
                               !aiController.IsBusy &&
                               !string.IsNullOrWhiteSpace(creationPrompt);
            GUI.enabled = canGenerate;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "ASK AI GAMEPLAY DESIGNER",
                    "让 AI 策划分析"), GUILayout.Height(48f)))
            {
                ClearPendingModification();
                gameplayProposal = null;
                pendingGenerationRepair = null;
                gameplayProposalPrompt = creationPrompt;
                statusMessage = RuleForgeLocalization.T(
                    "AI is understanding, completing, and critiquing your idea...",
                    "AI 正在理解、补全并检查你的玩法想法……");
                aiController.AnalyzeGameplay(
                    creationPrompt,
                    null,
                    string.Empty,
                    HandleGameplayProposal);
            }

            GUI.enabled = true;
            if (!CanUseRealAI())
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Real AI is not active and configured. Open Developer View to inspect or switch the provider; no mock result will be shown as AI output.",
                    "真实 AI 尚未启用并配置。可在“开发者视图”检查或切换 Provider；界面不会用模拟结果冒充 AI 输出。"),
                    GUI.skin.box);
            }

            DrawGameplayProposal();
            DrawGenerationRepair();
        }

        private void DrawGameplayProposal()
        {
            if (gameplayProposal == null)
            {
                return;
            }

            GUILayout.Space(10f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T("USER", "玩家"));
            GUILayout.Label(gameplayProposalPrompt, GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI GAMEPLAY DESIGNER", "AI 游戏策划"), GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T("Confidence: ", "理解信心：") +
                gameplayProposal.Confidence);
            GUILayout.Label(gameplayProposal.Summary);
            if (!string.IsNullOrWhiteSpace(gameplayProposal.SuggestedGoal))
            {
                GUILayout.Label(RuleForgeLocalization.T("Goal: ", "目标：") +
                    gameplayProposal.SuggestedGoal);
            }
            DrawStringList(gameplayProposal.SuggestedRules);
            if (!string.IsNullOrWhiteSpace(
                    gameplayProposal.DesignReasoningSummary))
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Design note: ", "设计说明：") +
                    gameplayProposal.DesignReasoningSummary);
            }
            DrawStringList(gameplayProposal.Warnings);

            if (!string.IsNullOrWhiteSpace(
                    gameplayProposal.ClarificationQuestion))
            {
                GUILayout.Label(gameplayProposal.ClarificationQuestion,
                    GUI.skin.box);
            }

            proposalRefinement = GUILayout.TextField(
                proposalRefinement ?? string.Empty);
            GUI.enabled = !aiController.IsBusy &&
                          !string.IsNullOrWhiteSpace(proposalRefinement);
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "SEND TO DESIGNER", "发送给 AI 策划")))
            {
                RefineGameplayProposal(proposalRefinement);
            }
            GUI.enabled = true;

            if (gameplayProposal.CanGenerate)
            {
                GUI.enabled = !aiController.IsBusy;
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

                GUILayout.BeginHorizontal();
                DrawProposalIntentButton(
                    RuleForgeLocalization.T("Make It Crazier", "更疯狂"),
                    RuleForgeLocalization.T(
                        "Make the proposal crazier while preserving its core idea.",
                        "保留核心想法，但让这个方案更疯狂。"));
                DrawProposalIntentButton(
                    RuleForgeLocalization.T("Change Reward", "调整奖励"),
                    RuleForgeLocalization.T(
                        "Propose a different reward while preserving the rest.",
                        "换一种奖励，其他设计尽量保持不变。"));
                DrawProposalIntentButton(
                    RuleForgeLocalization.T("Change Risk", "调整风险"),
                    RuleForgeLocalization.T(
                        "Propose a different risk while preserving the rest.",
                        "换一种风险，其他设计尽量保持不变。"));
                GUILayout.EndHorizontal();
                if (gameplayProposal.ActionSuggestions != null)
                {
                    for (int index = 0;
                         index < gameplayProposal.ActionSuggestions.Length;
                         index++)
                    {
                        string suggestion =
                            gameplayProposal.ActionSuggestions[index];
                        if (!string.IsNullOrWhiteSpace(suggestion))
                        {
                            DrawProposalIntentButton(suggestion, suggestion);
                        }
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawProposalIntentButton(string label, string intent)
        {
            GUI.enabled = !aiController.IsBusy;
            if (GUILayout.Button(label))
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
            proposalRefinement = string.Empty;
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

        private void DrawFirstUseGuide()
        {
            GUILayout.Label(RuleForgeLocalization.T(
                "Create a playable FPS challenge with one description",
                "用一句话创建可以立即试玩的 FPS 玩法"), GUI.skin.box);
            GUILayout.BeginHorizontal();
            DrawGuideStep("1", RuleForgeLocalization.T(
                "Describe", "描述"), RuleForgeLocalization.T(
                "Say what you want to play.", "用一句话描述玩法。"));
            DrawGuideStep("2", RuleForgeLocalization.T(
                "Generate", "生成"), RuleForgeLocalization.T(
                "AI builds the rules.", "AI 转换成游戏规则。"));
            DrawGuideStep("3", RuleForgeLocalization.T(
                "Play", "试玩"), RuleForgeLocalization.T(
                "Start immediately.", "立即开始试玩。"));
            DrawGuideStep("4", RuleForgeLocalization.T(
                "Modify", "修改"), RuleForgeLocalization.T(
                "Tell AI what to change.", "不满意就告诉 AI。"));
            GUILayout.EndHorizontal();
        }

        private static void DrawGuideStep(
            string number,
            string title,
            string description)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(150f));
            GUILayout.Label(number + ". " + title);
            GUILayout.Label(description);
            GUILayout.EndVertical();
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
                    textRect.x + 7f,
                    textRect.y + 5f,
                    textRect.width - 14f,
                    textRect.height - 10f),
                placeholder,
                placeholderStyle);
        }

        private static void DrawPromptChips(
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
                if (GUILayout.Button(label, GUILayout.Height(28f)))
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

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(candidate.DisplayName.ToUpperInvariant(), GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T("Goal", "目标"));
            GUILayout.Label(FormatGoal(candidate));

            List<string> rewards = new List<string>();
            List<string> penalties = new List<string>();
            List<string> neutral = new List<string>();
            int maximumStacks = 0;
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

                    maximumStacks = Mathf.Max(maximumStacks, effect.MaxStacks);
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

            DrawSummaryGroup(
                RuleForgeLocalization.T("Your Advantage", "你的优势"),
                rewards);
            DrawSummaryGroup(
                RuleForgeLocalization.T("Your Risk", "你的风险"),
                penalties);
            DrawSummaryGroup(
                RuleForgeLocalization.T("Other Rules", "其他规则"),
                neutral);
            if (maximumStacks > 1)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Maximum", "最大叠加"));
                GUILayout.Label(maximumStacks + RuleForgeLocalization.T(
                    " stacks", " 层"));
            }

            BalanceEvaluation balance = IsCurrentDraftValidated()
                ? currentDraftBalance
                : null;
            GUILayout.Label(RuleForgeLocalization.T("Risk Level", "风险等级"));
            GUILayout.Label(balance != null
                ? RuleForgeLocalization.DataValue(balance.Result.ToString())
                : RuleForgeLocalization.T("Not checked", "尚未检查"));
            if (balance != null)
            {
                string ratio = float.IsPositiveInfinity(
                        balance.RewardPenaltyRatio)
                    ? "∞"
                    : balance.RewardPenaltyRatio.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);
                GUILayout.Label(RuleForgeLocalization.T(
                    $"Reward Strength  {balance.RewardScore:0.#}",
                    $"奖励强度  {balance.RewardScore:0.#}"));
                GUILayout.Label(RuleForgeLocalization.T(
                    $"Risk Strength  {balance.PenaltyScore:0.#}",
                    $"风险强度  {balance.PenaltyScore:0.#}"));
                GUILayout.Label(RuleForgeLocalization.T(
                    $"Reward / Risk Ratio  {ratio}",
                    $"奖励 / 风险比例  {ratio}"));
                GUILayout.Label(RuleForgeLocalization.T(
                    "Estimated Difficulty  ",
                    "预估难度  ") +
                    RuleForgeLocalization.DataValue(
                        balance.Difficulty.ToString()));
                GUILayout.Label(RuleForgeLocalization.T(
                    "Growth Speed  ",
                    "成长速度  ") +
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

                    GUILayout.Label(RuleForgeLocalization.T(
                        "Tags  ",
                        "玩法标签  ") + string.Join(" · ", localizedTags));
                }
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
                    "Defeat " + target + " enemies",
                    "击败 " + target + " 个敌人");
            }

            return RuleForgeLocalization.T(
                "Reach " + target + " score",
                "达到 " + target + " 分");
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
                    "开始这个玩法"), GUILayout.Height(52f)))
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
            GUILayout.Space(14f);
            GUILayout.Label(RuleForgeLocalization.T(
                "What would you like to change?",
                "你想怎么修改这个玩法？"), GUI.skin.box);
            DrawPromptArea(
                ref modificationPrompt,
                RuleForgeLocalization.T(
                    "For example: The penalty is too light. Increase enemy speed growth by 50% and leave everything else unchanged.",
                    "例如：惩罚太轻了，把敌人速度成长提高 50%，其他不要改。"),
                78f);

            GUILayout.Label(RuleForgeLocalization.T(
                "Suggestions — clicking only fills the text box",
                "不知道怎么改？点击建议只会填入输入框"));
            DrawPromptChips(
                ModifySuggestionLabelsEnglish,
                ModifySuggestionLabelsChinese,
                ModifySuggestionsEnglish,
                ModifySuggestionsChinese,
                value => modificationPrompt = value,
                3);

            bool canModify = CanUseRealAI() &&
                             !aiController.IsBusy &&
                             !string.IsNullOrWhiteSpace(modificationPrompt) &&
                             pendingModification == null;
            GUI.enabled = canModify;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "ASK AI TO ANALYZE THE CHANGE",
                    "让 AI 分析修改建议"), GUILayout.Height(42f)))
            {
                if (TryBuildCandidate(
                        out ChallengeSpec current,
                        out string error))
                {
                    modificationBase = JsonUtility.FromJson<ChallengeSpec>(
                        JsonUtility.ToJson(current));
                    pendingModificationDiff.Clear();
                    modificationProposal = null;
                    modificationProposalPrompt = modificationPrompt;
                    modificationRequestSignature = BuildDraftSignature();
                    statusMessage = RuleForgeLocalization.T(
                        "AI is identifying the smallest relevant change...",
                        "AI 正在判断最相关的最小修改……");
                    aiController.AnalyzeModification(
                        modificationPrompt,
                        current,
                        HandleModificationProposal);
                }
                else
                {
                    statusMessage = error;
                }
            }

            GUI.enabled = true;
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

            modificationPrompt = intent;
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
            GUILayout.Label(RuleForgeLocalization.T("USER", "玩家"));
            GUILayout.Label(modificationProposalPrompt, GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "AI CHANGE PROPOSAL", "AI 修改提案"), GUI.skin.box);
            GUILayout.Label(modificationProposal.Summary);
            DrawStringList(modificationProposal.ProposedChanges);
            if (!string.IsNullOrWhiteSpace(
                    modificationProposal.DesignReasoningSummary))
            {
                GUILayout.Label(modificationProposal.DesignReasoningSummary);
            }
            DrawStringList(modificationProposal.Warnings);
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
                    "Switch Provider",
                    "切换 Provider"), GUILayout.Width(180f)))
            {
                aiController.SelectNextProvider();
                statusMessage = RuleForgeLocalization.T(
                    "Selected provider: ",
                    "已选择 Provider：") + aiController.ActiveProviderName;
            }

            GUI.enabled = true;
            DrawRuntimeApiKeySetup();
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

        private void DrawRuntimeApiKeySetup()
        {
            GUILayout.Space(8f);
            GUILayout.Label(RuleForgeLocalization.T(
                "OpenAI API Key — local testing only",
                "OpenAI API Key — 仅限本地试玩"), GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                "Use your own key. Never send it to the game author. Direct keys in client apps are not suitable for public distribution.",
                "请使用试玩者自己的 Key，不要发给游戏作者。客户端直连方式不适合公开发行。"));

            apiKeyDraft = GUILayout.PasswordField(
                apiKeyDraft ?? string.Empty,
                '•',
                GUILayout.Height(28f));
            rememberApiKeyOnThisComputer = GUILayout.Toggle(
                rememberApiKeyOnThisComputer,
                RuleForgeLocalization.T(
                    "Remember on this computer (not recommended on shared PCs)",
                    "记住到这台电脑（共用电脑不推荐）"));

            GUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrWhiteSpace(apiKeyDraft) &&
                          (aiController == null || !aiController.IsBusy);
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Use API Key",
                    "使用这个 API Key"), GUILayout.Height(30f)))
            {
                if (RuntimeOpenAICredentials.TrySet(
                        apiKeyDraft,
                        rememberApiKeyOnThisComputer,
                        out string error))
                {
                    apiKeyDraft = string.Empty;
                    aiController?.RefreshProviderSelection();
                    apiKeyStatus = RuleForgeLocalization.T(
                        "API Key is active. Generate will use the real OpenAI provider.",
                        "API Key 已启用，Generate 将使用真实 OpenAI Provider。" );
                }
                else
                {
                    apiKeyStatus = error;
                }
            }

            GUI.enabled = RuntimeOpenAICredentials.HasSessionKey ||
                          RuntimeOpenAICredentials.HasSavedKey;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Clear Local Key",
                    "清除本机 Key"), GUILayout.Height(30f)))
            {
                RuntimeOpenAICredentials.Clear();
                apiKeyDraft = string.Empty;
                aiController?.RefreshProviderSelection();
                apiKeyStatus = RuleForgeLocalization.T(
                    "The locally entered API Key was cleared.",
                    "已清除本机输入的 API Key。" );
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(
                RuntimeOpenAICredentials.HasSessionKey
                    ? RuleForgeLocalization.T(
                        RuntimeOpenAICredentials.HasSavedKey
                            ? "Local API Key status: Active and remembered"
                            : "Local API Key status: Active for this session",
                        RuntimeOpenAICredentials.HasSavedKey
                            ? "本机 API Key 状态：已启用并记住"
                            : "本机 API Key 状态：仅本次运行启用")
                    : RuleForgeLocalization.T(
                        "Local API Key status: Not configured",
                        "本机 API Key 状态：未配置"));
            if (!string.IsNullOrWhiteSpace(apiKeyStatus))
            {
                GUILayout.Label(apiKeyStatus, GUI.skin.box);
            }
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
                    return aiController.ActiveProviderIsConfigured
                        ? RuleForgeLocalization.T(
                            "Real AI Provider Active", "真实 AI Provider 已启用")
                        : RuleForgeLocalization.T(
                            "Real AI Provider Not Configured", "真实 AI Provider 未配置");
                default:
                    return RuleForgeLocalization.T("AI Not Connected", "AI 未连接");
            }
        }

        private void HandleAIGenerationPreview(AIChallengePreview preview)
        {
            if (preview == null || !preview.Success)
            {
                statusMessage = preview != null
                    ? preview.Error
                    : RuleForgeLocalization.T(
                        "AI provider returned no preview.",
                        "AI Provider 没有返回预览。" );
                return;
            }

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
                statusMessage = preview != null
                    ? preview.Error
                    : RuleForgeLocalization.T(
                        "AI provider returned no modification preview.",
                        "AI Provider 没有返回修改预览。" );
                return;
            }

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
                : modificationPrompt;
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
            statusMessage = gameplayProposal.CanGenerate
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
                "VALIDATOR REPAIR PROPOSAL", "验证器修复提案"), GUI.skin.box);
            DrawStringList(pendingGenerationRepair.RepairSourceErrors);
            GUILayout.Label(pendingGenerationRepair.RepairSummary);
            DrawStringList(pendingGenerationRepair.RepairChanges);
            if (!string.IsNullOrWhiteSpace(
                    pendingGenerationRepair.RepairReasoning))
            {
                GUILayout.Label(pendingGenerationRepair.RepairReasoning);
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
                proposalRefinement = RuleForgeLocalization.T(
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

            DrawTextField(
                draft.IsPercent
                    ? RuleForgeLocalization.T("Value %", "数值 %")
                    : RuleForgeLocalization.T("Value", "数值"),
                ref draft.ValueText);
            DrawTextField(RuleForgeLocalization.T(
                "Max Stack", "最大层数"), ref draft.MaxStacksText);
            DrawTextField(RuleForgeLocalization.T(
                "Duration sec", "持续秒数"), ref draft.DurationText);
            if (draft.HasScaling)
            {
                DrawTextField(RuleForgeLocalization.T(
                    "Scaling Min %", "缩放最小值 %"), ref draft.ScalingMinText);
                DrawTextField(RuleForgeLocalization.T(
                    "Scaling Max %", "缩放最大值 %"), ref draft.ScalingMaxText);
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
            RebuildFromActiveChallenge();
            statusMessage = RuleForgeLocalization.T(
                "Challenge validated and restarted. Press F2 to play.",
                "挑战已验证并重启。按 F2 返回创建器。" );
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

            if (!TryParseFloat(goalTargetText, out float goalTarget))
            {
                error = RuleForgeLocalization.T(
                    "Goal Target must be a number.", "目标数值必须是数字。" );
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
                rules);
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

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(
                       text,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out value) ||
                   float.TryParse(
                       text,
                       NumberStyles.Float,
                       CultureInfo.CurrentCulture,
                       out value);
        }

        private sealed class RuleDraft
        {
            public RuleDraft(GameplayRule rule, int fallbackNumber)
            {
                Id = string.IsNullOrWhiteSpace(rule.Id)
                    ? $"manual_rule_{fallbackNumber:00}"
                    : rule.Id;
                TriggerType = rule.Trigger != null &&
                              !string.IsNullOrWhiteSpace(rule.Trigger.Type)
                    ? rule.Trigger.Type
                    : TriggerOptions.Length > 0
                        ? TriggerOptions[0]
                        : string.Empty;
                RuleCondition[] conditions = rule.Conditions;
                for (int index = 0; index < conditions.Length; index++)
                {
                    if (conditions[index] != null)
                    {
                        Conditions.Add(new ConditionDraft(conditions[index]));
                    }
                }
                RuleEffect[] effects = rule.Effects;
                for (int index = 0; index < effects.Length; index++)
                {
                    if (effects[index] != null)
                    {
                        Effects.Add(new EffectDraft(effects[index]));
                    }
                }
            }

            public RuleDraft(string id, string triggerType)
            {
                Id = id;
                TriggerType = triggerType;
            }

            public string Id;
            public string TriggerType;
            public List<ConditionDraft> Conditions { get; } =
                new List<ConditionDraft>();
            public List<EffectDraft> Effects { get; } =
                new List<EffectDraft>();

            public bool TryBuild(out GameplayRule rule, out string error)
            {
                RuleCondition[] conditions = new RuleCondition[Conditions.Count];
                for (int index = 0; index < Conditions.Count; index++)
                {
                    if (!Conditions[index].TryBuild(out conditions[index], out error))
                    {
                        rule = null;
                        return false;
                    }
                }

                RuleEffect[] effects = new RuleEffect[Effects.Count];
                for (int index = 0; index < Effects.Count; index++)
                {
                    if (!Effects[index].TryBuild(out effects[index], out error))
                    {
                        rule = null;
                        return false;
                    }
                }

                rule = GameplayRule.Create(
                    Id,
                    TriggerType,
                    conditions,
                    effects);
                error = string.Empty;
                return true;
            }
        }

        private sealed class ConditionDraft
        {
            public ConditionDraft()
            {
                Type = RuleConditionType.Always.ToString();
                Comparison = RuleComparison.Equals.ToString();
                ValueText = "0";
                StringValue = string.Empty;
            }

            public ConditionDraft(RuleCondition condition)
            {
                Type = string.IsNullOrWhiteSpace(condition.Type)
                    ? RuleConditionType.Always.ToString()
                    : condition.Type;
                Comparison = condition.Comparison;
                ValueText = string.Equals(
                        Type,
                        RuleConditionType.RandomChance.ToString(),
                        StringComparison.OrdinalIgnoreCase)
                    ? (condition.Value * 100f).ToString("0.###", CultureInfo.InvariantCulture)
                    : condition.Value.ToString("0.###", CultureInfo.InvariantCulture);
                StringValue = condition.StringValue;
            }

            public string Type;
            public string Comparison;
            public string ValueText;
            public string StringValue;

            public void SetType(string type)
            {
                Type = type;
                bool usesComparison =
                    string.Equals(
                        Type,
                        RuleConditionType.EnemyType.ToString(),
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        Type,
                        RuleConditionType.EventValue.ToString(),
                        StringComparison.OrdinalIgnoreCase);
                if (!usesComparison)
                {
                    Comparison = string.Empty;
                    StringValue = string.Empty;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Comparison))
                    {
                        Comparison = RuleComparison.Equals.ToString();
                    }

                    if (string.Equals(
                            Type,
                            RuleConditionType.EnemyType.ToString(),
                            StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(StringValue))
                    {
                        StringValue = EnemyTypeOptions[0];
                    }
                    else if (string.Equals(
                                 Type,
                                 RuleConditionType.EventValue.ToString(),
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        StringValue = string.Empty;
                    }
                }
            }

            public bool TryBuild(out RuleCondition condition, out string error)
            {
                float value = 0f;
                if (!string.Equals(Type, RuleConditionType.Always.ToString(),
                        StringComparison.OrdinalIgnoreCase) &&
                    !TryParseFloat(ValueText, out value))
                {
                    condition = null;
                    error = RuleForgeLocalization.T(
                        $"Invalid condition value in {Type}.",
                        $"条件 {RuleForgeLocalization.DataValue(Type)} 的数值无效。" );
                    return false;
                }

                if (string.Equals(Type, RuleConditionType.RandomChance.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    value /= 100f;
                }

                condition = RuleCondition.Create(
                    Type,
                    string.Equals(Type, RuleConditionType.Always.ToString(),
                            StringComparison.OrdinalIgnoreCase)
                        ? string.Empty
                        : Comparison,
                    value,
                    StringValue);
                error = string.Empty;
                return true;
            }
        }

        private sealed class EffectDraft
        {
            private string kind;
            private string target;
            private string statId;
            private string operation;
            private string stringValue;
            private string stackMode;
            private float duration;
            private bool hasScaling;
            private string scalingSource;
            private string scalingMode;
            private float scalingSourceMin;
            private float scalingSourceMax;

            public EffectDraft(RuleEffect effect)
            {
                EffectId = effect.EffectId;
                kind = effect.Kind;
                target = effect.Target;
                statId = effect.StatId;
                operation = effect.Operation;
                stringValue = effect.StringValue;
                stackMode = effect.StackMode;
                duration = effect.Duration;
                ValueText = FormatValue(effect.Value, IsPercent);
                MaxStacksText = effect.MaxStacks.ToString(
                    CultureInfo.InvariantCulture);
                DurationText = duration.ToString(
                    "0.###",
                    CultureInfo.InvariantCulture);

                RuleScaling scaling = effect.Scaling;
                hasScaling = scaling != null &&
                             !string.IsNullOrWhiteSpace(scaling.Source);
                if (hasScaling)
                {
                    scalingSource = scaling.Source;
                    scalingMode = scaling.Mode;
                    scalingSourceMin = scaling.SourceMin;
                    scalingSourceMax = scaling.SourceMax;
                    ScalingMinText = FormatValue(scaling.EffectMin, true);
                    ScalingMaxText = FormatValue(scaling.EffectMax, true);
                }
            }

            public EffectDraft(EffectDefinition definition)
            {
                ApplyTemplate(definition);
            }

            public string EffectId { get; private set; }
            public bool IsPercent => string.Equals(
                operation,
                StatModifierOperation.AddPercent.ToString(),
                StringComparison.OrdinalIgnoreCase);
            public bool HasScaling => hasScaling;
            public string ValueText = "0";
            public string MaxStacksText = "1";
            public string DurationText = "0";
            public string ScalingMinText = "0";
            public string ScalingMaxText = "0";

            public void ApplyTemplate(EffectDefinition definition)
            {
                EffectId = definition.EffectId;
                kind = definition.CreatorKind;
                target = definition.CreatorTarget;
                statId = definition.CreatorStatId;
                operation = definition.CreatorOperation;
                stringValue = definition.CreatorStringValue;
                stackMode = definition.CreatorStackMode;
                duration = 0f;
                hasScaling = false;
                scalingSource = string.Empty;
                scalingMode = string.Empty;
                scalingSourceMin = 0f;
                scalingSourceMax = 1f;
                ScalingMinText = "0";
                ScalingMaxText = "0";
                ValueText = FormatValue(
                    definition.CreatorDefaultValue,
                    IsPercent);
                MaxStacksText = definition.CreatorMaxStacks.ToString(
                    CultureInfo.InvariantCulture);
                DurationText = "0";
            }

            public bool TryBuild(out RuleEffect effect, out string error)
            {
                if (!TryParseFloat(ValueText, out float value) ||
                    !TryParseFloat(DurationText, out float parsedDuration) ||
                    !int.TryParse(
                        MaxStacksText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int maxStacks))
                {
                    effect = null;
                    error = RuleForgeLocalization.T(
                        $"Invalid Value, Max Stack, or Duration in '{EffectId}'.",
                        $"效果“{RuleForgeLocalization.EffectName(EffectId, EffectId)}”的数值、最大层数或持续时间无效。" );
                    return false;
                }

                if (IsPercent)
                {
                    value /= 100f;
                }

                RuleScaling scaling = null;
                if (hasScaling)
                {
                    if (!TryParseFloat(ScalingMinText, out float scalingMin) ||
                        !TryParseFloat(ScalingMaxText, out float scalingMax))
                    {
                        effect = null;
                        error = RuleForgeLocalization.T(
                            $"Invalid scaling values in '{EffectId}'.",
                            $"效果“{RuleForgeLocalization.EffectName(EffectId, EffectId)}”的缩放数值无效。" );
                        return false;
                    }

                    scaling = RuleScaling.Create(
                        scalingSource,
                        scalingMode,
                        scalingSourceMin,
                        scalingSourceMax,
                        scalingMin / 100f,
                        scalingMax / 100f);
                }

                effect = RuleEffect.Create(
                    EffectId,
                    kind,
                    target,
                    statId,
                    operation,
                    value,
                    stringValue,
                    stackMode,
                    maxStacks,
                    parsedDuration,
                    scaling);
                error = string.Empty;
                return true;
            }

            private static string FormatValue(float value, bool percent)
            {
                float displayed = percent ? value * 100f : value;
                return displayed.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }
}
