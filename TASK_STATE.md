# RuleForge Task State

Updated: 2026-08-31 (Asia/Shanghai)

## Milestone status

Milestone 1 — FPS minimum sandbox — **PASS**.

The user explicitly confirmed `Milestone 1 PASS` after manually verifying the enemy-top collision correction.

Milestone 2 — Data-driven Runtime Stat system — **PASS**.

The user confirmed Milestone 2 acceptance with `2pass`, which is recorded as `Milestone 2 PASS`, and then explicitly instructed Codex to begin Milestone 3.

Milestone 3 — General Rule Engine — **PASS**.

The user explicitly confirmed `Milestone 3 PASS` after manual Unity acceptance.

Milestone 4 — Rule expression expansion — **PASS**.

The user explicitly confirmed `Milestone 4 PASS` after manual Unity acceptance.

Milestone 5 — Runtime tuning tools — implementation complete. The user explicitly instructed Codex to proceed to Milestone 6 before recording the exact `Milestone 5 PASS` phrase.

Milestone 6 — Validator + Risk / Reward — **PASS**.

The user explicitly confirmed `Milestone 6 PASS` after manual Unity acceptance.

Milestone 7 — Creator UI — **PASS**.

The user explicitly confirmed `Milestone 7 PASS` after manual Unity acceptance.

Milestone 8 — AI Gameplay Generation — implementation complete, awaiting manual Unity acceptance.

Milestone 9 — FPS Content Expansion — implementation complete, awaiting manual Unity acceptance.

The user explicitly instructed Codex to begin Milestone 9 before recording the exact `Milestone 8 PASS` phrase. Milestone 8 remains implemented but not explicitly accepted.

Milestone 10 — Analytics, Showcases, and Portfolio Evidence — implementation complete, awaiting manual Unity acceptance and collection of real data.

The user explicitly instructed Codex to begin Milestone 10 before recording the exact `Milestone 9 PASS` phrase. Milestone 9 remains implemented but not explicitly accepted.

Milestone 0 has passed, was fast-forwarded from `dev` to `main`, and both branches were pushed to the private repository:

- https://github.com/yuuuukkk/RuleForge
- Milestone 0 commit at the time of promotion: `abb0d3c build arena blockout`

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

Milestone 1 originally stored gameplay values on individual components; Milestone 2 migrated those values into ScriptableObject configs and RuntimeStats.

## Completed Milestone 2 implementation

- Added ScriptableObject base configs and generated default assets:
  - `Config/PlayerConfig.cs` and `Config/Defaults/PlayerConfig.asset`
  - `Config/WeaponConfig.cs` and `Config/Defaults/WeaponConfig.asset`
  - `Config/EnemyConfig.cs` and `Config/Defaults/EnemyConfig.asset`
  - `Config/GameplayBalanceConfig.cs` and `Config/Defaults/GameplayBalanceConfig.asset`
- Added the generic runtime stat layer under `Runtime/Stats/`:
  - `RuntimeStatId`
  - `StatModifierOperation`
  - `StatModifier` with a visible source label
  - `StatCalculator`
  - `RuntimeStat` with Base, Modifier list, and Final value visible in the Inspector
  - `IRuntimeStatProvider`
- Added typed runtime stat components:
  - `PlayerRuntimeStats`
  - `WeaponRuntimeStats`
  - `EnemyRuntimeStats`
- Migrated Player, Weapon, Enemy, Health, and Spawner gameplay numbers away from controller-local serialized fields.
- Controllers now read live `RuntimeStat.FinalValue` values, so future runtime modifiers affect existing objects without mutating Config assets.
- `RuntimeStat` supports AddFlat, AddPercent, Multiply, removal by modifier/source, clearing, minimum bounds, and change notifications.
- Health components subscribe/unsubscribe once to MaxHealth changes and clamp current health when the final maximum decreases.
- Enemy spawning reads configured runtime values from live enemy instances; newly spawned instances initialize from the EnemyConfig base asset.
- Added `RuleForge > Setup Milestone 2 Runtime Stats`, which generated the four default assets and bound:
  - PlayerConfig + PlayerRuntimeStats in Arena
  - WeaponConfig + WeaponRuntimeStats in Arena
  - EnemyConfig + EnemyRuntimeStats in Enemy.prefab
- Arena and Enemy.prefab resource references were checked; all non-built-in GUIDs resolve.

Milestone 2 runtime flow:

`ScriptableObject Config -> typed RuntimeStats -> RuntimeStat Base + Modifiers -> StatCalculator -> FinalValue -> gameplay controller`

Runtime code never assigns into the ScriptableObject configs.

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

Milestone 2 automated results:

- Unity compilation and Milestone 2 asset/bootstrap setup: Passed.
- EditMode: Passed, 5/5.
  - Existing damage tests: 2/2.
  - New low-cost StatCalculator/RuntimeStat tests: 3/3.
  - result: `Logs/milestone2-editmode-results.xml`
- PlayMode regression: not run to completion. A completed EditMode batch process remained alive and locked the project; the subsequent PlayMode Unity instance exited before creating a result file. Safety review did not allow Codex to force-close the two Unity processes because unsaved user work could not be ruled out.

Final pre-commit validation:

- All package manifests, asmdefs, and `SceneTemplateSettings.json` parse successfully.
- Every non-built-in GUID referenced by `Arena.unity` and `Enemy.prefab` resolves to an Asset meta file.
- No prompt keyword dispatch, AI service, RuleEngine, RuntimeStats, or ScriptableObject implementation exists in Milestone 1.
- The only post-test source cleanup removed an unused Editor-only `using` directive; runtime code was not changed after the passing test run.
- Unity-normalized cross-platform defaults in `ProjectSettings.asset` were restored, so that unrelated file is no longer part of the Milestone 1 diff.

User-reported follow-up triage after the baseline commit:

- Player death/end-state behavior is deferred to Milestone 9 `Goal / Survive`.
- Shooting and player-damage feedback are deferred to the planned P1 UX / P2 visual, audio, and UI work. The previously drafted Game Over and weapon-recoil edits were fully reverted before commit.
- Standing on an enemy's head is not covered by a later milestone and is a current collision defect. `PlayerController` now detects an enemy top contact and applies a configurable lateral slide so the player cannot remain perched there.
- The collision correction received static diff/null/scope review. Automated gameplay tests were intentionally not rerun because the user will personally verify scene behavior.
- Manual Unity verification of the enemy-top collision correction passed.

## Current repository state

