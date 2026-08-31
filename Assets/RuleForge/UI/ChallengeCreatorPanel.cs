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
        private string aiPrompt = string.Empty;

        private static readonly string[] TriggerOptions =
            GameplayEventCapabilities.GetRuntimeEventNames();
        private static readonly string[] GoalOptions =
            Enum.GetNames(typeof(ChallengeGoalType));
        private static readonly string[] ConditionOptions =
            Enum.GetNames(typeof(RuleConditionType));
        private static readonly string[] ComparisonOptions =
            Enum.GetNames(typeof(RuleComparison));
        private static readonly string[] EnemyTypeOptions =
        {
            "Grunt",
            "Runner",
            "Tank"
        };

        public bool IsOpen => isOpen;

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
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(RuleForgeLocalization.ToggleLabel,
                    GUILayout.Width(90f)))
            {
                RuleForgeLocalization.Toggle();
                statusMessage = string.Empty;
            }
            GUILayout.EndHorizontal();
            DrawCreator();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public void Configure(RuleEngine engine)
        {
            ruleEngine = engine;
        }

        public void ConfigureAI(AIGameplayController controller)
        {
            aiController = controller;
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
            statusMessage = string.Empty;
            validatedDraftSignature = string.Empty;
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
            MarkCurrentDraftValidated();
        }

        private void LoadChallengeForEditing(ChallengeSpec challenge)
        {
            if (challenge == null)
            {
                return;
            }

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

            GUILayout.Space(8f);
            GUILayout.Label(RuleForgeLocalization.T(
                "Creator Preview", "创建预览"), GUI.skin.box);
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

            GUILayout.Space(8f);
            GUILayout.Label(RuleForgeLocalization.T(
                $"Rules ({ruleDrafts.Count})",
                $"规则（{ruleDrafts.Count}）"), GUI.skin.box);
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

            GUILayout.Space(8f);
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Validate Preview", "验证预览"), GUILayout.Height(32f)))
            {
                ValidatePreview();
            }
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Validate & Play", "验证并开始"), GUILayout.Height(38f)))
            {
                ValidateAndPlay();
            }

            DrawValidationFeedback();
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUILayout.Label(statusMessage, GUI.skin.box);
            }
        }

        private void DrawAIGenerator()
        {
            GUILayout.Label(RuleForgeLocalization.T(
                "AI Gameplay Generation", "AI 游戏玩法生成"), GUI.skin.box);
            if (aiController == null)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "AIGameplayController is not configured. Manual Creator remains available.",
                    "AIGameplayController 未配置，手动创建仍可用。"));
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(RuleForgeLocalization.T("Provider: ", "Provider：") +
                aiController.ActiveProviderName,
                GUILayout.Width(430f));
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Switch Provider", "切换 Provider"), GUILayout.Width(140f)))
            {
                aiController.SelectNextProvider();
                statusMessage =
                    RuleForgeLocalization.T(
                        "Selected ", "已选择 ") +
                    aiController.ActiveProviderName + ".";
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(RuleForgeLocalization.T(
                "AI status: ", "AI 状态：") + GetAIStatus(), GUI.skin.box);
            GUILayout.Label(
                aiController.ActiveProviderIsConfigured
                    ? RuleForgeLocalization.T(
                        "Provider configuration: Ready", "Provider 配置：就绪")
                    : RuleForgeLocalization.T(
                        "Provider configuration: Not configured", "Provider 配置：未配置"));
            GUILayout.Label(RuleForgeLocalization.T(
                "Describe a new challenge or the smallest requested change:",
                "描述新挑战，或描述最小的修改内容："));
            aiPrompt = GUILayout.TextArea(
                aiPrompt ?? string.Empty,
                GUILayout.MinHeight(70f));

            GUILayout.BeginHorizontal();
            GUI.enabled = !aiController.IsBusy;
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "AI Generate Preview", "AI 生成预览"), GUILayout.Height(34f)))
            {
                statusMessage = RuleForgeLocalization.T(
                    "Generating structured ChallengeSpec...",
                    "正在生成结构化 ChallengeSpec……");
                aiController.GenerateChallenge(aiPrompt, HandleAIPreview);
            }

            if (GUILayout.Button(RuleForgeLocalization.T(
                    "AI Modify Preview", "AI 修改预览"), GUILayout.Height(34f)))
            {
                if (TryBuildCandidate(out ChallengeSpec current, out string error))
                {
                    statusMessage = RuleForgeLocalization.T(
                        "Generating smallest ChallengePatch...",
                        "正在生成最小化 ChallengePatch……");
                    aiController.ModifyChallenge(
                        aiPrompt,
                        current,
                        HandleAIPreview);
                }
                else
                {
                    statusMessage = error;
                }
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(
                RuleForgeLocalization.T(
                    "AI output is preview-only. Review/edit it, then use Validate & Play.",
                    "AI 输出仅用于预览。请先审核/编辑，再点击“验证并开始”。"));
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

        private void HandleAIPreview(AIChallengePreview preview)
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

            LoadChallengeForEditing(preview.Challenge);
            ValidationResult currentValidation = ruleEngine.ValidateChallenge(
                preview.Challenge,
                rewardStrength,
                penaltyStrength);
            MarkCurrentDraftValidated();
            bool isValid = currentValidation != null &&
                           currentValidation.IsValid;
            statusMessage = isValid
                ? preview.Source + RuleForgeLocalization.T(
                    " — Validator PASS. Review/edit before Play.",
                    " — 验证器通过。请在开始前审核/编辑。")
                : preview.Source + RuleForgeLocalization.T(
                    " — Validator REJECTED. Edit the preview before Play.",
                    " — 验证器拒绝。请先编辑预览。" );
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
                    StringComparison.OrdinalIgnoreCase))
            {
                DrawDropdown(
                    $"rule-{ruleIndex}-condition-{conditionIndex}-comparison",
                    RuleForgeLocalization.T("Comparison", "比较"),
                    draft.Comparison,
                    ComparisonOptions,
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

            ValidationResult validation = ruleEngine.LastValidationResult;
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

            BalanceEvaluation balance = ruleEngine.LastBalanceEvaluation;
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
                if (!string.Equals(Type, RuleConditionType.EnemyType.ToString(),
                        StringComparison.OrdinalIgnoreCase))
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

                    if (string.IsNullOrWhiteSpace(StringValue))
                    {
                        StringValue = EnemyTypeOptions[0];
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
