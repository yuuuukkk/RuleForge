# RuleForge

RuleForge is a Unity FPS demo in which players describe a challenge in natural language, review an AI gameplay proposal, and then play or refine the validated result. The AI helps clarify vague requests and explain design trade-offs; it does not directly execute player text. Manual parameters and raw rule data remain in Advanced/Developer views.

## V0.1 status

- Milestones 1–7 have passed manual acceptance, except Milestone 5 did not receive the exact final PASS phrase before work continued.
- Milestones 8–10 and subsequent Creator/gameplay-feedback updates are implemented, but the latest changes still need manual Unity and live-provider acceptance.
- The project uses Unity `2022.3.62f3c1` LTS and the `dev` branch for current development.

## Core flow

```text
Natural-language request
-> AI gameplay proposal -> player review/refinement
-> structured ChallengeSpec or minimal ChallengePatch
-> intent preservation + trusted Validator + Risk/Reward evaluation
-> readable preview or before/after diff (with player-approved repair if needed)
-> player-controlled Play or Apply
-> generic RuleEngine and RuntimeStats
-> gameplay feedback and analytics
```

AI never generates or executes C# and never activates a challenge automatically. Examples and suggestion buttons only fill natural-language input; they do not load fixed challenges. The Validator remains the final legality boundary, and the player confirms a generated or repaired challenge before it is applied.

## Open and initialize

1. Add this repository root in Unity Hub and open it with Unity `2022.3.62f3c1`.
2. Open `Assets/Scenes/Arena.unity`.
3. Exit Play Mode, then run `RuleForge > Setup Milestone 10 Analytics & Showcases` once. This idempotent setup applies all prerequisite scene/config setup from earlier milestones.
4. After Kenney assets finish importing, run `RuleForge > Setup Milestone 11 Art Placeholders` once. This binds the verified placeholder models, skins, HUD icons, and crosshair without replacing gameplay colliders.
5. Enter Play Mode. The opening menu offers Start Game, Edit Challenge, and Display Settings. In Creator, describe an idea, review the AI proposal, generate a validated preview, and choose Play. After a run, describe what should change and review the proposed diff before applying it.

### AI provider for the shared playtest

- A revocable DeepSeek playtest key is deliberately embedded in the current `dev` source. On a fresh installation without a saved provider choice, DeepSeek is selected by default. This key is extractable from any distributed client; set its quota and revoke/rotate it when the playtest ends. Do not use it as a production secret.
- Creator > AI Settings shows the selected provider, model, key source, and real connection status. Verify the connection before relying on AI generation. Network access, a valid model, and available quota are still required; a configured key alone does not prove the API works.
- A player may select another provider and enter a personal key. A locally entered key overrides the embedded DeepSeek key for that provider; provider-specific environment variables are also supported.
- Existing downloadable builds are not updated by a source push. Rebuild and redistribute the Windows ZIP to include the latest Creator, gameplay, and key changes.

Runtime controls:

- `WASD`, mouse, jump: FPS movement.
- Mouse button: fire; `R`: reload; `1/2/3`: switch weapon.
- `F1`: runtime tuning and stat breakdown.
- `F2`: Creator, AI proposal/preview, and advanced editing.
- `F3`: real-data analytics dashboard.
- `Esc`: pause/menu during a run.

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
- Never commit `Library/`, `Temp/`, `Logs/`, `Obj/`, analytics output, or personal/production API keys. The intentionally shared DeepSeek playtest key above is an explicit exception; no other key should be committed.
- Builds are distributed separately from source updates; avoid adding new build artifacts to a source commit unless deliberately preparing a playable download.

## Third-party placeholder art

The approved Kenney CC0 placeholder-art source files and their original license records live in
`Assets/ThirdParty/Kenney`. See `Assets/ThirdParty/Kenney/README.md` for package versions, official
sources, archive hashes, imported subsets, and the intended RuleForge mapping. Run the Milestone 11
Editor menu after import to bind the art to the gameplay prefab and Arena scene.