- Active branch: `dev`
- Upstream: `origin/dev`
- Baseline Milestone 1 commit `348046b` is pushed and `dev` is synchronized with `origin/dev`.
- Milestone 1 collision correction and all Milestone 2 work remain uncommitted, per the user's end-of-day consolidation rule.
- Milestone 3 Rule Engine work remains uncommitted after passing manual Unity acceptance.
- Milestone 4 rule-expression work remains uncommitted after passing manual Unity acceptance.
- Milestone 5 tuning-panel work also remains uncommitted and is awaiting manual Unity acceptance.
- Milestone 6 Validator and Risk / Reward work remains uncommitted after passing manual Unity acceptance.
- Milestone 7 Creator UI work remains uncommitted after passing manual Unity acceptance.
- Milestone 8 AI Gameplay Generation work remains uncommitted and is awaiting manual Unity acceptance.
- Milestone 9 FPS Content Expansion work remains uncommitted and is awaiting manual Unity acceptance.
- Milestone 10 Analytics, Showcases, and Portfolio Evidence work remains uncommitted and is awaiting manual Unity acceptance plus real player/API samples.
- Two Unity processes were still present at the last check: PID 22396 and PID 25864. They originated during this batch-test sequence, but Codex was not permitted to force-close them. If Unity reports that the project is already open, the user should close the existing Unity instance or end those background processes after confirming no unsaved Editor work exists.

## Git collaboration rule

- The user owns all remote pushes.
- Do not stage, commit, switch/promote branches, or push until the user explicitly says today's work should be submitted.
- Keep today's accepted fixes in the working tree without per-fix commits.
- When the user gives the final submission instruction, create one consolidated local commit, report its hash and suggested push command, and let the user perform the push.

## Verification responsibility

- Codex must not start Unity instances or run automatic PlayMode/gameplay tests.
- The user owns all Unity Editor gameplay and scene testing.
- Codex is limited to code implementation, code-structure review, obvious compile-risk review, and static logic review.
- If runtime behavior cannot be confirmed without starting Unity, list it as a manual verification item and do not let the test environment or Unity process state block completion.
- After the current Milestone code is complete, stop instead of expanding the test scope.

## Completed Milestone 3 implementation

- Added JSON-serializable DSL data classes:
  - `ChallengeSpec`
  - `GameplayRule`
  - `RuleTrigger`
  - `RuleCondition`
  - `RuleEffect`
- Added `GameplayEventBus` and the typed `GameplayEvent` / `GameplayEventType` contract.
- Added a generic `RuleEngine` flow:
  - event trigger matching
  - condition evaluation
  - effect execution
  - independent per-effect stack tracking
  - runtime debug stack state
  - challenge runtime reset without mutating Config assets
- Added the required runtime service boundary:
  - `PlayerRuntimeService`
  - `EnemyRuntimeService`
  - `WeaponRuntimeService`
  - `SpawnRuntimeService`
  - `TimeRuntimeService`
- `RuleEngine` and `EffectExecutor` use runtime services instead of directly modifying GameObjects.
- `EnemyHealth` publishes `EnemyKilled` once when health reaches zero.
- `EnemySpawner` publishes `EnemySpawned`; `EnemyRuntimeService` stores global enemy modifiers and applies them to every current and newly spawned enemy.
- Added editable `Challenge/Examples/blood_pact.json`:
  - EnemyKilled trigger
  - WeaponDamage AddPercent 0.05
  - EnemyMoveSpeed AddPercent 0.08
  - Stack / maxStacks 10
- Added idempotent Editor menu `RuleForge > Setup Milestone 3 Rule Engine`, which creates/configures `GameSystems/RuleRuntime`, the five services, and the RuleEngine with Blood Pact.
- No `BloodPactRule.cs`, prompt keyword matching, or Blood Pact-specific runtime branch exists. The 5%, 8%, and 10-stack values exist only in JSON data.

Milestone 3 runtime flow:

`EnemyHealth -> GameplayEventBus -> RuleEngine -> ConditionEvaluator -> EffectExecutor -> RuntimeService -> RuntimeStat Modifier`

Static verification:

- `blood_pact.json` parses and contains one rule, EnemyKilled, two effects, 0.05 / 0.08 values, and maxStacks 10.
- Runtime and Editor C# compiled successfully using Roslyn with the installed Unity 2022.3.62f3c1 managed assemblies, without starting Unity.
- Compiler output contained only expected serialized-private-field warnings; no C# errors.
- Gameplay/PlayMode behavior was not automatically run, per user instruction.

## Completed Milestone 4 implementation

- Extended the existing DSL without adding case-specific gameplay scripts:
  - condition `stringValue`
  - effect `kind` and `stringValue`
  - linear `RuleScaling` data
- Extended the shared RuleEngine with:
  - `RandomChance` conditions evaluated on every matching event
  - `EnemyType` equality / inequality conditions
  - `Linear` scaling from `EventValue` or `PlayerMissingHPPercent`
  - replaceable scaled modifiers so health updates do not accumulate duplicate modifiers
  - repeatable instant actions that are not incorrectly capped by `StackMode.None`
  - generic `StatModifier`, `SpawnEnemy`, and `GiveAmmo` effect kinds
- Added generic `EnemyRuntimeIdentity`; enemy kill events carry the runtime enemy type as event data.
- Added data-driven weapon ammo/reload support:
  - WeaponConfig magazine capacity, starting reserve ammo, and reload duration
  - corresponding WeaponRuntimeStats values
  - R-key reload, PlayerReload event, WeaponFired event, runtime ammo debug fields
  - `WeaponRuntimeService.GiveAmmo`
- Player health now publishes PlayerHit and PlayerHPChanged events with normalized health data.
- PlayerHealth exposes the live current health as a serialized runtime debug field for manual scaled-rule verification.
- `SpawnRuntimeService` can spawn and label a data-selected enemy type such as `Runner`.
- Added `rule_expression_showcase.json` containing four rules executed by the same RuleEngine:
  - Blood Pact stack rule
  - missing health linearly increases weapon damage from +0% to +100%
  - reload has 30% chance to spawn a Runner
  - killing an enemy whose type equals Runner gives 10 reserve ammo
- Added `RuleForge > Setup Milestone 4 Rule Expressions`. It runs the M3 setup if needed and binds the M4 showcase JSON.
- No `LowHPDamageScript`, `ReloadSpawnScript`, Runner-specific gameplay script, or case-specific runtime branch exists.

Static verification:

- Runtime and Editor C# compiled successfully with Roslyn and the installed Unity 2022.3.62f3c1 managed assemblies, without starting Unity.
- The M4 JSON parses as four rules with the expected triggers, conditions, effect kinds, scaling source, 0.3 probability, and 10-ammo reward.
- Only expected private serialized-field warnings were emitted; there were no C# errors.
- Gameplay/PlayMode behavior was not automatically run, per user instruction.

## Completed Milestone 5 implementation

- Added a developer `RuntimeTuningPanel` toggled with F1 using Unity immediate-mode GUI; no Canvas/presentation scope was added.
- The panel displays the active Challenge, every Rule trigger, conditions, effects, trusted Reward/Penalty classification, and current editable parameters.
- Runtime-editable fields include:
  - effect Value (AddPercent is displayed as a human-readable percentage)
  - MaxStacks
  - Probability percentage
  - Duration seconds
  - scaled Effect Min / Effect Max percentages
  - Reward Multiplier
  - Penalty Multiplier
