using System;
using System.Collections.Generic;
using System.Globalization;
using RuleForge.Config;
using RuleForge.DSL;
using RuleForge.Rules;
using RuleForge.Runtime.Stats;
using RuleForge.UI;
using RuleForge.Validation;
using UnityEngine;

namespace RuleForge.Debugging
{
    [DisallowMultipleComponent]
    public sealed class RuntimeTuningPanel : MonoBehaviour
    {
        [SerializeField] private RuleEngine ruleEngine;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private readonly List<RuleDraft> ruleDrafts = new List<RuleDraft>();
        private Vector2 scrollPosition;
        private bool isOpen;
        private string rewardMultiplierText = "1";
        private string penaltyMultiplierText = "1";
        private string statusMessage = string.Empty;
        private string validatedDraftSignature = string.Empty;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            ResolveRuleEngine();
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
            float panelHeight = Mathf.Max(240f, Screen.height - 40f);
            GUILayout.BeginArea(
                new Rect(20f, 20f, 640f, panelHeight),
                RuleForgeLocalization.T(
                    "RuleForge Runtime Tuning — F1 to close",
                    "RuleForge 运行时调节 — F1 关闭"),
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
            DrawChallengeEditor();
            GUILayout.Space(12f);
            DrawStatBreakdown();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            RuleForgeGuiTheme.End(previousSkin);
        }

        public void Configure(RuleEngine engine)
        {
            ruleEngine = engine;
        }

        private void SetOpen(bool shouldOpen)
        {
            if (shouldOpen)
            {
                RuntimePanelCoordinator.Open(this, () => SetOpen(false));
                ResolveRuleEngine();
                RebuildDrafts();
                RuntimeInputGate.SetBlocked(this, true);
            }
            else
            {
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
        }

        private void RebuildDrafts()
        {
            ruleDrafts.Clear();
            statusMessage = string.Empty;
            validatedDraftSignature = string.Empty;
            if (ruleEngine == null || ruleEngine.ActiveChallenge == null)
            {
                return;
            }

            rewardMultiplierText = FormatNumber(ruleEngine.RewardMultiplier);
            penaltyMultiplierText = FormatNumber(ruleEngine.PenaltyMultiplier);

            GameplayRule[] rules = ruleEngine.ActiveChallenge.Rules;
            for (int index = 0; index < rules.Length; index++)
            {
                if (rules[index] != null)
                {
                    ruleDrafts.Add(new RuleDraft(rules[index], index));
                }
            }

            ruleEngine.ValidateChallenge(
                ruleEngine.ActiveChallenge,
                ruleEngine.RewardMultiplier,
                ruleEngine.PenaltyMultiplier);
            MarkCurrentDraftValidated();
        }

        private void DrawChallengeEditor()
        {
            DrawValidationAndBalance();

            if (ruleEngine == null || ruleEngine.ActiveChallenge == null)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "No active ChallengeSpec is loaded.", "没有加载活动 ChallengeSpec。"));
                return;
            }

            GUILayout.Label(
                RuleForgeLocalization.T("Challenge: ", "挑战：") +
                ruleEngine.ActiveChallenge.DisplayName,
                GUI.skin.box);
            DrawTextFieldRow(
                RuleForgeLocalization.T("Reward Multiplier", "奖励倍率"),
                ref rewardMultiplierText,
                RuleForgeLocalization.T("1.0 = unchanged", "1.0 = 不变"));
            DrawTextFieldRow(
                RuleForgeLocalization.T("Penalty Multiplier", "风险倍率"),
                ref penaltyMultiplierText,
                RuleForgeLocalization.T("1.0 = unchanged", "1.0 = 不变"));

            for (int index = 0; index < ruleDrafts.Count; index++)
            {
                DrawRule(ruleDrafts[index]);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Validate Preview", "验证预览"), GUILayout.Height(28f)))
            {
                ValidatePreview();
            }
            if (GUILayout.Button(RuleForgeLocalization.T(
                    "Apply & Restart Challenge", "应用并重启挑战"), GUILayout.Height(32f)))
            {
                ApplyAndRestart();
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUILayout.Label(statusMessage, GUI.skin.box);
            }
        }

