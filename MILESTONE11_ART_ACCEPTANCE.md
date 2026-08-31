# Milestone 11 — Art Placeholder Acceptance

This check is owned by the developer in the Unity Editor. Codex does not start Unity or run
Play Mode for this milestone.

## Setup

1. Open the project in Unity `2022.3.62f3c1` and wait for `Assets/ThirdParty/Kenney` to finish importing.
2. Exit Play Mode.
3. Run `RuleForge > Setup Milestone 11 Art Placeholders` once.
4. Confirm the Console contains `RuleForge Milestone 11 art setup complete` and no missing-art error.

## Manual checks

- `Assets/Scenes/Arena.unity` contains `Environment/ArtPlaceholder` with floor, walls, door,
  containers, and console visuals. Existing `Ground`, `Walls`, and `Cover` gameplay objects remain.
- `Assets/RuleForge/Enemies/Enemy.prefab` has `EnemyVisualController`. Grunt, Runner, and Tank
  instances use the bound survivor/zombie skins while retaining the existing CharacterController
  and damage components.
- The `WeaponVisual` object has `WeaponVisualController`; keys `1`, `2`, and `3` show three
  different Blaster Kit models and the existing weapon controls still operate.
- The HUD shows the objective, health, and ammo icons, a centered crosshair, and the Sci-Fi panel
  skin. F1/F2/F3 tools remain usable and still release the cursor as before.
- Walk into walls/cover and fire at enemies to confirm the art is visual-only: existing colliders,
  damage, respawn, and challenge behavior remain unchanged.

If an imported model or texture is missing, wait for the Asset Database import to finish and rerun
the setup menu. Do not manually edit the generated package license files.
