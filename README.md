# VillageCraft — The Living Overworld

A 3D village management game written in C#, inspired by Minecraft villages and city builders such as Cities: Skylines. Place complete prefabs, organize workers and supplies, and protect a growing settlement.

> **Unofficial fan-made project.** VillageCraft is not affiliated with, endorsed by, or an official product of Mojang Studios or Microsoft. Minecraft and its associated assets belong to their respective owners.

## Development collaboration

**GPT 6.1 Sol works on the game code and logic.** The repository owner acts as QA, guiding development through prompts and testing and validating code changes. This is an ongoing development collaboration.

## Contents

- [About the game](#about-the-game)
- [Screenshots](#screenshots)
- [Requirements and platforms](#requirements-and-platforms)
- [Minecraft 1.16 assets](#minecraft-116-assets)
- [Build and run](#build-and-run)
- [Getting started](#getting-started)
- [Controls](#controls)
- [Economy and logistics](#economy-and-logistics)
- [Villagers and professions](#villagers-and-professions)
- [World, daylight and defense](#world-daylight-and-defense)
- [Interface and options](#interface-and-options)
- [Saving and loading](#saving-and-loading)
- [Content and modding](#content-and-modding)
- [JavaScript behaviors](#javascript-behaviors)
- [Project structure](#project-structure)
- [Logging](#logging)
- [Development checks](#development-checks)
- [Publishing](#publishing)
- [Credits and asset ownership](#credits-and-asset-ownership)

## About the game

VillageCraft focuses on managing a living village rather than placing individual blocks. Roads connect homes, farms, workshops, warehouses and defenses. Villagers travel to work, deliverymen transport supplies, and production depends on staffed buildings with the right inputs.

The game includes farming, forestry, mining, cave expeditions, animal husbandry, trade, equipment upgrades, guardians and hostile encounters. Building definitions, models, professions and balance settings are external TOML files; JavaScript components provide custom behavior.

The project is under active development. Features and save formats may evolve; back up important saves before updating. The project file is the source of truth for the current build version. Requirements recorded in AGENTS.md may include work that has not shipped yet.

## Screenshots

Captured in-game with Minecraft 1.16 assets supplied separately. These images show the English interface; Brazilian Portuguese is the default language.

### Build a village

Place complete buildings and connect the settlement with roads.

![Village overview](images/village-overview.png)

### Choose buildings and roads

The compact construction menu previews prefabs and their costs.

![Construction menu](images/building-menu.png)

### Manage villagers

Inspect residents and reassign professions as the village grows.

![Villager management](images/villager-management.png)

### Defend through the night

Regular encounters continue at game-clock intervals until 05:00.

![Night encounters](images/night-encounters.png)

### Review a lost village

The game-over screen shows save statistics and offers retry or save deletion.

![Save statistics](images/save-statistics.png)

## Requirements and platforms

- **.NET 10 SDK** to build and run from source.
- A graphical desktop with **OpenGL 3.3** support.
- Separately supplied local Minecraft assets, as described below.

Windows is tested locally. Linux and macOS are targets through the native dependencies and still require platform validation. Linux may require additional system packages for OpenGL, GLFW and X11.

The main libraries are Raylib-cs for rendering, ImGui.NET through rlImgui-cs for the interface, Tomlyn for content, and Jint for JavaScript scripting.

## Minecraft 1.16 assets

**Minecraft textures and other proprietary game assets are not included in this repository.** The development setup uses assets from **Minecraft 1.16**. Supply assets separately from your own installation or another source you are authorized to use. This project does not provide an asset download or grant redistribution rights.

Set `VILLAGECRAFT_ASSETS` to your asset directory. Assets are read directly from there and are **not copied on every build or publish**.

### Used-asset cache

The game resolves its referenced Minecraft textures at startup and writes only those files to `<executable>/UserData/cache.zip`. The archive is loaded once into memory on later launches; cached assets take priority, so the original folders are not needed for cached files. Newly referenced files are resolved externally and added to the cache. Additional assets requested during play are saved on shutdown.

After replacing textures or changing the asset source, delete `UserData/cache.zip` while the game is closed to rebuild it. A corrupt cache is logged and rebuilt from external assets. This local cache contains proprietary Minecraft files and must not be committed or distributed.

### Indexed assets

The resolver accepts `assets_index.json` with name-to-path entries or Minecraft-style `objects` entries containing hashes. Keep referenced files in their corresponding locations:

```text
MinecraftAssets/
├── assets_index.json
├── objects/
│   └── <first two hash characters>/
│       └── <full hash>
└── assets/                         # Optional loose overrides
    └── minecraft/
        └── textures/
            ├── block/
            ├── entity/
            └── item/
```

Indexed ZIP resource packs are supported when their textures use the expected `assets/minecraft/textures/...` layout. An index alone does not contain textures: its referenced objects or packs must also be present. A standard asset index may not contain every required texture; loose files can supply missing assets.

### Loose textures and overrides

A `textures/` directory at the asset root is also supported, including a repository-root folder during development. Keep the extracted layout, for example `textures/entity/piglin/piglin.png` and `textures/block/oak_planks.png`. This folder is ignored by Git and is read directly without build-time copying.

Extracted PNGs can be supplied beneath `assets/minecraft/textures/`, for example:

```text
MinecraftAssets/assets/minecraft/textures/block/oak_planks.png
MinecraftAssets/assets/minecraft/textures/entity/villager/villager.png
```

Nether enemies require these Java 1.16 skin paths beneath `assets/minecraft/textures/`:

- `entity/piglin/piglin.png`
- `entity/piglin/piglin_brute.png`
- `entity/piglin/zombified_piglin.png`
- `entity/blaze.png`

The indexed Programmer Art pack alone does not contain the Piglin skins. Missing textures generate a warning once per texture per run in `UserData/Logs`; models use fallback colors until the correct files are supplied. Ordinary pig skins and the obsolete `entity/zombie_pigman.png` path are not substitutes.

Loose textures take priority over indexed textures. Preserve logical paths to avoid ambiguous filenames. Missing textures fall back to model colors. Restart after changing assets.

The build expects the locally supplied `mojangles.ttf` at the repository root and copies it to `Content/Fonts/`. Supply it separately if absent. Its ownership and redistribution rights remain separate from the project's code.

## Build and run

From the repository root:

**PowerShell:**

```powershell
$env:VILLAGECRAFT_ASSETS = 'C:\MinecraftAssets'
dotnet run --project VillageCraft -c Release
```

The helper preserves an existing asset setting, or defaults it to the repository root:

```powershell
./run.ps1
```

**Linux / macOS shell:**

```sh
export VILLAGECRAFT_ASSETS="$HOME/MinecraftAssets"
dotnet run --project VillageCraft -c Release
```

To build without launching:

```sh
dotnet build VillageCraft -c Release
```

Without the variable, development discovery checks parent directories for `assets_index.json` and the working directory. An explicit asset path is recommended for distributed builds.

## Getting started

1. Create a village. The default map is **32 × 32**; larger dimensions, terrain seed and tutorial are selectable.
2. Connect entrances to roads leading to the central square. Rotate previews so entrances face the road.
3. Inspect the starting warehouse, homes and workplaces to check workers and local supplies.
4. Maintain food, water and housing, and keep deliverymen available to supply production.
5. Expand, trade surplus, and prepare defenses before hostile encounters begin.

The guided tutorial covers inspection, roads, forestry, water, housing and defense. Skipping it is remembered across new games. **Options → Reset tutorial** enables it again.

## Controls

| Input | Action |
|---|---|
| Left click | Inspect, or place the selected prefab |
| Right click | Inspect and cancel construction |
| Tab | Cancel building or toggle the building inventory |
| R | Rotate the prefab by 90° |
| WASD | Pan the camera |
| Q / E | Rotate the camera |
| Shift + Q / E | Rotate faster |
| Mouse wheel | Zoom |
| Top-row 1 / 2 / 3 / 4 | Select 1× / 2× / 4× / 16× simulation speed |
| Space | Pause or resume |
| Escape | Open or close the game menu |

Number-pad keys do not control simulation speed. The fourth tier is four times faster than the third.

## Economy and logistics

- **Emeralds** pay for construction, training, upkeep and upgrades. Trade and daily settlement provide income.
- **Goods** represent labor and combat contributions. They convert to emeralds at **06:00**, with upkeep accounted for during settlement.
- **Physical resources** remain in local storage until transported. Deliverymen bring inputs to workplaces and move outputs to warehouses.
- **Small and large warehouses** hold resources and provide delivery jobs. Emeralds and Goods remain in the treasury.
- **Trade** uses market storage. Sale requests arrange deliveries; complete the sale once enough stock has arrived.

All professions can automatically fetch wood/stone and repair damaged buildings. Couriers prioritize active deliveries, and defenders prioritize combat. Repair costs, work time and amount are configurable in `economy.toml`.

Delivery scheduling prioritizes empty input buffers and protects feed from being collected as surplus. Villagers prefer connected roads and use building entrances rather than crossing through buildings.

Inspect buildings to identify missing inputs. Connection and staffing alone do not supply a workplace.

Caves provide valuable materials for equipment upgrades. A staffed forge enables armor and tool improvements. Leather supports armor production, and wizards can enchant armor. Cow sheds produce leather and milk; milk removes negative effects and helps injured villagers recover.

## Villagers and professions

Villagers have persistent names, professions, health, homes, workplaces and inventories. Inspect them directly or use the People panel to review status and train professions.

Specialists include farmers, woodcutters, miners, explorers, smiths, animal keepers, traders, deliverymen and defenders. **Citizens** can assist workplaces at half specialist efficiency.

Workers travel to connected workplaces. Ordinary villagers return home at night; defenders patrol and engage threats. Food, water, services and taxes affect happiness. Employment decisions and daily routines are editable scripts. The People panel includes **Reassign all professions**, assigning specialists to connected workplaces and leaving surplus residents as citizens.

Each eligible home with at least two residents and a free bed has a **60% daily birth chance** by default. Newborns remain hidden for one day, then join the village as fully grown citizens. Change the chance in Options or `game.toml`; pending babies are saved.

## World, daylight and defense

Terrain includes water, stepped mountains, grass and natural trees. Adjacent elevations differ by at most one block. Woodcutters harvest tree entities, which regrow before another harvest.

Night ambient lighting keeps the village readable; tune `NightAmbient` in `rendering.toml` if needed. Sunlight damage follows solar strength independently of this visual setting.

The clock advances continuously through minutes: **00:00 midnight**, **06:00 sunrise**, **12:00 noon**, **18:00 sunset**. A default day lasts ten real minutes at 1× speed.

New villages receive five peaceful nights. After that, regular monsters spawn repeatedly from **18:00 until 05:00**, at a default interval of **60 in-game minutes**. Options controls the interval and group size; spawning respects the hostile cap and resumes correctly after saving/loading. Boss waves default to every **30 days**. Options adjusts these rules; existing saves retain their configured values.

Zombies and skeletons burn in exposed daylight unless protected by helmets or shelter. Regular spawns can wear leather helmets; susceptible boss-wave monsters always do. Walls, gates, combat villagers and golems defend the village. Torches and the torch workshop/crafting system have been removed in 1.0.6.

Losing all villagers pauses the game on a statistics screen. Reload a living save to retry, start again, or use the red delete-save confirmation. Defeat does not automatically overwrite the previous save.

## Interface and options

The compact Minecraft-inspired interface uses pixel icons, a hotbar and the locally supplied Mojangles font. Hover for labels and help. The building inventory groups prefabs by category and supports search.

Inspection is the default mouse tool. New selections close unpinned panels; pinned panels stay open. Management and inspector panels can be moved and resized.

Localization uses stable IDs such as `ui.options = "Options"`, with a usage comment above every translation. See the [localization guide](VillageCraft/Content/Locales/README.md) for adding languages and preserving format arguments.

Options include language, UI scale, quality, shadows, water, frame limit, resolution and windowed/fullscreen/borderless modes. **Brazilian Portuguese is the default language**; English is included and more languages can be added through TOML.

## Saving and loading

```text
<executable directory>/UserData/
├── options.toml
└── <save name>.vcs
```

With `dotnet run`, this is normally `VillageCraft/bin/Release/net10.0/UserData/`. The executable directory must be writable.

Use **Escape → Save Game**, **Save Game As**, or **Load Game**. Save As supports a new name and confirms replacement of an existing save.

Saves use versioned binary records, a `VCTY` header and SHA-256 integrity checks, without `BinaryFormatter`. **Version 1.0.6 uses beta save format 11 with numeric resource IDs. Older beta saves are not supported and are never automatically overwritten.** Back up saves before updates. Game definitions/preferences use TOML; the external asset index remains JSON.

## Content and modding

Content loads and validates once at startup. Restart after editing. Keep IDs stable because models, scripts and saves refer to them.

| Location under `VillageCraft/Content/` | Purpose |
|---|---|
| `Prefabs/` | Buildings/roads: costs, jobs, recipes, entrances and script binding |
| `Models/` | Geometry, materials and texture UVs |
| `Professions/` | Appearance, eligible jobs, training and efficiency |
| `Units/` | Combat models, statistics and AI binding |
| `Locales/` | Interface and content translations |
| `Scripts/` | Editable JavaScript components and shared helpers |
| `Templates/` | Authoring examples; `.example` files are inactive |
| `game.toml` | New-game rules and starter layout |
| `resources.toml`, `economy.toml` | Resource definitions, prices and economic settings |
| `upgrades.toml` | Equipment recipes and effects |
| `terrain.toml`, `tutorial.toml` | Terrain and tutorial settings |
| `sunlight.toml`, `rendering.toml` | Sunlight damage and rendering |
| `names.toml`, `ui_icons.toml` | Name pools and pixel icons |

C# stocks, costs, recipes, inventory, cargo and production statistics use **`ResourceType` enum keys**. TOML and JavaScript keep readable names, validated at the boundary. Adding a resource type requires a stable enum member and recompilation alongside its TOML definition. Buildings, professions and models remain extensible through content files.

See the [content guide](VillageCraft/Content/README.md), [JavaScript API](VillageCraft/Content/SCRIPTING.md), and [profession template](VillageCraft/Content/Templates/profession.toml.example).

`tools/New-Catalog.ps1` regenerates definitions and **overwrites manual edits**. The game never invokes it automatically.

## JavaScript behaviors

Jint binds scripts directly to live game APIs without JSON conversion. Scripts support lifecycle callbacks, world events, live queries, queued mutations and per-instance tags.

| Component | Behavior |
|---|---|
| `villager.js` | Work schedule, supplies, deliveries, forestry, automatic repairs and defense |
| `employment.js` | Job allocation and bulk profession reassignment |
| `population.js` | Configurable daily birth rolls |
| `melee.js`, `ranged.js`, `creeper.js` | Enemy attack and movement decisions |
| `_ai.js` | Shared queries and initial enemy target selection |
| `guardian.js`, `animal.js` | Guardian combat/patrol and animal movement |
| `building_<id>.js` | A separate component for each shipped prefab type |
| `building.js` | Generic fallback for new buildings |

Each prefab's TOML selects its own script, so changing chicken-coop behavior does not change pig-pen behavior. Type or instance overrides can replace the shipped defaults. Shared libraries begin with `_` and are not standalone components.

Use constants such as `resources.Food` and `tasks.GoingToWork` in scripts. C# provides validated navigation, combat, storage transfers and production operations; JavaScript decides when, where and on whom to act. Clock, persistence, damage rules and global simulation systems remain in C#.

Editor autocomplete and type checking are provided by [`game.d.ts`](VillageCraft/Content/game.d.ts) and the editor-only `jsconfig.json`. Open `VillageCraft/Content/` in a JavaScript/TypeScript editor, or run `tsc --project VillageCraft/Content/jsconfig.json`.

Restart after edits. JavaScript globals are per-component/per-world and are not saved. Refer to the API guide for persisted entity fields, callbacks and examples.

## Project structure

```text
VillageCraft/
├── VillageCraft.csproj
├── Program.cs
├── City*.cs                 # Simulation systems and validated actions
├── GameUi*.cs               # Interface and menus
├── Scene.cs                 # World rendering
├── Assets.cs                # External asset discovery
├── BinarySave.cs            # Save format
├── ResourceType.cs          # Stable resource IDs and boundary converters
├── Script*.cs               # JavaScript runtime and live bindings
└── Content/                 # Definitions, scripts and guides
screenshots/                 # Ignored game-test captures
run.ps1                      # Development launcher
AGENTS.md                    # Owner's project instructions
README.md
```

## Logging

Every run creates a UTF-8 log in `<executable directory>/UserData/Logs/`. Log timestamps are UTC. Logs capture console diagnostics, unhandled managed errors, renderer messages and script exceptions with their component names and stack traces.

JavaScript `world.log(message)`, `world.warn(message)` and `world.error(message)` always write to the run log, including in Release builds. Short `log` messages also appear in the village journal. Logging does not require a debug flag. Generated logs stay outside version control.

## Development checks

```sh
dotnet run --project VillageCraft -c Debug -- --self-test
```

Checks cover simulation, scripted behavior/overrides, physical deliveries, terrain, resources, births, repairs and binary save integrity. Development tests, smoke-test modes and UI test hooks are excluded from **Release** builds. Save test captures in repository-root `screenshots/`.

## Publishing

For a self-contained Windows x64 build:

```sh
dotnet publish VillageCraft -c Release -r win-x64 --self-contained true -o dist/win-x64
```

Use the appropriate runtime identifier and validate on other platforms. Distribute the **entire publish directory**, including native libraries and `Content/`.

**Publishing does not bundle Minecraft assets.** Users must point `VILLAGECRAFT_ASSETS` to their own external assets. Review font and other third-party asset permissions before distribution.

## Credits and asset ownership

VillageCraft is an independent fan project inspired by Minecraft and city management games. Development textures come from **Minecraft 1.16**, belong to their respective owners and are not included in this repository.

C# systems, prefab geometry, configuration and scripts are maintained separately from those assets. Third-party libraries retain their own licenses. This repository does not imply permission to redistribute Minecraft textures, sounds or fonts.
