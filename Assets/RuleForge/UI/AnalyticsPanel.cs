using System.Globalization;
using RuleForge.Analytics;
using UnityEngine;

namespace RuleForge.UI
{
    [DisallowMultipleComponent]
    public sealed class AnalyticsPanel : MonoBehaviour
    {
        [SerializeField] private AnalyticsRecorder recorder;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private bool visible;

        private Rect windowRect = new Rect(24f, 94f, 540f, 560f);
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;

        private void Awake()
        {
            ResolveRecorder();
        }

        private void OnEnable()
        {
            if (visible)
            {
                RuntimePanelCoordinator.Open(this, () => SetVisible(false));
                RuntimeInputGate.SetBlocked(this, true);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetVisible(!visible);
            }
        }

        private void OnDisable()
        {
            RuntimeInputGate.SetBlocked(this, false);
            RuntimePanelCoordinator.Close(this);
            visible = false;
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(16f, Screen.height - 28f, 220f, 22f),
                RuleForgeLocalization.T("F3  Analytics", "F3  数据分析"));
            if (!visible)
            {
                return;
            }

            EnsureStyles();
            windowRect = GUI.Window(
                GetInstanceID(),
                windowRect,
                DrawWindow,
                RuleForgeLocalization.T(
                    "RuleForge Analytics — REAL DATA ONLY",
                    "RuleForge 数据分析 — 仅真实数据"));
        }

        public void Configure(AnalyticsRecorder analyticsRecorder)
        {
            recorder = analyticsRecorder;
        }

        private void SetVisible(bool shouldShow)
        {
            if (shouldShow)
            {
                RuntimePanelCoordinator.Open(this, () => SetVisible(false));
                RuntimeInputGate.SetBlocked(this, true);
                recorder?.RefreshSummary();
            }
            else
            {
                RuntimeInputGate.SetBlocked(this, false);
                RuntimePanelCoordinator.Close(this);
            }

            visible = shouldShow;
        }

