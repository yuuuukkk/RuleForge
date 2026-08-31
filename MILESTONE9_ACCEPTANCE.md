# Milestone 9 手动验收

Codex 不启动 Unity、不运行 PlayMode。本文件中的 Gameplay 验收由玩家在 Unity Editor 内完成。

## 初始化

1. 退出 Play Mode。
2. 点击 `RuleForge > Setup Milestone 9 FPS Content`。
3. 确认 Console 出现 Milestone 9 setup complete，且没有红色编译错误。
4. 进入 Play Mode。

## 三种武器

HUD 右上角应显示武器名和弹药。

1. 按 `1`：Assault Rifle，30 发弹匣，可以按住鼠标连续射击。
2. 按 `2`：Shotgun，8 发弹匣，单次点击发射多颗带散布的弹丸。
3. 按 `3`：Sniper，5 发弹匣，射速慢、单发伤害最高。
4. 三种武器的模型颜色和长度应有变化，按 `R` 均可换弹。
5. 在 F2 Creator 中选择一种 Weapon 后点击 `Validate & Play`，应自动装备选择的武器。

## 三种敌人

场上基础敌人应按 Grunt、Runner、Tank 循环生成：

- Grunt：中等体型、速度、生命和伤害。
- Runner：更小、更快、生命较低。
- Tank：更大、更慢、生命和接触伤害较高。

击杀后应按各自 Respawn Delay 补充敌人。规则生成的 Runner/Grunt/Tank 也必须使用对应 Config，而不是只改名字。

## 三种 Goal

按 `F2`，分别设置并点击 `Validate & Play`：

1. `KillCount`，Goal Target = `2`：击杀两个敌人后显示 `VICTORY`。
2. `Score`，Goal Target = `200`：每次击杀获得 100 分，两次击杀后显示 `VICTORY`。
3. `Survive`，Goal Target = `10`：存活 10 秒后显示 `VICTORY`。
4. 任意 Goal 进行中玩家死亡，应显示 `DEFEAT`。
5. 再次 `Validate & Play` 后，计时、击杀数、分数和胜负状态应重置。

## Rule Feedback

使用 Rule Expression Showcase 或 Blood Pact：

1. 击杀敌人。
2. 屏幕上方应短暂显示 `RULE TRIGGERED`。
3. 应列出本次真正应用的效果，例如 `Player Damage +5%`、`Enemy Move Speed +8%`。
4. Stack 效果应显示当前层数，例如 `Stack 2/10`。
5. 达到最大层数后，没有真正应用的新效果不应继续伪报。

全部通过后回复：`Milestone 9 PASS`。