- Added `Apply & Restart Challenge`:
  - applies changes only to the parsed runtime ChallengeSpec copy
  - never mutates the source JSON TextAsset or Config assets
  - removes old rule modifiers and stack/execution state
  - restores player health and weapon ammo
  - removes extra spawned enemies/Runners and rebuilds the baseline enemy set
  - republishes GameStarted after reset
- Added Stat Breakdown for Player Damage and Enemy Move Speed:
  - Base
  - each Modifier operation/value/source
  - Final
- Added a minimal trusted `EffectCatalog` ScriptableObject with display name and system-owned polarity for the current showcase effects.
- RuleEngine applies runtime Reward/Penalty multipliers according to EffectCatalog polarity instead of trusting polarity declared by Challenge data.
- Added `RuleForge > Setup Milestone 5 Tuning Panel`; it runs prerequisite setup, assigns GameplayBalanceConfig/EffectCatalog, and adds the tuning panel to RuleRuntime.
- Added runtime mutation methods only on parsed DSL objects; JSON and ScriptableObject assets remain immutable during play.
- Duration is now represented and editable in RuleEffect data. No current M4 showcase rule is timed, so timed execution remains outside the M5 acceptance case.

Static verification:

- Runtime and Editor C# compiled successfully with Roslyn and the installed Unity 2022.3.62f3c1 managed assemblies without starting Unity.
- Only expected private serialized-field warnings were emitted; there were no C# errors.
- EffectCatalog asset resolves to the correct script GUID; all asset GUIDs remain unique.
- No hardcoded 8%/20% tuning branch, prompt keyword matching, or case-specific gameplay script exists.
- Gameplay/PlayMode behavior was not automatically run, per user instruction.

## Completed Milestone 6 implementation

- Added a unified `ValidationResult` contract with `IsValid`, `Errors`, and `Warnings`.
- Added the required validation layers under `Assets/RuleForge/Validation/`:
  - `SchemaValidator`
  - `SemanticValidator`
  - `RangeValidator`
  - `ComplexityValidator`
  - `BalanceEvaluator`
- Added `ChallengeValidator` as the single validation entry point.
- Validation now rejects:
  - missing required challenge/rule/trigger/effect data
  - unknown Trigger, Condition, Effect, EffectKind, Target, Stat, Operation, StackMode, Scaling Source, or Scaling Mode
  - duplicate rule IDs
  - context-incompatible EnemyType conditions
  - target/stat mismatches
  - invalid probability, duration, stack, effect value, and scaling ranges
  - rule/effect/condition counts above configured complexity limits
- All numeric and complexity limits are read from `GameplayBalanceConfig`; allowed Effect identity, polarity, and score weight are read from the trusted `EffectCatalog`.
- Default trusted limits are configured as:
  - MaxRules 6
  - MaxEffectsPerRule 4
  - MaxConditionsPerRule 3
  - MaxStacks 20
  - PlayerDamage 0 to 1.5
  - EnemyMoveSpeed 0.05 to 1.5
  - PlayerDamageFromMissingHealth 0 to 1.5
  - SpawnRunner fixed at 0
  - GiveAmmo 1 to 100
- RuleEngine validates every parsed ChallengeSpec before activation. Rejected candidates do not replace or mutate the current active challenge.
- Runtime tuning now edits a cloned candidate, validates it, and only then activates/restarts it; invalid panel values cannot partially mutate live rule data.
- Added approximate RewardScore, PenaltyScore, RewardPenaltyRatio, and Balanced/RewardHeavy/RiskHeavy feedback using trusted Effect polarity/weights and configured balanced-ratio thresholds.
- F1 panel now shows validation status, every error/warning, Reward/Risk bars, ratio, and balance result.
- Added `RuleForge > Setup Milestone 6 Validator`; it runs prerequisite setup and configures trusted limits/weights.
- Added `RuleForge > Validate Milestone 6 Acceptance Samples` for manual one-click Console verification.
- Added deliberate acceptance assets:
  - `invalid_player_damage_500.json`
  - `invalid_twenty_rules.json`
- Added two low-cost pure validator EditMode tests for the same rejection cases. They were statically compiled but not automatically run.

Static verification:

- Runtime, Editor, and EditMode test C# assemblies compiled successfully with Roslyn using Unity's generated reference lists, without starting Unity.
- All four challenge JSON files parse successfully.
- The valid M4 showcase has 4 rules, at most 2 effects per rule, and at most 1 condition per rule, all within M6 limits.
- The +500% sample contains value 5.0 against the configured PlayerDamage maximum 1.5.
- The complexity sample contains 20 rules against the configured maximum 6.
- All 85 Unity Asset GUIDs are unique.
- No prompt keyword dispatch or case-specific Gameplay rule script was added.
- Gameplay/PlayMode behavior and the Unity menu actions were not automatically run, per user instruction.

## Completed Milestone 7 implementation

- Added `ChallengeCreatorPanel`, a separate runtime Creator UI toggled with F2.
- The Creator loads a cloned copy of the active Challenge and exposes:
  - Challenge Name
  - Goal
  - Weapon
  - Reward Strength slider
  - Penalty Strength slider
  - Rule cards
- Rule cards support:
  - Trigger dropdown sourced from the allowed `GameplayEventType` enum
  - Effect dropdown sourced from creator-enabled trusted `EffectCatalog` definitions
  - direct numeric Value and Max Stack editing
  - scaling min/max editing for existing scaled effects
  - Add Rule / Delete Rule
  - Add Effect / Delete Effect
- Existing Conditions are cloned and preserved. They are shown as read-only counts because V0.1 explicitly does not require a complete condition/node editor.
- Added Goal and Weapon fields to ChallengeSpec. All existing example and acceptance JSON assets now include both fields.
- Added safe DSL factory methods for ChallengeSpec, GameplayRule, RuleTrigger, RuleEffect, and RuleScaling; Creator builds structured DSL data rather than editing runtime objects directly.
- EffectDefinition now stores optional creator templates: allowed status, kind, target, stat, operation, default value/string, stack mode, and max stacks.
- Creator effect selection never contains an Effect-to-Runtime mapping hardcoded in the UI; mappings come from the trusted EffectCatalog asset.
- Added configurable minimum/maximum strength multiplier bounds to GameplayBalanceConfig; the default Creator slider range is 0 to 2.
- Reward/Risk evaluation now includes the selected Reward/Penalty strength multipliers.
- `Validate & Play` flow:
  - parses all numeric draft values
  - builds a new ChallengeSpec candidate
  - submits it to the existing M6 Validator
  - activates only a valid candidate
  - applies strength multipliers
  - restarts the challenge
