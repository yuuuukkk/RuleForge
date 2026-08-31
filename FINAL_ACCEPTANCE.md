# RuleForge V0.1 最终手动验收

Codex 不启动 Unity、不进入 PlayMode、不调用真实 AI。以下运行验证由开发者在 Unity Editor 中完成。

## 1. 初始化与编译

1. 退出 Play Mode，打开 `Assets/Scenes/Arena.unity`。
2. 运行 `RuleForge > Setup Milestone 10 Analytics & Showcases`。
3. 等待编译完成，确认 Console 无红色错误。
4. Hierarchy 中确认 `GameSystems/RuleRuntime` 存在，且包含 RuleEngine、五个 Runtime Service、F1/F2/F3 工具、Goal、HUD 和 Analytics 组件。

## 2. FPS 与输入

1. 玩家可移动、射击、换弹；敌人受伤、死亡并按配置重生。
2. 玩家不能稳定站在敌人头顶。
3. HUD 持续显示目标、武器、弹药和 HP；受伤时 HP 数字下降。
4. 玩家死亡显示 `DEFEAT`，死亡后不能继续移动、射击或换弹。
5. 再次 `Validate & Play` 后生命、弹药、目标进度和输入恢复。
6. 分别打开 F1、F2、F3：鼠标可点击，点击面板不会开枪；关闭最后一个面板后 FPS 输入恢复。

## 3. 数据驱动与通用 Rule Runtime

1. F1 能看到 Player Damage、Enemy Move Speed 的 Base、Modifiers、Final。
2. Blood Pact：每次击杀 Damage +5%、Enemy Speed +8%，10 层后停止；新生成敌人继承速度 Modifier。
3. Last Stand：HP 越低，伤害越高、移动速度越低；恢复/重开后 Modifier 回到初始值。
4. Reload Gamble：换弹约 30% 概率生成 Runner；击杀 Runner 增加 10 备用弹药。
5. 在 F1 把一个 StatModifier 的 Duration 设为 3 秒并重开：触发后 Modifier 生效，3 秒后自动移除；Stack 的各层应独立过期。
6. Creator 和 Validator 不应提供或接受当前 Runtime 不发布的 TimerInterval、KillStreakReached、HeadshotStreakReached 触发器。

## 4. Validator、Creator 与 AI 边界

1. `PlayerDamage +500%` 和 20 Rules 样例必须被 Validator 拒绝，当前有效 Challenge 不被替换。
2. 关闭 AI 时，F2 仍可 Add/Delete Rule、改参数并通过 `Validate & Play` 运行。
3. Mock Generate/Modify 只更新 Preview，不自动开始；只有玩家点击 `Validate & Play` 才激活。
4. AI Preview 可由玩家继续修改；Patch 未请求字段保持不变；Patch 后仍必须经过 Validator。
5. 若配置真实 Provider，按 `MILESTONE10_PROMPT_TEST_SET.md` 采集真实结果；不得手工补写或美化数据。

## 5. 内容、目标、反馈与 Analytics

1. `1/2/3` 的 Assault Rifle、Shotgun、Sniper 在射速、弹匣、弹丸/散布和可见外观上有区别。
2. Grunt、Runner、Tank 的体型、速度、生命、伤害和重生节奏有区别。
3. KillCount、Score、Survive 均能 Victory；玩家死亡能 Defeat；重开会清空状态。
4. 真正应用效果时显示 `RULE TRIGGERED` 和正确 Stack；达到上限后不伪报。
5. F3 无真实样本时显示 `N/A`；Mock 只增加 All requests，不增加 Real generation samples。
6. Gameplay 完成/失败/重开/停止后写入 Challenge、时长、结果、Kills、Headshots、Rules Triggered。
7. F3 数据目录包含两个 JSONL、两个 CSV 和 `analytics_summary.json`；最后击杀和同帧 Rule Trigger 被计入。

全部通过后回复：`RuleForge V0.1 FINAL PASS`。
