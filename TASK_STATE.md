# RuleForge Task State

Updated: 2026-08-27 (Asia/Shanghai)

## Active milestone

Milestone 1 — FPS minimum sandbox.

Milestone 0 has passed, was fast-forwarded from `dev` to `main`, and both branches were pushed to the private repository:

- https://github.com/yuuuukkk/RuleForge
- Milestone 0 commit at the time of promotion: `abb0d3c build arena blockout`

Do not start Milestone 2 until the user explicitly replies `Milestone 1 PASS`.

## Completed Milestone 1 implementation

- Added the generic damage contract:
  - `Assets/RuleForge/Runtime/DamageInfo.cs`
  - `Assets/RuleForge/Runtime/IDamageable.cs`
- Added player runtime:
  - `Assets/RuleForge/Player/PlayerController.cs`
  - `Assets/RuleForge/Player/PlayerHealth.cs`
- Added weapon runtime:
  - `Assets/RuleForge/Weapons/WeaponController.cs`
- Added enemy runtime:
  - `Assets/RuleForge/Enemies/EnemyController.cs`
  - `Assets/RuleForge/Enemies/EnemyHealth.cs`
  - `Assets/RuleForge/Enemies/EnemySpawner.cs`
  - generated `Assets/RuleForge/Enemies/Enemy.prefab`
- Added `Assets/RuleForge/RuleForge.Runtime.asmdef`.
- Added Editor-only scene bootstrap:
  - `Assets/RuleForge/Debug/Editor/Milestone1SceneBootstrap.cs`
  - menu: `RuleForge > Setup Milestone 1 Sandbox`
- Bootstrap was executed successfully in Unity 2022.3.62f3c1.
- `Assets/Scenes/Arena.unity` now contains:
  - Player with CharacterController, PlayerController, and PlayerHealth
  - Main Camera and AudioListener
  - Weapon and visible WeaponVisual
  - EnemySpawner under GameSystems
  - Enemy prefab reference and all 8 spawn point references
  - Directional Light
- Added Unity Test Framework `1.1.33`.
- Added EditMode and PlayMode test assemblies and tests.

## Runtime data flow

`Legacy Input -> PlayerController -> CharacterController`

`Fire1 -> WeaponController -> Physics.Raycast -> IDamageable -> EnemyHealth`

`EnemyHealth.Died -> EnemySpawner -> configured respawn delay -> new Enemy prefab instance`

`EnemyController -> acquire PlayerHealth -> CharacterController.SimpleMove -> contact DamageInfo -> PlayerHealth`

All Milestone 1 gameplay values are serialized Inspector fields. ScriptableObject configs and the formal RuntimeStats layer belong to Milestone 2 and have not been implemented.

## Tests

Latest recorded results:

- EditMode: Passed, 2/2
  - `DamageInfo_ClampsNegativeDamageToZero`
  - `EnemyHealth_TakesDamageAndDiesAtZero`
  - result: `Logs/milestone1-editmode-results.xml`
- PlayMode: Passed, 2/2
  - `WeaponRaycast_DamagesEnemy`
  - `Arena_EnemyDeathTriggersRespawn`
  - result: `Logs/milestone1-playmode-results.xml`

The first PlayMode raycast attempt failed because the test camera intersected Arena content. It was fixed by placing isolated raycast-test objects at y=50; the rerun passed.

Final pre-commit validation:

- All package manifests, asmdefs, and `SceneTemplateSettings.json` parse successfully.
- Every non-built-in GUID referenced by `Arena.unity` and `Enemy.prefab` resolves to an Asset meta file.
- No prompt keyword dispatch, AI service, RuleEngine, RuntimeStats, or ScriptableObject implementation exists in Milestone 1.
- The only post-test source cleanup removed an unused Editor-only `using` directive; runtime code was not changed after the passing test run.
- Unity-normalized cross-platform defaults in `ProjectSettings.asset` were restored, so that unrelated file is no longer part of the Milestone 1 diff.

## Current repository state

- Active branch: `dev`
- Upstream: `origin/dev`
- The reviewed Milestone 1 implementation, tests, and handoff state are ready for the Milestone 1 commit and push on `dev`. After resuming, use `git status`, `git log -1`, and the remote branch to determine whether that final commit/push step already completed.
- Unity Editor processes are currently running. Do not kill them unless required and authorized; the user may be using the Editor.

Expected modified/generated files include:

- `Assets/Scenes/Arena.unity`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/SceneTemplateSettings.json`
- all Milestone 1 C# scripts, asmdefs, prefab, and Unity meta files listed above

## Remaining work

1. Confirm the Milestone 1 commit is present on `dev` and pushed to `origin/dev`; perform it if it is still pending.
2. Give the user the manual Unity acceptance steps and stop.
3. The user will personally test movement, mouse look, shooting, enemy damage/death, and respawn.
4. Wait for exact confirmation `Milestone 1 PASS`; only then promote `dev` to `main` and begin Milestone 2.

## Resume protocol

After any context compression, read this file first, then run:

```powershell
git status --short --branch
git diff --stat
git diff
```

Continue from the remaining-work list without repeating completed implementation.