- Invalid Creator input does not mutate the active Challenge, Config assets, or runtime modifier state.
- Added `RuleForge > Setup Milestone 7 Creator UI`; it runs prerequisite setup, configures creator templates, and adds the Creator component to RuleRuntime.
- No AI service, prompt handling, keyword matching, or fixed prompt-to-template mapping was added.

Known M7 scope limits:

- Goal and Weapon are editable ChallengeSpec metadata in M7. Goal execution belongs to the later Goal milestone, and the current sandbox has only its existing rifle.
- Existing Conditions are preserved but not editable in this V0.1 Creator.
- The panel uses immediate-mode runtime GUI, matching the existing tuning tools; presentation polish is outside M7.

Static verification:

- Runtime, Editor, and EditMode test assemblies compiled successfully with Roslyn using Unity's generated reference lists, without starting Unity.
- All four Challenge JSON assets parse and include non-empty Goal and Weapon fields.
- The EffectCatalog asset contains four creator-enabled templates; the existing missing-health scaling effect is preserved but intentionally not offered as a new simple template.
- All 87 Unity Asset GUIDs are unique.
- UI and Validation folder meta GUIDs remain unchanged from the repository skeleton.
- No AI-related class, prompt parsing, case-specific rule script, or runtime ScriptableObject mutation was introduced.
- Gameplay/PlayMode behavior and the Unity menu action were not automatically run, per user instruction.

## Completed Milestone 8 implementation

- Added the required provider abstraction `IAIGameplayService` with independent `GenerateChallenge` and `ModifyChallenge` operations.
- Added two provider implementations:
  - `MockAIGameplayService`, which returns fixed configured JSON regardless of prompt and keeps the project usable without an API.
  - `OpenAIResponsesGameplayService`, which uses the Responses API with strict Structured Outputs JSON Schema and `store: false`.
- The OpenAI provider defaults to `gpt-5-mini`, a model documented to support the Responses endpoint and Structured Outputs. Endpoint/model remain Inspector-configurable and are isolated from gameplay runtime logic.
- OpenAI keys are never serialized or committed:
  - direct OpenAI access is Editor-only and reads `OPENAI_API_KEY` from the process environment
  - player builds reject direct OpenAI access and require a secure backend/proxy endpoint
- Added runtime-built Gameplay Vocabulary containing only the currently published gameplay Events, allowed Conditions, Effects, Operations, effect identity mappings, and trusted balance/complexity limits.
- Added strict JSON schemas for full `ChallengeSpec` generation and `ChallengePatch` generation. Raw Responses API parsing searches all output content items for `output_text` and handles explicit refusal/error responses.
- Added `ChallengePatch` and an applier supporting exactly:
  - AddRule
  - RemoveRule
  - ModifyValue
  - ModifyMaxStack
  - ModifyDuration
  - ModifyProbability
  - ModifyGoal
- Patch application clones the current Creator candidate first. A failed patch cannot partially mutate the current preview or active challenge.
- Added `AIGameplayController` as the provider-independent orchestration layer:
  - User Prompt + Creator Preferences + Gameplay Vocabulary
  - provider Structured Output
  - ChallengeSpec or ChallengePatch parse/apply
  - Validator + BalanceEvaluator
  - Creator Preview callback
- AI generation and modification never call `TryApplyRuntimeChallenge` or `RestartChallenge`; only the existing player-controlled `Validate & Play` button may activate a preview.
- Extended the F2 Creator with:
  - provider status and provider switching
  - natural-language prompt input
  - `AI Generate Preview`
  - `AI Modify Preview`
  - Validator/Risk-Reward feedback for generated candidates
  - visible condition details and editable effect Duration
- Hardened `SemanticValidator` so an untrusted AI cannot pair a trusted effect ID/polarity with a different runtime kind, target, stat, operation, or string payload.
- Restricted AI schema Events to the events actually published by the current sandbox: GameStarted, EnemyKilled, PlayerHit, PlayerReload, WeaponFired, and PlayerHPChanged.
- Added `RuleForge > Setup Milestone 8 AI Gameplay`; it runs the M7 setup, adds both providers plus the controller to RuleRuntime, configures the Mock assets, and keeps Mock selected by default.
- Added `mock_penalty_patch.json`, two pure EditMode patch tests, and `MILESTONE8_ACCEPTANCE.md` with the offline smoke flow and the required 20-prompt real-provider checklist.
- No prompt keyword matching, natural-language runtime parser, AI-generated C#, or fixed prompt-to-template dispatch was added.

Static verification:

- Runtime, Editor, and EditMode test assemblies compile successfully with Roslyn using Unity 2022.3.62f3c1 reference assemblies, without starting Unity.
- The Mock ChallengePatch JSON parses successfully.
- All 96 Unity Asset GUIDs are unique.
- Searches found no `prompt.Contains`, `Contains("speed")`, `Contains("damage")`, or `Contains("reload")` dispatch in RuleForge source.
- No API key literal is present; only the `OPENAI_API_KEY` environment-variable name and Authorization header construction exist.
- AI provider code contains no challenge activation/restart call.
- Gameplay/PlayMode behavior, live API calls, and the Unity setup menu were not run, per user instruction.

## Completed Milestone 9 implementation

- Expanded the existing data-driven weapon system to three profiles:
  - Assault Rifle: automatic, 30-round magazine, medium damage and fire rate.
  - Shotgun: semi-automatic, 8 pellets with spread, 8-round magazine.
  - Sniper: semi-automatic, long range, slow fire rate, highest single-shot damage.
- Added `WeaponArchetype` and presentation/fire-mode fields to `WeaponConfig` while retaining `WeaponRuntimeStats` as the live mutable stat layer.
- Added `WeaponLoadout` with keys 1/2/3, exact-name Challenge weapon selection, ammo reset on equip, and simple color/scale differences for manual identification.
- Refactored `WeaponController` to use automatic or click fire per profile, fire configurable pellet counts/spread, and publish `EnemyHit` for successful enemy hits.
- Expanded the existing data-driven enemy system to three profiles:
  - Grunt: balanced baseline.
  - Runner: smaller, faster, lower health.
  - Tank: larger, slower, higher health and contact damage.
- `EnemySpawner` now owns a configured roster, cycles Grunt/Runner/Tank for baseline population, and resolves exact requested types for SpawnEnemy effects. Spawned instances receive the correct Config, identity, stats, health, visual color, and scale before registration.
- Added the supported Challenge goals `Survive`, `KillCount`, and `Score`, plus a positive `goalTarget` field throughout ChallengeSpec, JSON samples, Creator UI, Validator, AI vocabulary/schema, current preferences, and ChallengePatch ModifyGoal flow.
- Added `ChallengeGoalController`:
  - Survive accumulates elapsed time.
  - KillCount observes EnemyKilled events.
  - Score awards 100 points per kill.
  - player death produces Defeat.
  - challenge restart clears progress/result and equips the Challenge-selected weapon.
