# RuleForge V0.1 录屏与作品集取证清单

本清单不包含虚构数字。录屏、截图和最终指标均必须来自实际 Unity Gameplay 与真实 AI 请求。

## Showcase 01 — Blood Pact

- 录到击杀敌人。
- 录到 `RULE TRIGGERED`。
- 录到 Player Damage +5%、Enemy Move Speed +8%。
- 录到 Stack 层数增长并在 10 层停止。
- 同屏展示 Reward / Penalty 或 F1 风险收益信息。

## Showcase 02 — Last Stand

- 录到玩家生命下降。
- 录到伤害随 Missing HP 线性上升。
- 录到移动速度随 Missing HP 线性下降。
- 录到 F1 Runtime Stat 的 Base、Modifiers、Final。

## Showcase 03 — Reload Gamble

- 录到 Reload。
- 录到 30% 概率触发 Spawn Runner 的真实结果，不剪辑成“每次必出”。
- 录到击杀 Runner 后 Give Ammo。
- 录到 Probability、Risk/Reward Loop 和组合 Rule。

## AI Creator 闭环

- 输入自然语言 Prompt。
- 展示 Structured Preview。
- 展示 Validator 和 Reward/Penalty 结果。
- 玩家点击 `Validate & Play`。
- 使用 Modify Prompt 修改 Preview，再次由玩家确认并运行。

## Analytics 证据

- F3 面板截图必须显示真实样本数。
- 保存 `ai_generation.csv`、`gameplay_sessions.csv`、`analytics_summary.json`。
- 保留原始 JSONL，不删除失败或 Validator 拒绝记录。
- 明确说明 Mock 请求不进入真实 Provider 汇总。
- 样本不足时显示 N/A，不把 0 样本写成 0% 成功率。

## 建议最终素材

- 3 段 20–40 秒 Showcase 录像。
- 1 段 Prompt → Preview → Validate & Play → Modify → Replay 闭环录像。
- Creator、F1 Validator、F3 Analytics 各 1 张清晰截图。
- 一张只引用真实 `analytics_summary.json` 数值的结果表。

