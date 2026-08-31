# RuleForge

RuleForge is a Unity FPS rule sandbox where players create gameplay challenges through a manual Creator or natural-language AI planning. AI output is constrained to a structured DSL, validated against trusted vocabulary and balance limits, shown as an editable preview, and activated only after the player confirms it.

## V0.1 status

- Milestones 1–7 have passed manual acceptance, except Milestone 5 did not receive the exact final PASS phrase before work continued.
- Milestones 8–10 are implemented and awaiting consolidated manual Unity acceptance.
- The project uses Unity `2022.3.62f3c1` LTS and the `dev` branch for current development.

## Core flow

```text
Natural-language request or manual Creator
-> structured ChallengeSpec / ChallengePatch
-> Schema + Semantic + Range + Complexity validation
-> Risk / Reward evaluation
-> editable Creator preview
-> player-controlled Validate & Play
-> generic RuleEngine and RuntimeStats
-> gameplay and real-data analytics
```

AI never generates or executes C# and never activates a challenge automatically. Runtime accepts only structured data that passes the trusted Validator.

## Open and initialize

1. Add this repository root in Unity Hub and open it with Unity `2022.3.62f3c1`.
2. Open `Assets/Scenes/Arena.unity`.
3. Exit Play Mode, then run `RuleForge > Setup Milestone 10 Analytics & Showcases` once. This idempotent setup applies all prerequisite scene/config setup from earlier milestones.
4. After Kenney assets finish importing, run `RuleForge > Setup Milestone 11 Art Placeholders` once. This binds the verified placeholder models, skins, HUD icons, and crosshair without replacing gameplay colliders.
5. Enter Play Mode.

Runtime controls:

- `WASD`, mouse, jump: FPS movement.
- Mouse button: fire; `R`: reload; `1/2/3`: switch weapon.
- `F1`: runtime tuning and stat breakdown.
- `F2`: manual Creator and AI preview.
- `F3`: real-data analytics dashboard.

Opening any runtime tool releases the cursor and blocks gameplay input until every open tool is closed.

## Manual acceptance

Codex does not start Unity or run PlayMode tests for this project. Use:

- `FINAL_ACCEPTANCE.md` for the consolidated final verification.
- `MILESTONE11_ART_ACCEPTANCE.md` for the developer-owned placeholder-art binding checks.
- `MILESTONE8_ACCEPTANCE.md`, `MILESTONE9_ACCEPTANCE.md`, and `MILESTONE10_ACCEPTANCE.md` for detailed checks.
- `MILESTONE10_PROMPT_TEST_SET.md` for real-provider benchmark inputs.
- `PORTFOLIO_CAPTURE_CHECKLIST.md` for evidence capture.

Analytics are written under `Application.persistentDataPath/RuleForgeAnalytics`. Mock samples remain identifiable and are excluded from real-provider benchmark rates.

## Repository policy

- `main`: stable accepted versions.
- `dev`: current development.
- Commit `Assets/`, `Packages/`, and `ProjectSettings/`.
- Never commit `Library/`, `Temp/`, `Logs/`, `Obj/`, analytics output, API keys, or builds.
- The developer performs remote pushes.

## Third-party placeholder art

The approved Kenney CC0 placeholder-art source files and their original license records live in
`Assets/ThirdParty/Kenney`. See `Assets/ThirdParty/Kenney/README.md` for package versions, official
sources, archive hashes, imported subsets, and the intended RuleForge mapping. Run the Milestone 11
Editor menu after import to bind the art to the gameplay prefab and Arena scene.
