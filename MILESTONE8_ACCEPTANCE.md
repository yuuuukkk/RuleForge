# Milestone 8 Manual Acceptance

Codex does not run Unity or PlayMode for this milestone. Complete these checks in the Unity Editor.

## One-time setup

1. Exit Play Mode.
2. Run `RuleForge > Setup Milestone 8 AI Gameplay`.
3. Enter Play Mode and press `F2`.
4. Confirm the provider starts as `Mock AI (fixed offline sample)`.

## Offline smoke check

1. Enter any text and click `AI Generate Preview`.
2. Confirm `AI CREATED` appears, the Creator fields change, and gameplay does not start automatically.
3. Click `AI Modify Preview` after the Mock generation.
4. Confirm Enemy Move Speed changes to `16%`, other fields remain unchanged, and gameplay still does not start automatically.
5. Click `Validate & Play` only after reviewing the preview.

The Mock deliberately returns the same configured sample for every prompt. It proves the offline provider and preview pipeline, not natural-language variety.

## OpenAI setup

For local Editor-only testing, set `OPENAI_API_KEY` in Windows before launching Unity, restart Unity, then use `Switch Provider` in the F2 panel. The key is read from the process environment and is never serialized into the project. For a distributed build, configure the OpenAI provider component to call a secure backend proxy; direct OpenAI access is disabled in player builds.

## Twenty-prompt variety check

Use the real provider. For prompts 1–12 click `AI Generate Preview`. For prompts 13–20, first generate a suitable challenge, then click `AI Modify Preview`. After each result, record Trigger, Effects, values, Max Stack, Probability, Duration, ratio, and Validator status.

1. Create a high-risk mode where every kill gives me 5% damage, while enemy speed grows 7.5%, stacking 10 times.
2. Make a kill-based challenge where the penalty grows exactly twice as fast as the reward.
3. Every reload should have a 20% chance to spawn a Runner, with no other rule.
4. Killing a Runner should grant 15 reserve ammo.
5. As my health drops, increase weapon damage linearly from 0% to 80%.
6. At game start give 10% weapon damage, but also increase enemy speed by 15%, without stacking.
7. Every shot has a 10% chance to spawn a Runner.
8. Each time I am hit, give 5 ammo but increase enemy speed by 4%, stacking 6 times.
9. On every kill, combine an ammo reward with a small damage reward and a larger speed penalty.
10. Build a low-risk challenge based on reloading, with a 40% probability and a short duration value.
11. Create a severe six-stack challenge where reward and penalty are balanced one-to-one.
12. Make a two-rule challenge: kills scale damage, while reloads may spawn Runners.
13. The penalty is too light; double only the enemy-speed value.
14. Reduce only the reward value by half.
15. Change only the maximum stack count to 6.
16. Change only the effect duration to 20 seconds.
17. Raise only the reload probability to 50%.
18. Change only the goal to `Survive` with a target of `30` seconds.
19. Remove only the Runner-spawn rule.
20. Add one new kill rule that grants ammo; keep every existing rule unchanged.

## Pass conditions

- Real-provider outputs show meaningful variation across Trigger, effect combinations, parameters, stacks, probability, duration, and requested ratios.
- No invented vocabulary passes validation.
- Invalid values remain editable in Preview but cannot activate through `Validate & Play`.
- Generate and Modify never start gameplay automatically.
- Modification preserves every field not requested by the player.
- The active challenge changes only after the player clicks `Validate & Play`.

When all checks pass, report exactly: `Milestone 8 PASS`.