- Added rule feedback contracts and `RuleEngine.RuleTriggered`. Feedback is emitted only for effects actually applied and includes the applied value plus current/max stacks.
- Added `GameplayHud` showing live goal progress, selected weapon/ammo, Victory/Defeat, and the required temporary `RULE TRIGGERED` effect list.
- Added trusted `SpawnGrunt` and `SpawnTank` catalog definitions and balance limits alongside the existing `SpawnRunner` effect.
- Added six profile assets under `Assets/RuleForge/Config/Defaults/` and updated all Challenge JSON examples to valid M9 Goal/Weapon metadata.
- Added `RuleForge > Setup Milestone 9 FPS Content`. It runs prior setup, configures all six profiles, the Enemy prefab/roster, runtime services, WeaponLoadout, ChallengeGoalController, and GameplayHud, then saves the active scene/assets.
- Added `MILESTONE9_ACCEPTANCE.md` containing the user-owned manual test flow for all three weapons, all three enemies, all three goals, restart/death behavior, and Rule Feedback.

Milestone 9 static verification:

- Runtime (46 source files), Editor (9 source files), and EditMode test (4 source files) assemblies compile successfully with Roslyn and Unity 2022.3.62f3c1 managed references, without starting Unity.
- All 5 RuleForge JSON files parse successfully.
- All 108 Unity Asset GUIDs are unique, and Config asset GUID references resolve.
- `git diff --check` passes for tracked changes.
- Searches found no prompt keyword dispatch and no AI-provider call that activates or restarts a challenge.
- Gameplay/PlayMode behavior and the Milestone 9 setup menu were not run, per user instruction.

## Completed Milestone 10 implementation

- Added the `Assets/RuleForge/Analytics/` runtime data layer:
  - `AIGenerationRecord` captures UTC timestamp, request type, provider, Prompt, Generation Time, Parse Success, Validation Result, Rule Count, Reward Score, Penalty Score, Retry status, and benchmark eligibility.
  - `GameplaySessionRecord` captures Challenge, Play Duration, Victory/outcome, Kills, Headshots, and Rules Triggered.
  - `AnalyticsStorage` appends raw JSONL and CSV evidence and rebuilds `analytics_summary.json` from the raw logs.
  - `AnalyticsRecorder` subscribes to AI completion, Challenge restart/result, Gameplay events, and Rule feedback without coupling analytics into the rule executor.
- Added truthful aggregate definitions:
  - First-pass generation success = non-Retry real-provider Generate outputs that both parse and validate.
  - Validation rejection rate = parsed real-provider Generate outputs rejected by Validator.
  - Retry success = exact same Provider/operation/Prompt retried after the previous matching failure and then passing parse + validation.
  - Average generation latency = real-provider Generate latency only.
- Mock requests remain in raw logs but are explicitly marked ineligible and excluded from portfolio benchmark rates. Zero-sample rates display N/A in the runtime panel rather than a misleading 0%.
- Added CSV formula-injection protection for user-controlled text fields while preserving the exact Prompt in append-only JSONL.
- Added `AnalyticsPanel`, toggled with F3, showing real sample counts, benchmark rates, current gameplay counters, and the persistent evidence directory. It can refresh the summary and copy the data-folder path.
- Added `AIGenerationTelemetry` and provider `IsBenchmarkEligible` metadata. `AIGameplayController` now times Generate/Modify requests and publishes result telemetry after structured parsing/patching/validation; it still never auto-activates AI output.
- Added Headshot event publication for actual enemy damage when a ray hits the upper 28% of the hit collider. M10 analytics counts the published Headshot event; no headshot damage bonus was added.
- Added `ChallengeGoalController.StateChanged`. Gameplay completion is deferred until LateUpdate so the final EnemyKilled and same-frame RuleTriggered events are included before the session is written.
- Extended current AI Gameplay Vocabulary with the now-published Headshot event.
- Added official standalone Showcase data and menu loaders:
  - Showcase 01 Blood Pact: existing `blood_pact.json`, corrected to explicit trusted `StatModifier` identity.
  - Showcase 02 Last Stand: missing-health damage increase plus missing-health movement decrease.
  - Showcase 03 Reload Gamble: 30% Reload spawn Runner plus Runner-kill ammo reward.
- Added trusted `PlayerMoveSpeedFromMissingHealth` effect metadata and range `[-0.75, 0]` for Last Stand.
- Updated scaling validation to accept bounded descending endpoints such as 0% to -50%; runtime linear interpolation already supported this data shape.
- Added `RuleForge > Setup Milestone 10 Analytics & Showcases` and three `RuleForge > Showcases > Load ...` menu actions. Menu setup never enters Play Mode and does not create sample results.
- Added two minimal EditMode tests (statically compiled, not run): real-provider summary filtering/calculation and descending Last Stand scaling validation.
- Added evidence/acceptance material:
  - `MILESTONE10_PROMPT_TEST_SET.md`: 40 Generate + 10 Modify prompts, with no fabricated results.
  - `PORTFOLIO_CAPTURE_CHECKLIST.md`: Blood Pact, Last Stand, Reload Gamble, Creator loop, and Analytics evidence checklist.
  - `MILESTONE10_ACCEPTANCE.md`: user-owned Unity verification and data-file checks.

Milestone 10 static verification:

- Runtime (51 source files), Editor (10 source files), and EditMode test (5 source files) assemblies compile successfully with Roslyn and Unity 2022.3.62f3c1 managed references, without starting Unity.
- All 7 RuleForge JSON files parse successfully.
- Prompt test set contains exactly 40 Generate prompts and 10 Modify prompts (50 total).
- All 117 Unity Asset GUIDs are unique.
- The complete M10 file/meta set exists, and `git diff --check` passes for tracked changes.
- No Unity/PlayMode instance, live OpenAI request, gameplay run, recording, fake analytics record, commit, or push was performed.

## Final acceptance and Fix implementation

The user started the consolidated final acceptance/Fix phase. Codex completed a static cross-milestone audit without starting Unity or PlayMode and fixed the following confirmed issues:

