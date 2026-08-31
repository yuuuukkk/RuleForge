# Milestone 10 手动验收

Codex 不启动 Unity、不运行 PlayMode、不调用真实 AI。以下测试由玩家在 Unity Editor 内完成。

## 初始化

1. 退出 Play Mode。
2. 点击 `RuleForge > Setup Milestone 10 Analytics & Showcases`。
3. 等待 Unity 编译完成，确认 Console 没有红色错误。
4. 进入 Play Mode，按 `F3` 打开 Analytics 面板。
5. 没有真实样本时，成功率和延迟应显示 `N/A`，不能显示伪造结果。

## AI 数据

1. 按 `F2`，使用 Mock 运行一次 Generate。
2. 按 `F3`：All requests 应增加，但 Real generation samples 不应增加。
3. 如已配置真实 OpenAI Provider，执行 `MILESTONE10_PROMPT_TEST_SET.md`。
4. 每次真实 Generate 都应记录 Prompt、Generation Time、Parse Success、Validation Result、Rule Count、Reward Score、Penalty Score。
5. 对失败 Prompt 原样重试，确认 Retry 样本和 Retry success 更新。

## Gameplay 数据

1. 完成、失败、重开或退出一局。
2. F3 应记录 Challenge、Play Duration、Victory、Kills、Headshots、Rules Triggered。
3. 射击敌人碰撞体上部约 28% 区域，Headshots 应增加；命中下部不增加。
4. 达成最后一次击杀时，该击杀及同帧触发的 Rule 都应计入完成局。
5. F3 点击 `Copy data folder`，确认目录包含：
   - `ai_generation.jsonl`
   - `ai_generation.csv`
   - `gameplay_sessions.jsonl`
   - `gameplay_sessions.csv`
   - `analytics_summary.json`

## 官方 Showcase

每次切换前退出 Play Mode：

1. `RuleForge > Showcases > Load 01 Blood Pact`
   - 击杀触发 Damage +5%、Enemy Speed +8%，最多 10 层。
2. `RuleForge > Showcases > Load 02 Last Stand`
   - 生命越低，伤害越高，同时移动速度越低。
3. `RuleForge > Showcases > Load 03 Reload Gamble`
   - Reload 有 30% 概率生成 Runner；击杀 Runner 获得弹药。

录屏与作品集素材按 `PORTFOLIO_CAPTURE_CHECKLIST.md` 收集。不得修改原始日志来美化统计。

全部通过后回复：`Milestone 10 PASS`。