        private void DrawValidationAndBalance()
        {
            if (ruleEngine == null)
            {
                return;
            }

            GUILayout.Label(RuleForgeLocalization.T(
                "Validator + Risk / Reward", "验证器 + 风险 / 奖励"), GUI.skin.box);
            string currentSignature = BuildDraftSignature();
            if (string.IsNullOrEmpty(currentSignature) ||
                !string.Equals(
                    currentSignature,
                    validatedDraftSignature,
                    StringComparison.Ordinal))
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Validation: current edits not checked",
                    "验证：当前修改尚未检查"));
                GUILayout.Space(8f);
                return;
            }

            ValidationResult validation = ruleEngine.LastValidationResult;
            if (validation != null)
            {
                GUILayout.Label(
                    validation.IsValid
                        ? RuleForgeLocalization.T("Validation: PASS", "验证：通过")
                        : RuleForgeLocalization.T("Validation: REJECTED", "验证：拒绝"));

                IReadOnlyList<string> errors = validation.Errors;
                for (int index = 0; index < errors.Count; index++)
                {
                    GUILayout.Label(RuleForgeLocalization.T(
                        $"ERROR: {errors[index]}",
                        $"错误：{RuleForgeLocalization.ValidationMessage(errors[index])}"));
                }

                IReadOnlyList<string> warnings = validation.Warnings;
                for (int index = 0; index < warnings.Count; index++)
                {
                    GUILayout.Label(RuleForgeLocalization.T(
                        $"WARNING: {warnings[index]}",
                        $"警告：{RuleForgeLocalization.ValidationMessage(warnings[index])}"));
                }
            }

            BalanceEvaluation balance = ruleEngine.LastBalanceEvaluation;
            if (balance == null)
            {
                return;
            }

            float largestScore = Mathf.Max(
                1f,
                balance.RewardScore,
                balance.PenaltyScore);
            GUILayout.Label(
                RuleForgeLocalization.T("Reward ", "奖励 ") +
                $"{BuildScoreBar(balance.RewardScore, largestScore)} " +
                $"{balance.RewardScore:0.#}");
            GUILayout.Label(
                RuleForgeLocalization.T("Risk     ", "风险     ") +
                $"{BuildScoreBar(balance.PenaltyScore, largestScore)} " +
                $"{balance.PenaltyScore:0.#}");
            string ratio = float.IsPositiveInfinity(balance.RewardPenaltyRatio)
                ? "∞"
                : balance.RewardPenaltyRatio.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);
            GUILayout.Label(RuleForgeLocalization.T(
                $"Ratio: {ratio}  Result: {balance.Result}",
                $"比例：{ratio}  结果：{RuleForgeLocalization.DataValue(balance.Result.ToString())}"));
            GUILayout.Label(RuleForgeLocalization.T(
                $"Difficulty: {balance.Difficulty}  Growth: {balance.Growth} " +
                $"({balance.GrowthScore:0.##})",
                $"难度：{RuleForgeLocalization.DataValue(balance.Difficulty.ToString())}  " +
                $"成长：{RuleForgeLocalization.DataValue(balance.Growth.ToString())} " +
                $"({balance.GrowthScore:0.##})"));
            if (balance.GameplayTags.Count > 0)
            {
                string tags = string.Empty;
                for (int index = 0; index < balance.GameplayTags.Count; index++)
                {
                    if (index > 0)
                    {
                        tags += " · ";
                    }

                    tags += RuleForgeLocalization.DataValue(
                        balance.GameplayTags[index]);
                }

                GUILayout.Label(RuleForgeLocalization.T("Tags: ", "标签：") + tags);
            }
            GUILayout.Space(8f);
        }

        private void DrawRule(RuleDraft draft)
        {
            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(RuleForgeLocalization.T(
                $"RULE {draft.RuleIndex + 1:00} — {draft.Rule.Id}",
                $"规则 {draft.RuleIndex + 1:00} — {draft.Rule.Id}"));
            string trigger = draft.Rule.Trigger?.Type ?? "None";
            GUILayout.Label(RuleForgeLocalization.T("Trigger: ", "触发器：") +
                            RuleForgeLocalization.DataValue(trigger));

            for (int index = 0; index < draft.Conditions.Count; index++)
            {
                ConditionDraft condition = draft.Conditions[index];
                if (condition.IsProbability)
                {
                    DrawTextFieldRow(
                        RuleForgeLocalization.T("Probability %", "概率 %"),
                        ref condition.ValueText,
                        RuleForgeLocalization.T("30 = 30%", "30 表示 30%"));
                }
                else
                {
                    GUILayout.Label(
                        RuleForgeLocalization.T("Condition: ", "条件：") +
                        RuleForgeLocalization.DataValue(condition.Condition.Type) + " " +
                        RuleForgeLocalization.DataValue(condition.Condition.Comparison) + " " +
                        RuleForgeLocalization.DataValue(condition.Condition.StringValue));
                }
            }

            for (int index = 0; index < draft.Effects.Count; index++)
            {
                DrawEffect(draft.Effects[index]);
            }

            GUILayout.EndVertical();
        }

        private void DrawEffect(EffectDraft draft)
        {
            RuleEffect effect = draft.Effect;
            string displayName = effect.EffectId;
            string polarity = "Neutral";
            if (ruleEngine.TryGetEffectDefinition(
                    effect.EffectId,
                    out EffectDefinition definition))
            {
                displayName = definition.DisplayName;
                polarity = definition.Polarity.ToString();
            }

            GUILayout.Space(4f);
            GUILayout.Label(
                RuleForgeLocalization.DataValue(polarity) + ": " +
                RuleForgeLocalization.EffectName(effect.EffectId, displayName));
            if (draft.IsPercentValue)
            {
                DrawTextFieldRow(
                    RuleForgeLocalization.T("Value %", "数值 %"),
                    ref draft.ValueText,
                    RuleForgeLocalization.T("20 = 20%", "20 表示 20%"));
            }
            else
            {
                DrawTextFieldRow(RuleForgeLocalization.T("Value", "数值"),
                    ref draft.ValueText, string.Empty);
            }

            DrawTextFieldRow(RuleForgeLocalization.T("Max Stacks", "最大层数"),
                ref draft.MaxStacksText, string.Empty);
            DrawTextFieldRow(RuleForgeLocalization.T("Duration (sec)", "持续时间（秒）"),
                ref draft.DurationText, string.Empty);

            if (effect.Scaling != null)
            {
                DrawTextFieldRow(
                    RuleForgeLocalization.T("Scaling Effect Min %", "缩放效果最小值 %"),
                    ref draft.ScalingMinText,
                    string.Empty);
                DrawTextFieldRow(
                    RuleForgeLocalization.T("Scaling Effect Max %", "缩放效果最大值 %"),
                    ref draft.ScalingMaxText,
                    string.Empty);
            }
        }

        private void DrawStatBreakdown()
        {
            GUILayout.Label(RuleForgeLocalization.T(
                "Stat Breakdown", "属性分解"), GUI.skin.box);
            if (ruleEngine == null)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "RuleEngine is not available.", "RuleEngine 不可用。"));
                return;
            }

            if (ruleEngine.WeaponService != null &&
                ruleEngine.WeaponService.TryGetStat(
                    RuntimeStatId.WeaponDamage,
                    out RuntimeStat weaponDamage))
            {
                DrawStat(RuleForgeLocalization.T("Player Damage", "玩家伤害"), weaponDamage);
            }

            if (ruleEngine.EnemyService != null &&
                ruleEngine.EnemyService.TryGetRepresentativeStat(
                    RuntimeStatId.EnemyMoveSpeed,
                    out RuntimeStat enemyMoveSpeed))
            {
                DrawStat(RuleForgeLocalization.T(
                    "Enemy Move Speed", "敌人移动速度"), enemyMoveSpeed);
            }
            else
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Enemy Move Speed: waiting for an active enemy.",
                    "敌人移动速度：等待活动敌人。"));
            }
        }

        private static void DrawStat(string label, RuntimeStat stat)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(label);
            GUILayout.Label(RuleForgeLocalization.T("Base: ", "基础值：") +
                            $"{stat.BaseValue:0.###}");
            IReadOnlyList<StatModifier> modifiers = stat.Modifiers;
            if (modifiers == null || modifiers.Count == 0)
            {
                GUILayout.Label(RuleForgeLocalization.T(
                    "Modifiers: none", "修正项：无"));
            }
            else
            {
                for (int index = 0; index < modifiers.Count; index++)
                {
                    StatModifier modifier = modifiers[index];
                    if (modifier != null)
                    {
                        GUILayout.Label(
                            $"{FormatModifier(modifier)}  [{modifier.Source}]");
                    }
                }
            }

            GUILayout.Label(RuleForgeLocalization.T("Final: ", "最终值：") +
                            $"{stat.FinalValue:0.###}");
            GUILayout.EndVertical();
        }

        private void ApplyAndRestart()
        {
            if (!TryBuildCandidate(
                    out ChallengeSpec candidate,
                    out float rewardMultiplier,
                    out float penaltyMultiplier,
                    out string error))
            {
                statusMessage = error;
                return;
            }

            if (!ruleEngine.TryApplyRuntimeChallenge(
                    candidate,
                    rewardMultiplier,
                    penaltyMultiplier))
            {
                ValidationResult validation = ruleEngine.LastValidationResult;
                statusMessage = validation != null
                    ? RuleForgeLocalization.ValidationSummary(validation)
                    : RuleForgeLocalization.T(
                        "Challenge rejected by Validator.", "挑战被验证器拒绝。" );
                return;
            }

            ruleEngine.RestartChallenge();
            RebuildDrafts();
            statusMessage = RuleForgeLocalization.T(
                "Challenge restarted with the edited runtime values.",
                "挑战已使用修改后的运行时数值重新开始。" );
        }

        private void ValidatePreview()
        {
            if (!TryBuildCandidate(
                    out ChallengeSpec candidate,
                    out float rewardMultiplier,
                    out float penaltyMultiplier,
                    out string error))
            {
                statusMessage = error;
                return;
            }

            ValidationResult validation = ruleEngine.ValidateChallenge(
                candidate,
                rewardMultiplier,
                penaltyMultiplier);
            MarkCurrentDraftValidated();
            statusMessage = validation != null && validation.IsValid
                ? RuleForgeLocalization.T(
                    "Preview validated. Runtime has not been changed.",
                    "预览验证通过，运行时尚未修改。")
                : validation != null
                    ? RuleForgeLocalization.ValidationSummary(validation)
                    : RuleForgeLocalization.T(
                        "Preview could not be validated.", "预览无法验证。" );
        }

        private bool TryBuildCandidate(
            out ChallengeSpec candidate,
            out float rewardMultiplier,
            out float penaltyMultiplier,
            out string error)
        {
            candidate = null;
            rewardMultiplier = 0f;
            penaltyMultiplier = 0f;
            if (ruleEngine == null || ruleEngine.ActiveChallenge == null)
            {
                error = RuleForgeLocalization.T(
                    "No active ChallengeSpec is loaded.", "没有加载活动 ChallengeSpec。" );
                return false;
            }

            if (!TryParseFloat(rewardMultiplierText, out rewardMultiplier) ||
                !TryParseFloat(penaltyMultiplierText, out penaltyMultiplier))
            {
                error = RuleForgeLocalization.T(
                    "Reward/Penalty multiplier is not a valid number.",
                    "奖励/风险倍率不是有效数字。" );
                return false;
            }

            if (!IsFinite(rewardMultiplier) || !IsFinite(penaltyMultiplier))
            {
                error = RuleForgeLocalization.T(
                    "Reward/Penalty multiplier must be a finite number.",
                    "奖励/风险倍率必须是有限数值。" );
                return false;
            }

            GameplayBalanceConfig balance = ruleEngine.BalanceConfig;
            if (balance != null &&
                (rewardMultiplier < balance.MinimumStrengthMultiplier ||
                 rewardMultiplier > balance.MaximumStrengthMultiplier ||
                 penaltyMultiplier < balance.MinimumStrengthMultiplier ||
                 penaltyMultiplier > balance.MaximumStrengthMultiplier))
            {
                error = RuleForgeLocalization.T(
                    $"Reward/Penalty multiplier must be between " +
                    $"{balance.MinimumStrengthMultiplier:0.###} and " +
                    $"{balance.MaximumStrengthMultiplier:0.###}.",
                    $"奖励/风险倍率必须位于 " +
                    $"{balance.MinimumStrengthMultiplier:0.###} 到 " +
                    $"{balance.MaximumStrengthMultiplier:0.###} 之间。" );
                return false;
            }

            for (int index = 0; index < ruleDrafts.Count; index++)
            {
                if (!ruleDrafts[index].TryParse(out error))
                {
                    return false;
                }
            }

            try
            {
                string json = JsonUtility.ToJson(ruleEngine.ActiveChallenge);
                candidate = JsonUtility.FromJson<ChallengeSpec>(json);
            }
            catch (ArgumentException exception)
            {
                error = RuleForgeLocalization.T(
                    $"Could not create validation candidate: {exception.Message}",
                    $"无法创建验证候选：{exception.Message}" );
                return false;
            }

            if (candidate == null)
            {
                error = RuleForgeLocalization.T(
                    "Could not create a validation candidate.",
                    "无法创建验证候选。" );
                return false;
            }

            for (int index = 0; index < ruleDrafts.Count; index++)
            {
                ruleDrafts[index].Apply(candidate);
            }

            error = string.Empty;
            return true;
        }

        private void MarkCurrentDraftValidated()
        {
            validatedDraftSignature = BuildDraftSignature();
        }

        private string BuildDraftSignature()
        {
            if (!TryBuildCandidate(
                    out ChallengeSpec candidate,
                    out float rewardMultiplier,
                    out float penaltyMultiplier,
                    out _))
            {
                return string.Empty;
            }

            return JsonUtility.ToJson(candidate) + "|" +
                   rewardMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                   penaltyMultiplier.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void DrawTextFieldRow(
            string label,
            ref string value,
            string hint)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(180f));
            value = GUILayout.TextField(value, GUILayout.Width(100f));
            if (!string.IsNullOrWhiteSpace(hint))
            {
                GUILayout.Label(hint);
            }

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

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatModifier(StatModifier modifier)
        {
            switch (modifier.Operation)
            {
                case StatModifierOperation.AddFlat:
                    return $"{modifier.Value:+0.###;-0.###;0}";
                case StatModifierOperation.AddPercent:
                    return $"{modifier.Value * 100f:+0.##;-0.##;0}%";
                case StatModifierOperation.Multiply:
                    return $"×{modifier.Value:0.###}";
                default:
                    return modifier.Value.ToString("0.###");
            }
        }

        private static string BuildScoreBar(float score, float largestScore)
        {
            const int width = 16;
            int filled = Mathf.Clamp(
                Mathf.RoundToInt(score / largestScore * width),
                0,
                width);
            return new string('█', filled) + new string('·', width - filled);
        }

        private sealed class RuleDraft
        {
            public RuleDraft(GameplayRule rule, int ruleIndex)
            {
                Rule = rule;
                RuleIndex = ruleIndex;

                RuleCondition[] conditions = rule.Conditions;
                for (int index = 0; index < conditions.Length; index++)
                {
                    if (conditions[index] != null)
                    {
                        Conditions.Add(
                            new ConditionDraft(conditions[index], index));
                    }
                }

                RuleEffect[] effects = rule.Effects;
                for (int index = 0; index < effects.Length; index++)
                {
                    if (effects[index] != null)
                    {
                        Effects.Add(new EffectDraft(effects[index], index));
                    }
                }
            }

            public GameplayRule Rule { get; }
            public int RuleIndex { get; }
            public List<ConditionDraft> Conditions { get; } =
                new List<ConditionDraft>();
            public List<EffectDraft> Effects { get; } =
                new List<EffectDraft>();

            public bool TryParse(out string error)
            {
                for (int index = 0; index < Conditions.Count; index++)
                {
                    if (!Conditions[index].TryParse(out error))
                    {
                        return false;
                    }
                }

                for (int index = 0; index < Effects.Count; index++)
                {
                    if (!Effects[index].TryParse(out error))
                    {
                        return false;
                    }
                }

                error = string.Empty;
                return true;
            }

            public void Apply(ChallengeSpec challenge)
            {
                GameplayRule targetRule = challenge.Rules[RuleIndex];
                for (int index = 0; index < Conditions.Count; index++)
                {
                    Conditions[index].Apply(targetRule);
                }

                for (int index = 0; index < Effects.Count; index++)
                {
                    Effects[index].Apply(targetRule);
                }
            }
        }

        private sealed class ConditionDraft
        {
            private float parsedValue;

            public ConditionDraft(RuleCondition condition, int conditionIndex)
            {
                Condition = condition;
                ConditionIndex = conditionIndex;
                IsProbability = string.Equals(
                    condition.Type,
                    RuleConditionType.RandomChance.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                ValueText = IsProbability
                    ? FormatNumber(condition.Value * 100f)
                    : FormatNumber(condition.Value);
            }

            public RuleCondition Condition { get; }
            public int ConditionIndex { get; }
            public bool IsProbability { get; }
            public string ValueText;

            public bool TryParse(out string error)
            {
                if (!TryParseFloat(ValueText, out parsedValue))
                {
                    error = RuleForgeLocalization.T(
                        $"Invalid condition value in {Condition.Type}.",
                        $"条件 {RuleForgeLocalization.DataValue(Condition.Type)} 的数值无效。" );
                    return false;
                }

                if (IsProbability)
                {
                    parsedValue /= 100f;
                }

                error = string.Empty;
                return true;
            }

            public void Apply(GameplayRule targetRule)
            {
                targetRule.Conditions[ConditionIndex].SetValue(parsedValue);
            }
        }

        private sealed class EffectDraft
        {
            private float parsedValue;
            private int parsedMaxStacks;
            private float parsedDuration;
            private float parsedScalingMin;
            private float parsedScalingMax;

            public EffectDraft(RuleEffect effect, int effectIndex)
            {
                Effect = effect;
                EffectIndex = effectIndex;
                IsPercentValue = string.Equals(
                    effect.Operation,
                    StatModifierOperation.AddPercent.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                ValueText = FormatNumber(
                    IsPercentValue ? effect.Value * 100f : effect.Value);
                MaxStacksText = effect.MaxStacks.ToString(
                    CultureInfo.InvariantCulture);
                DurationText = FormatNumber(effect.Duration);
                if (effect.Scaling != null)
                {
                    ScalingMinText = FormatNumber(effect.Scaling.EffectMin * 100f);
                    ScalingMaxText = FormatNumber(effect.Scaling.EffectMax * 100f);
                }
            }

            public RuleEffect Effect { get; }
            public int EffectIndex { get; }
            public bool IsPercentValue { get; }
            public string ValueText;
            public string MaxStacksText;
            public string DurationText;
            public string ScalingMinText = "0";
            public string ScalingMaxText = "0";

            public bool TryParse(out string error)
            {
                if (!TryParseFloat(ValueText, out parsedValue) ||
                    !int.TryParse(
                        MaxStacksText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out parsedMaxStacks) ||
                    !TryParseFloat(DurationText, out parsedDuration))
                {
                    error = RuleForgeLocalization.T(
                        $"Invalid Value, Max Stacks, or Duration in {Effect.EffectId}.",
                        $"效果“{RuleForgeLocalization.EffectName(Effect.EffectId, Effect.EffectId)}”的数值、最大层数或持续时间无效。" );
                    return false;
                }

                if (IsPercentValue)
                {
                    parsedValue /= 100f;
                }

                if (Effect.Scaling != null)
                {
                    if (!TryParseFloat(ScalingMinText, out parsedScalingMin) ||
                        !TryParseFloat(ScalingMaxText, out parsedScalingMax))
                    {
                        error = RuleForgeLocalization.T(
                            $"Invalid scaling range in {Effect.EffectId}.",
                            $"效果“{RuleForgeLocalization.EffectName(Effect.EffectId, Effect.EffectId)}”的缩放范围无效。" );
                        return false;
                    }

                    parsedScalingMin /= 100f;
                    parsedScalingMax /= 100f;
                }

                error = string.Empty;
                return true;
            }

            public void Apply(GameplayRule targetRule)
            {
                RuleEffect targetEffect = targetRule.Effects[EffectIndex];
                targetEffect.SetValue(parsedValue);
                targetEffect.SetMaxStacks(parsedMaxStacks);
                targetEffect.SetDuration(parsedDuration);
                targetEffect.Scaling?.SetEffectRange(
                    parsedScalingMin,
                    parsedScalingMax);
            }
        }
    }
}
