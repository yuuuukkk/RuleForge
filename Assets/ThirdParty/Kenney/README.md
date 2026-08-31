# Kenney placeholder art

This folder contains the approved placeholder-art selection for the RuleForge FPS demo.
Every imported asset was downloaded from an official Kenney page and is distributed under
Creative Commons Zero 1.0. The original license text is retained inside each package folder.

CC0 permits personal, educational, and commercial use. Attribution to Kenney is appreciated
but is not required by the included license files.

## Imported packages

| Package | Version | Official source | SHA-256 of downloaded ZIP | Imported subset |
| --- | --- | --- | --- | --- |
| Space Station Kit | 1.0 | https://kenney.nl/assets/space-station-kit | `215E79BD5415CFF93665183390F0343ED9ACF87780306331013B78520170C6D8` | All 97 FBX models and their colormap texture |
| Animated Characters Survivors | 1.1 | https://kenney.nl/assets/animated-characters-survivors | `FDADCED07A0454C9B7F0B46507BE6144A072B4D03B4FFA37F225893C76C62845` | Character model, idle/run/jump animations, and four skins |
| Blaster Kit | 2.1 | https://kenney.nl/assets/blaster-kit | `91E3093E95427D59625E7E2CE2D0399B861600160FD0B4ADA7714796B67CEA8C` | All 40 FBX models and their colormap texture |
| UI Pack: Sci-fi | 2.0 | https://kenney.nl/assets/ui-pack-sci-fi | `4AE5A4949B71BA6C08BFB4D4708B3880915782F7DEAE7BC5872E1D56F0A668AF` | Double-resolution Blue, Red, Grey, and Extra PNGs plus two fonts |
| Game Icons | source license has no package version | https://kenney.nl/assets/game-icons | `7A86D8D58E0B851E22004B3C70BF90B003632BBF9AC633424DAA3BB17D9E7E4E` | 105 white double-resolution PNG icons |
| Crosshair Pack | 1.1 | https://kenney.nl/assets/crosshair-pack | `26CF8F3E135F8C9A3354A8A6E6C2576E78BD8E2DB01C841F93AB43AD3205A78F` | 200 outline PNG crosshairs |

The older `Animated Characters 3` pack from the initial shortlist is no longer present on
Kenney's current asset catalog. It was replaced during import by the maintained
`Animated Characters Survivors` package from Kenney's official site instead of using a mirror.

## Import policy

- FBX is retained for Unity's built-in model importer.
- Duplicate OBJ, GLB, SVG, source-art, preview, and alternate-resolution files are intentionally
  omitted to avoid unnecessary project size and import time.
- The source-art layer is wired through the idempotent Editor menu
  `RuleForge > Setup Milestone 11 Art Placeholders`. Running the binding keeps the existing gameplay
  colliders/physics and adds visual-only placeholder instances for the Arena environment,
  enemy skins, three weapon models, HUD icons, and the crosshair.
- Do not remove the package `LICENSE.txt` files when selecting or relocating individual assets.

## Intended RuleForge mapping

- `SpaceStationKit`: modular Arena environment and props.
- `AnimatedCharactersSurvivors`: visual bases for Grunt, Runner, and Tank placeholders.
- `BlasterKit`: visual bases for Assault Rifle, Shotgun, and Sniper placeholders.
- `UISciFi`, `GameIcons`, and `CrosshairPack`: HUD panel skin, objective/health/ammo indicators,
  runtime panels, weapon indicators, and reticle.

The Milestone 11 menu uses these concrete bindings:

- Enemy skins: Grunt → `zombieA`, Runner → `zombieC`, Tank → `survivorFemaleA`.
- Weapon models: Assault Rifle → `blaster-a`, Shotgun → `blaster-j`, Sniper → `blaster-r`.
- HUD textures: `crosshair-000`, `target`, `cross`, `barsHorizontal`, and the Sci-Fi
  `Extra/panel_rectangle`.
- Environment visuals: `floor`, `wall`, `door-double`, `container`, `container-tall`, and
  `computer-screen`, placed under `Environment/ArtPlaceholder` without gameplay colliders.
