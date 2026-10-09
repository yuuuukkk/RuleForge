# RuleForge

RuleForge is an AI Gameplay UGC / FPS rule sandbox. The project is being built strictly milestone by milestone; the current repository contains **Milestone 0 only**.

## Current status

- Unity project scaffold created for the locally installed Unity `2022.3.62f3c1` LTS editor.
- Arena scene scaffold created at `Assets/Scenes/Arena.unity`.
- RuleForge module directories created under `Assets/RuleForge/`.
- No FPS runtime, Rule Engine, Creator UI, or AI implementation has been added.

## Open the project

1. Install Unity Hub and Unity `2022.3.62f3c1`.
2. In Unity Hub, choose **Add > Add project from disk** and select this repository root.
3. Open `Assets/Scenes/Arena.unity`.
4. Keep the project on `2022.3.62f3c1` during V0.1 unless an upgrade is explicitly approved.

## Repository policy

- `main`: stable, accepted milestones.
- `dev`: day-to-day development.
- Commit `Assets/`, `Packages/`, and `ProjectSettings/`.
- Never commit `Library/`, `Temp/`, `Logs/`, `Obj/`, or build output.