- Added one trusted list of gameplay events that are actually published by the current Runtime. Creator dropdowns, AI Gameplay Vocabulary, schemas, and SemanticValidator now agree. Roadmap-only `TimerInterval`, `KillStreakReached`, and `HeadshotStreakReached` values can no longer validate as rules that would never trigger.
- Added shared `RuntimeInputGate` behavior for F1/F2/F3. Opening any runtime tool releases the cursor and blocks player movement/fire/reload; multiple open panels are reference-counted, and input/cursor state restores only after the final panel closes. F3 is now clickable instead of remaining under cursor lock.
- Player death now blocks movement, fire, and reload. The existing Defeat state remains visible, and the gameplay HUD now shows live HP so incoming damage has immediate numeric feedback.
- Implemented real `RuleEffect.duration` behavior for StatModifier effects. Duration 0 remains persistent until restart; positive duration expires the modifier. Stack instances receive unique sources and expire independently; scaled modifiers refresh their own expiry. Runtime restart clears all scheduled expirations.
- SemanticValidator now rejects Duration or Scaling on instant actions and rejects stacked Scaling, all of which were previously accepted but ignored at execution time. It also rejects unknown comparisons consistently and non-finite Goal Target values.
- Challenge restart ordering now starts the new analytics/goal session before reset-generated HP/rule events, so those events are not attributed to the previous session.
- Corrected impossible real-provider test prompts that asked ChallengePatch to modify Weapon, an operation deliberately absent from the approved V0.1 patch vocabulary.
- Replaced stale Milestone 0 README text with the current V0.1 architecture, setup, controls, security boundary, analytics, and acceptance links.
- Added `FINAL_ACCEPTANCE.md` with one consolidated developer-owned verification flow.

Final static verification after these fixes:

- Runtime assembly: 52 source files compiled successfully with Roslyn and Unity 2022.3.62f3c1 references.
- Editor assembly: 10 source files compiled successfully.
- EditMode test assembly: 5 source files compiled successfully.
- All 7 Challenge/Patch JSON files parse.
- All 118 Unity Asset GUIDs are unique; serialized scene/prefab/config references resolve, excluding Unity built-in GUIDs.
- All relevant `.cs`, `.json`, `.asset`, and `.prefab` files have matching meta files.
- Prompt test set remains exactly 40 Generate + 10 Modify = 50.
- Searches found no Prompt keyword gameplay dispatch and no AI-side challenge activation/restart call.
- `git diff --check` passes.
- No Unity/PlayMode instance, live provider call, gameplay run, fake analytics data, commit, or push was performed.

## Approved placeholder art collection

- The user selected candidate 1, the Kenney Sci-Fi CC0 placeholder suite.
- Six archives were downloaded only from official `kenney.nl` URLs, hashed, extracted in a
  temporary directory, and checked for their original package license files before import.
- Imported the verified Unity-facing subset under `Assets/ThirdParty/Kenney/`:
  - Space Station Kit 1.0: 97 FBX models plus colormap.
  - Animated Characters Survivors 1.1: one rigged character, three animation FBXs, and four skins.
  - Blaster Kit 2.1: 40 FBX models plus colormap.
  - UI Pack: Sci-fi 2.0: 250 double-resolution PNG elements and two fonts.
  - Game Icons: 105 white double-resolution PNG icons.
  - Crosshair Pack 1.1: 200 outline PNG reticles.
- Every imported package retains its original `LICENSE.txt`. Static review confirmed that all six
  state CC0/commercial use and optional attribution, with Kenney identified as creator/distributor.
- `Assets/ThirdParty/Kenney/README.md` records official sources, versions, archive SHA-256 hashes,
  imported subsets, and intended RuleForge mapping.
- The original shortlist's old `Animated Characters 3` package was not downloaded from a mirror
  after its current `kenney.nl` page returned 404. It was replaced with the current official
  `Animated Characters Survivors` package, which provides the same four-skin placeholder role.
- Added the idempotent Editor-only binding menu
  `RuleForge > Setup Milestone 11 Art Placeholders` in
  `Assets/RuleForge/Debug/Editor/Milestone11ArtBootstrap.cs`.
- Added runtime visual adapters:
  - `Assets/RuleForge/Enemies/EnemyVisualController.cs` binds the rigged survivor model and
    Grunt/Runner/Tank skins on `Enemy.prefab` without changing its gameplay collider.
  - `Assets/RuleForge/Weapons/WeaponVisualController.cs` binds three Blaster Kit models through
    `WeaponLoadout`, with the original primitive retained as a fallback until setup runs.
  - `GameplayHud`, `EnemySpawner`, and `WeaponLoadout` now expose the art binding hooks.
- The setup menu adds visual-only Space Station objects under `Environment/ArtPlaceholder`, keeping
  the existing Ground/Walls/Cover physics objects intact. It also assigns HUD objective/health/ammo
  icons, a crosshair texture, and a Sci-Fi panel texture from `UISciFi`.
