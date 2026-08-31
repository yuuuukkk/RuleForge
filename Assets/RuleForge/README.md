# RuleForge source layout

| Folder | Responsibility |
| --- | --- |
| `AI` | Provider abstraction, strict structured output, and minimal ChallengePatch application |
| `Analytics` | Append-only AI/gameplay evidence and truthful summary calculation |
| `Challenge` | JSON challenge and patch examples |
| `Config` | Designer-owned ScriptableObject bases, trusted effect identity, and limits |
| `DSL` | Serializable ChallengeSpec, rules, conditions, effects, scaling, duration, and goals |
| `Rules` | Event bus, generic RuleEngine, conditions, effects, stacking, scaling, and timed modifiers |
| `Runtime` | Runtime stats, modifiers, services, and goal execution |
| `Validation` | Schema, semantic, range, complexity, and Risk / Reward evaluation |
| `Player`, `Weapons`, `Enemies` | FPS sandbox runtime and data-driven profiles |
| `UI` | Creator, gameplay HUD, analytics dashboard, and shared input gating |
| `Debug` | Runtime tuning and stat breakdown tools |
| `Tests` | Minimal EditMode and legacy PlayMode coverage |

Runtime gameplay values flow from Config assets into mutable RuntimeStats. Rule effects modify only RuntimeStats/services; they do not mutate ScriptableObject assets. Concrete showcase behavior is expressed in JSON, not case-specific gameplay scripts.
