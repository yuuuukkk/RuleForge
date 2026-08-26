# RuleForge source layout

These folders reserve the architecture described by the product specification. They intentionally contain no runtime implementation during Milestone 0.

| Folder | Intended responsibility |
| --- | --- |
| `AI` | Provider abstraction and structured-output adapters |
| `Config` | Designer-owned ScriptableObject configuration |
| `DSL` | Serializable challenge and rule schema |
| `Challenge` | Challenge loading, compilation, and patching |
| `Rules` | Generic rule evaluation and execution |
| `Runtime` | Runtime stats, modifiers, and services |
| `Validation` | Schema, semantic, range, complexity, and balance checks |
| `Player`, `Weapons`, `Enemies` | FPS sandbox components |
| `UI` | Creator and runtime UI |
| `Analytics` | Generation and playtest metrics |
| `Debug` | Tuning panel and stat breakdown |
| `Tests` | EditMode and PlayMode tests |
| `Scenes` | RuleForge-owned scene assets beyond the top-level Arena scene |