- Unity was not started by Codex. The current workspace now contains generated `.meta` files for the
  imported art (created by the user's/previous editor refresh); if a fresh checkout has not imported
  them yet, wait for Unity to finish importing before running the menu.
- Static compile after the art binding code was added: runtime 54 source files, Editor 11 source
  files, EditMode tests, and PlayMode tests all compiled successfully with the installed Unity
  2022.3.62f3c1 Roslyn reference configuration; no Unity/PlayMode execution was performed.

## Safe Mode diagnosis

- The project-local Bee log recorded the Runtime assembly failing with CS1069: `UnityWebRequest`,
  `UploadHandlerRaw`, and `DownloadHandlerBuffer` were forwarded to
  `UnityEngine.UnityWebRequestModule`, but that module was not referenced.
- `OpenAIResponsesGameplayService.cs` is the only RuleForge source using `UnityEngine.Networking`.
- Added the standard built-in dependency `com.unity.modules.unitywebrequest: 1.0.0` to
  `Packages/manifest.json`. The manifest parses successfully.
- Unity was not started to force package resolution. On the next normal open, let Package Manager
  finish resolving the new built-in module and allow one script recompile. The existing stale
  `Library/Bee` log will remain historical until Unity refreshes it.
- Recompiled the runtime and Editor assemblies statically with the missing UnityWebRequest module
  reference included; both pass. No other RuleForge `UnityEngine.*` module dependency is missing
  from the manifest based on source inspection.
- After the manifest change, the existing Unity project compiler regenerated
  `Library/ScriptAssemblies/RuleForge.Runtime.dll`; the latest project-local Bee result is
  `exitcode: 0`, confirming the Safe Mode compile error is cleared.

## Unity lifecycle fix

- Unity Console reported `CreateImpl is not allowed ... from a MonoBehaviour constructor` for
  `WeaponLoadout`. The cause was a `MaterialPropertyBlock` created in a field initializer.
- Moved lazy `MaterialPropertyBlock` creation into runtime methods for `WeaponLoadout`,
  `EnemyVisualController`, and `WeaponVisualController`; no Unity-native object is constructed
  during a MonoBehaviour field initializer now.
- Runtime and Editor assemblies were statically recompiled after this fix with exit code 0.
- A later Console screenshot also showed `ArgumentNullException: Parameter name: dest` during the
  earlier setup attempt, but no RuleForge source passes a `dest` argument and the screenshot omits
  its stack trace. Treat it as a separate setup/import diagnostic: clear the historical Console,
  let the fixed assemblies/domain reload, rerun the setup from the saved `Arena.unity` scene, and
  capture the expanded stack if it recurs.

## Current UI / AI data-chain stage

- Static scene inspection confirms `Arena.unity` already serializes the runtime UI components:
  `ChallengeCreatorPanel` (F2), `RuntimeTuningPanel` (F1), `GameplayHud`, `AnalyticsPanel` (F3),
  `AIGameplayController`, and `RuleEngine` under `GameSystems/RuleRuntime`. The Creator panel now
  also serializes the real `WeaponLoadout` reference instead of relying only on discovery.
- The Arena HUD's five Kenney UI texture references (reticle, objective, health, ammo, panel) are
  now serialized directly to the verified imported assets. Enemy/weapon model bindings and the
  decorative environment remain owned by the Milestone 11 idempotent menu if they are not already
  present in the user's scene.
- The verified Kenney Blaster Kit bindings are now serialized on `WeaponVisual` for Assault Rifle,
  Shotgun, and Sniper; `Enemy.prefab` now serializes the survivor model plus Grunt/Runner/Tank
  skins and disables the primitive fallback renderer. The Space Station decorative environment
  still uses the idempotent Milestone 11 menu because it creates multiple prefab instances.
- Creator UI edits are data-driven: it clones the active `ChallengeSpec`, exposes challenge fields,
  trigger, rule id, conditions (type/comparison/value/string value), effect catalog selection,
  effect value/max stacks/duration/scaling, then rebuilds a candidate and calls
  `RuleEngine.TryApplyRuntimeChallenge`. That method runs `ChallengeValidator` and
  `BalanceEvaluator` before activation; `Validate Preview` runs those checks without activation,
  and `Validate & Play` then restarts the runtime.
- Added `RuleCondition.Create` so manually edited condition drafts become real DSL data instead of
  read-only text. Weapon choices now come from the serialized `WeaponLoadout` configs.
- Added explicit provider metadata and truthful Creator labels: `AI Not Connected`,
  `Mock Provider Active`, `Real AI Provider Active`, or `Real AI Provider Not Configured`.
- Removed the old static `Manual Challenge` / `KillCount 10` / `Assault Rifle` editor defaults;
  an empty Creator now requires user input unless a real active `ChallengeSpec` supplies values.
- Added `RuleForgeLocalization` for the tool panels only. Creator (F2), Runtime Tuning (F1), and
  Analytics (F3) expose a `中文` / `English` switch and localize their labels while retaining real
  runtime values. The Gameplay HUD no longer contains or responds to a language switch.
- AI status is `PARTIAL`: the scene defaults to the configured Mock provider, while the real
  `OpenAIResponsesGameplayService` exists and sends structured `UnityWebRequest` requests only
  when selected and authorized. No live LLM request was made by Codex. Generate returns a
  structured `ChallengeSpec`; Modify returns `ChallengePatch`; both pass through Validator and
  BalanceEvaluator before the UI preview. No prompt keyword dispatch was found.
- Static compile after the UI changes: Runtime, Editor, EditMode test, and PlayMode test
  assemblies all compile with exit code 0 using the installed Unity 2022.3.62f3c1 Roslyn
  references. Unity/PlayMode was not started.
- Static compile was repeated after localization and serialized art binding changes; all four
  assemblies still exit 0. Arena/Enemy YAML block IDs remain unique and all referenced Kenney
  asset GUIDs have matching meta files.
- Manual Unity verification remains required: clear historical Console errors, let scripts reload,
  run `RuleForge > Setup Milestone 10 Analytics & Showcases` (and Milestone 11 art setup if the
  HUD textures/models are still empty), enter Play Mode, press F2, verify the truthful AI status,
  edit a generated rule/condition/effect, confirm Validator + Reward/Risk feedback changes, then
  use `Validate & Play`; switch to OpenAI only after setting `OPENAI_API_KEY` and verify a real
  request if desired.

## UI re-review after localization correction

- The user requested a complete UI re-review and explicitly said not to repeat compilation checks.
  No compiler, Unity instance, PlayMode, or gameplay test was run in this review.
- F1/F2/F3 now use `RuntimePanelCoordinator`; opening one tool closes the previous tool so panels
  cannot overlap and `RuntimeInputGate` cursor ownership is released correctly.
- Removed the language button and all localization dependencies from `GameplayHud`. Language can
  be switched only from the three tool panels and persists while moving between them.
- Added localized display mappings for every current Goal, runtime Trigger, Condition, Comparison,
  weapon, enemy type, Effect, polarity, and Balance result. Raw DSL values remain unchanged.
- EnemyType is now a trusted localized dropdown (`Grunt`/`Runner`/`Tank`) rather than free text,
  preventing Chinese display text from being written into `ChallengeSpec.stringValue`.
- Removed the fixed English AI example prompt; the prompt starts empty.
- Creator and Runtime Tuning now fingerprint the exact editable candidate plus strength values.
  After any edit, old Validator/Balance results are hidden and the panel says the current edits
  are not checked. `Validate Preview` validates/evaluates without changing Runtime. Reopening a
  panel revalidates the real active challenge, preventing a preview from another panel from being
  shown as active data.
- Added a RuleEngine preview overload so BalanceEvaluator uses the panel's draft Reward/Penalty
  multipliers without mutating live multipliers.
- Fixed two final review findings: newly selected EnemyType conditions always receive a trusted
  default enemy value, and F1/F2 now validate and apply the candidate ChallengeSpec plus its draft
  Reward/Penalty strengths atomically. A rejected candidate cannot partially change live strength
  values, and F1 rejects non-finite or out-of-range multiplier text before validation.
- Switching the F1/F2 panel language clears old action-status text so an English status message is
  not left inside an otherwise Chinese panel (or vice versa).
- Static UI interaction/data-chain matrix: 23/23 PASS (panel toggles, HUD exclusion, mutual
  exclusion, Generate/Modify calls, exact preview/apply/restart paths, stale-result guards, atomic
  strength application, real weapon and Effect sources, input range protection, and no prompt
  keyword gameplay).
- Localization coverage: 30/30 dropdown/runtime vocabulary entries and 8/8 EffectCatalog entries.
- All 7 Challenge/Patch JSON files parse. Arena contains exactly one F1, F2, F3, AI Controller,
  and Gameplay HUD component, with one serialized F1/F2/F3 key binding each.

## UI Safe Mode follow-up

- After the UI review, Unity entered Safe Mode because its Bee Runtime compile found four CS8967
  errors in `ChallengeCreatorPanel.cs`. The localized Effect label used interpolated expressions
  split across physical lines, which Unity 2022's C# 9 compiler does not accept.
- Replaced those two label constructions with ordinary string concatenation; behavior and raw
  Effect IDs are unchanged.
- Ran targeted Runtime, Editor, EditMode-test, and PlayMode-test assembly compilation using Unity's
  existing Bee response files, without starting Unity or executing tests. All four completed with
  exit code 0 and no additional compiler errors.

## Runtime art-instantiation follow-up

- Unity Play Mode reported two `InvalidCastException` instances. The full Editor log identified
  `EnemyVisualController.EnsureModel` and `WeaponVisualController.Apply`; both failed inside generic
  `Object.Instantiate<GameObject>` while instantiating serialized Kenney FBX references.
- The first defensive code change removed the forced generic return cast, but the user's rerun then
  exposed the actual binding defect: all four hand-serialized FBX references used local file ID
  `100100000`, which is the imported model's Prefab container rather than its root GameObject.
- Enemy and weapon visual controllers now instantiate through `UnityEngine.Object`, accept either
  a returned `GameObject` or `Component.gameObject`, and report a specific error for any unsupported
  asset type. The weapon fallback renderer is disabled only after model creation succeeds.
- A subsequent user run showed no art exception but still no visible enemies. That proved the guessed
  `100000` local ID also resolved as null under this Unity 2022 import's stable-FBX ID generation.
  Directly guessing imported FBX sub-object IDs in scene YAML is not reliable.
- Cleared all four guessed model references. Runtime visual controllers now always restore the
  original colored primitive renderers when a model reference is null or cannot instantiate, so
  enemies and the weapon can never become invisible because of optional placeholder art.
- The verified FBX files all exist. The existing `RuleForge > Setup Milestone 11 Art Placeholders`
  menu loads them through Unity `AssetDatabase.LoadAssetAtPath<GameObject>` and serializes Unity's
  actual imported object IDs; the user must run that menu outside Play Mode because Codex is not
  allowed to start/control Unity for setup or gameplay testing.
- Runtime, Editor, EditMode-test, and PlayMode-test assemblies compile with exit code 0 after this
  change. No Unity instance or gameplay test was started by Codex; the user owns runtime retesting.

## Art binding and Chinese gameplay follow-up

- Unity-generated FBX local IDs are now serialized correctly after the user's Inspector setup.
  `Enemy.prefab` binds `characterMedium.fbx`; Arena binds `blaster-a.fbx`, `blaster-m.fbx`, and
  `blaster-f.fbx`. Grunt/Runner/Tank bind zombieA/zombieC/survivorFemaleA skins respectively.
- Arena also serializes the Kenney reticle, objective, health, ammo, and panel textures. The
  Space Station source assets exist, but Arena still has no `ArtPlaceholder` environment instances.
- The game was English because the earlier instruction intentionally removed localization from
  `GameplayHud`, leaving hard-coded English labels, and tool localization defaulted to English.
- The latest user instruction supersedes that: Gameplay HUD is now fixed Chinese, including goal
  progress, weapon/ammo, health/shield, damage source, rule effects/stacks, and victory/defeat.
  F1/F2/F3 now start in Chinese and retain their English toggle. Internal ChallengeSpec and DSL
  identity values remain unchanged.
- Runtime, Editor, EditMode-test, and PlayMode-test assemblies compile with exit code 0 after the
  Chinese UI change. No Unity instance or gameplay test was started by Codex.

## Remaining work

1. The user opens Arena after Unity finishes importing/recompiling and performs the Art Integration
   manual checklist recorded below. Do not ask the user to rerun the Milestone 11 bootstrap because
   it may overwrite their manually adjusted `a / m / f` weapon bindings and transforms.
2. If the actual Kenney model dimensions make any runtime Arena module visibly misaligned, adjust
   only `ArenaArtRuntime` layout positions/scales after the user supplies a Game-view screenshot;
   do not replace the preserved graybox colliders or spawn transforms.
3. Milestones 8, 9, and 10 still lack their exact PASS phrases; the consolidated final PASS may supersede them if the user chooses.
4. For portfolio statistics, the user runs the 50 prompts in `MILESTONE10_PROMPT_TEST_SET.md` with the real provider and captures real evidence using `PORTFOLIO_CAPTURE_CHECKLIST.md`. Codex must not invent or prefill results.
5. Preserve all work uncommitted until the user explicitly gives the final submission instruction. Then create one consolidated local commit, report its hash, and let the user perform the push.

## Art Integration / Visual Replacement

- Added `ArenaArtRuntime` to the existing Arena `Environment` object. It uses only the already
  imported Kenney Space Station Kit and builds a compact 20x20 presentation layer at runtime:
  tiled floor, modular perimeter walls/pillars, five cover pieces, a raised rear deck/ramp/rails,
  and two control stations.
- Existing `Ground`, `Walls`, and three `Cover` objects remain untouched as collision roots. Their
  primitive renderers are hidden only after at least one formal art instance was created. Imported
  model colliders are disabled, so third-party hierarchies cannot change existing gameplay physics.
  The two additional spawn shields and raised deck use separate named BoxCollider proxy objects.
- `PlayerSpawn`, all four `EnemySpawnPoints`, `GameSystems`, RuleEngine, ChallengeSpec, runtime
  stats, enemy AI, and weapon configs were not modified by the Arena art layer.
- Preserved the user's serialized first-person weapon selection and transforms: Assault Rifle =
  `blaster-a`, Shotgun = `blaster-m`, Sniper = `blaster-f`. No Milestone 11 bootstrap was executed.
- Added `WeaponFireVisualController`. Existing raycast/hitscan remains authoritative for damage.
  Every accepted shot now creates a pooled short-lived cyan tracer from the equipped model's
  forward-most renderer bound to the real raycast hit point, or to max range on a miss. It also
  creates pooled muzzle and impact flashes; all visual objects have no damage role.
- Existing enemy art remains `characterMedium` under the Enemy gameplay root with zombieA,
  zombieC, and survivorFemaleA skin bindings. The capsule collider and Enemy scripts remain on
  the root, and the primitive renderer remains only as a safe fallback.
- Lighting was adjusted without changing render pipeline: cool directional light at 1.15 and a
  brighter blue-gray flat ambient light.
- Static verification: runtime, editor, edit-mode-test, and play-mode-test assemblies all compiled
  with exit code 0 using Unity 2022.3.62f3c1 Bee/Roslyn references. Tests were not executed and no
  Unity or PlayMode instance was started. Arena YAML has no duplicate object IDs; all non-builtin
  asset GUIDs resolve to project meta files.
- Manual Unity verification required: confirm Space Station floor/walls/covers are visible and the
  gray primitives are hidden; walk and shoot into all old/new cover colliders; confirm four spawn
  points remain outside walls; switch 1/2/3 and inspect each weapon; fire at an enemy, wall, and sky
  to verify muzzle flash, tracer direction, real impact point, and max-range miss tracer; confirm
  enemies retain visible model/skin, collision, movement, damage, and death; confirm F1/F2/F3,
  RuleEngine, goals, and Chinese HUD behave as before.

## Resume protocol

After any context compression, read this file first, then run:

```powershell
git status --short --branch
git diff --stat
git diff
```

Continue from the remaining-work list without repeating completed implementation.