        private void DrawWindow(int windowId)
        {
            if (GUI.Button(new Rect(windowRect.width - 106f, 30f, 90f, 24f),
                    RuleForgeLocalization.ToggleLabel))
            {
                RuleForgeLocalization.Toggle();
            }

            if (recorder == null)
            {
                GUI.Label(new Rect(16f, 60f, 500f, 24f),
                    RuleForgeLocalization.T(
                        "AnalyticsRecorder is not configured.",
                        "AnalyticsRecorder 未配置。"));
                GUI.DragWindow();
                return;
            }

            AnalyticsSummary summary = recorder.LatestSummary ??
                                       new AnalyticsSummary();
            float y = 62f;
            DrawHeading(RuleForgeLocalization.T(
                "AI benchmark (Mock excluded)", "AI 基准统计（不含 Mock）"), ref y);
            DrawLine(
                RuleForgeLocalization.T(
                    $"Real generation samples: {summary.generationBenchmarkRequests}  " +
                    $"(real AI requests: {summary.benchmarkAIRequests}, " +
                    $"all: {summary.totalAIRequests})",
                    $"真实生成样本：{summary.generationBenchmarkRequests}  " +
                    $"（真实 AI 请求：{summary.benchmarkAIRequests}，" +
                    $"全部请求：{summary.totalAIRequests}）"),
                ref y);
            DrawLine(
                RuleForgeLocalization.T("First-pass success: ", "首次通过率：") + RateText(
                    summary.firstPassSuccesses,
                    summary.firstPassAttempts,
                    summary.firstPassGenerationSuccessRate),
                ref y);
            DrawLine(
                RuleForgeLocalization.T("Validation rejection: ", "验证拒绝率：") + RateText(
                    summary.validationRejections,
                    summary.parsedBenchmarkOutputs,
                    summary.validationRejectionRate),
                ref y);
            DrawLine(
                RuleForgeLocalization.T("Retry success: ", "重试成功率：") + RateText(
                    summary.retrySuccesses,
                    summary.retryAttempts,
                    summary.retrySuccessRate),
                ref y);
            DrawLine(
                summary.generationBenchmarkRequests > 0
                    ? RuleForgeLocalization.T(
                        $"Average latency: {summary.averageGenerationLatencyMs:0} ms",
                        $"平均延迟：{summary.averageGenerationLatencyMs:0} ms")
                    : RuleForgeLocalization.T("Average latency: N/A", "平均延迟：无数据"),
                ref y);

            y += 8f;
            DrawHeading(RuleForgeLocalization.T("Gameplay", "游戏过程"), ref y);
            DrawLine(
                RuleForgeLocalization.T(
                    $"Sessions: {summary.gameplaySessions}   Victories: {summary.victories}   ",
                    $"会话：{summary.gameplaySessions}   胜利：{summary.victories}   ") +
                (summary.gameplaySessions > 0
                    ? RuleForgeLocalization.T(
                        $"Win rate: {summary.victoryRate:P1}",
                        $"胜率：{summary.victoryRate:P1}")
                    : RuleForgeLocalization.T("Win rate: N/A", "胜率：无数据")),
                ref y);
            DrawLine(
                summary.gameplaySessions > 0
                    ? RuleForgeLocalization.T(
                        $"Average duration: {summary.averagePlayDurationSeconds:0.0} sec",
                        $"平均时长：{summary.averagePlayDurationSeconds:0.0} 秒")
                    : RuleForgeLocalization.T("Average duration: N/A", "平均时长：无数据"),
                ref y);
            DrawLine(
                RuleForgeLocalization.T(
                    $"Kills: {summary.totalKills}   Headshots: {summary.totalHeadshots}   " +
                    $"Rules triggered: {summary.totalRulesTriggered}",
                    $"击杀：{summary.totalKills}   爆头：{summary.totalHeadshots}   " +
                    $"规则触发：{summary.totalRulesTriggered}"),
                ref y);

            y += 8f;
            DrawHeading(RuleForgeLocalization.T("Current session", "当前会话"), ref y);
            DrawLine(
                recorder.SessionActive
                    ? $"{recorder.ActiveChallengeName}   " +
                      RuleForgeLocalization.T(
                          $"{recorder.CurrentPlayDuration:0.0} sec",
                          $"{recorder.CurrentPlayDuration:0.0} 秒")
                    : RuleForgeLocalization.T("No active session", "没有活动会话"),
                ref y);
            DrawLine(
                RuleForgeLocalization.T(
                    $"Kills {recorder.CurrentKills}   Headshots {recorder.CurrentHeadshots}   " +
                    $"Rule triggers {recorder.CurrentRulesTriggered}",
                    $"击杀 {recorder.CurrentKills}   爆头 {recorder.CurrentHeadshots}   " +
                    $"规则触发 {recorder.CurrentRulesTriggered}"),
                ref y);

            y += 8f;
            DrawHeading(RuleForgeLocalization.T("Evidence files", "证据文件"), ref y);
            GUI.TextField(
                new Rect(16f, y, 508f, 24f),
                recorder.OutputDirectory);
            y += 32f;
            if (GUI.Button(new Rect(16f, y, 150f, 28f),
                    RuleForgeLocalization.T("Refresh summary", "刷新统计")))
            {
                recorder.RefreshSummary();
            }

            if (GUI.Button(new Rect(176f, y, 170f, 28f),
                    RuleForgeLocalization.T("Copy data folder", "复制数据目录")))
            {
                GUIUtility.systemCopyBuffer = recorder.OutputDirectory;
            }

            y += 38f;
            GUI.Label(
                new Rect(16f, y, 508f, 46f),
                RuleForgeLocalization.T(
                    "Raw JSONL and CSV are append-only. Rates remain N/A until " +
                    "real provider or gameplay samples exist.",
                    "原始 JSONL 和 CSV 只追加不覆盖。没有真实 Provider 或游戏样本前，比例显示为无数据。"),
                labelStyle);
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 26f));
        }

        private void DrawHeading(string text, ref float y)
        {
            GUI.Label(new Rect(16f, y, 508f, 24f), text, titleStyle);
            y += 26f;
        }

        private void DrawLine(string text, ref float y)
        {
            GUI.Label(new Rect(20f, y, 500f, 22f), text, labelStyle);
            y += 23f;
        }

        private static string RateText(
            int numerator,
            int denominator,
            float rate)
        {
            return denominator > 0
                ? rate.ToString("P1", CultureInfo.InvariantCulture) +
                  $" ({numerator}/{denominator})"
                : RuleForgeLocalization.T(
                    "N/A (0 samples)", "无数据（0 个样本）");
        }

        private void ResolveRecorder()
        {
            recorder = recorder != null
                ? recorder
                : FindObjectOfType<AnalyticsRecorder>();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true
            };
        }
    }
}
