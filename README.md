# VillageCraft — The Living Overworld

A 3D village management game written in C#, inspired by Minecraft villages and city builders such as Cities: Skylines. Build with complete prefabs, organize workers and supplies, and protect a growing settlement.

> **Unofficial fan-made project.** VillageCraft is not affiliated with, endorsed by, or an official product of Mojang Studios or Microsoft. Minecraft and its associated assets belong to their respective owners.

## Development collaboration

**GPT 6.1 Sol works on the game code and logic.** The repository owner acts as QA, guiding development through prompts and testing and validating all code changes. This project is developed through that Human and AI collaboration.

## Contents

- [About the game](#about-the-game)
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
- [Project structure](#project-structure)
- [Publishing](#publishing)
- [Credits and asset ownership](#credits-and-asset-ownership)

## About the game

VillageCraft focuses on managing a living village rather than placing individual blocks. Roads connect homes, farms, workshops, warehouses and defenses. Villagers travel to work, deliverymen transport supplies, and production depends on staffed buildings with the right inputs.

The game includes farming, forestry, mining, cave expeditions, animal husbandry, trade, equipment upgrades, guardians and hostile encounters. Building definitions, models, professions and balance settings are external TOML files; JavaScript components provide custom behavior.

The project is under active development. Features and save formats may evolve; back up important saves before updating. The project file is the source of truth for the current build version.

## Requirements and platforms

- **.NET 10 SDK** to build and run from source.
- A graphical desktop and graphics hardware supporting **OpenGL 3.3**.
- A separately supplied local asset directory, described below.

Windows is tested locally. Linux and macOS are targets through the native dependencies, but still require platform validation. Linux may require additional system packages for OpenGL, GLFW and X11.

The main libraries are Raylib-cs for rendering, ImGui.NET through rlImgui-cs for the interface, Tomlyn for content, and Jint for JavaScript scripting.

## Minecraft 1.16 assets

**Minecraft textures and other proprietary game assets are not included in this repository.** The development setup uses assets from **Minecraft 1.16**. Obtain the assets separately from your own installation or another source you are authorized to use. This project does not provide an asset download or grant redistribution rights.

Set `VILLAGECRAFT_ASSETS` to your local asset directory. Assets are read directly from that location and are **not copied on every build or publish**.

### Indexed assets

The resolver accepts `assets_index.json` with name-to-path entries or Minecraft-style `objects` entries containing hashes. Keep the referenced files in the corresponding locations:

```text
MinecraftAssets/
├── assets_index.json
├── objects/
│   └── <first two hash characters>/
│       └── <full hash>
└── assets/                         # Optional loose texture overrides
    └── minecraft/
        └── textures/
            ├── block/
            ├── entity/
            └── item/
```

Indexed ZIP resource packs are also supported. Their textures must use the expected `assets/minecraft/textures/...` layout. An index alone does not contain the textures: its referenced objects or resource packs must also be present. A standard asset index may not contain every required block or entity texture; loose textures can supply missing files.

### Loose textures and overrides

You can instead supply extracted PNG textures beneath `assets/minecraft/textures/`. For example:

```text
MinecraftAssets/assets/minecraft/textures/block/oak_planks.png
MinecraftAssets/assets/minecraft/textures/entity/villager/villager.png
```

Loose textures take priority over indexed textures. Preserve logical paths to avoid ambiguous filenames. Missing textures fall back to model colors. Restart the game after changing assets.

The interface looks for `mojangles.ttf` at the repository root when building and copies it to `Content/Fonts/` in the output. Supply that font locally if it is absent; its ownership and redistribution rights remain separate from this project's code.

## Build and run

From the repository root, configure assets and launch the game.

**PowerShell:**

```powershell
$env:VILLAGECRAFT_ASSETS = 'C:\MinecraftAssets'
dotnet run --project VillageCraft -c Release
```

The included helper preserves an existing asset setting, or defaults it to the repository root:

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

Without the environment variable, development discovery checks parent directories for `assets_index.json` and the current working directory. An explicit asset path is recommended for distributed builds.

## Getting started

1. Start a new village. The default map is **32 × 32**; map size, terrain seed and tutorial can be selected when creating it.
2. Connect building entrances to the road network leading to the central square. Rotate previews so entrances face a road.
3. Inspect the starting warehouse, homes and workplaces to check workers and supplies.
4. Maintain food, water and housing, and keep deliverymen available to move production inputs.
5. Expand production, trade surplus, and prepare defenders before hostile encounters begin.

The guided tutorial introduces inspection, roads, forestry, water, housing and defense. Skipping it is remembered across new games. Use **Options → Reset tutorial** to enable it again.

## Controls

| Input | Action |
|---|---|
| Left click | Inspect, or place the selected prefab |
| Right click | Inspect and cancel construction |
| Tab | Cancel building or toggle the building inventory |
| R | Rotate the selected prefab by 90° |
| WASD | Pan the camera |
| Q / E | Rotate the camera |
| Shift + Q / E | Rotate the camera faster |
| Mouse wheel | Zoom |
| Top-row 1 / 2 / 3 / 4 | Select 1× / 2× / 4× / 16× simulation speed |
| Space | Pause or resume |
| Escape | Open or close the game menu |

Number-pad keys are not used for simulation speed. The fourth speed is four times faster than the third.

## Economy and logistics

- **Emeralds** are the city currency. Trade and daily settlement provide income; construction, training, upkeep and upgrades spend it.
- **Goods** represent contributions from labor and combat. They convert to emeralds at **06:00**, with upkeep accounted for during settlement.
- **Physical resources** remain in building storage until transported. Deliverymen bring inputs to workplaces and move production into warehouses.
- **Small and large warehouses** store resources and provide delivery jobs. Emeralds and Goods remain in the treasury.
- **Trade** uses the market's local stock. A sale request arranges deliveries; complete the sale when enough stock has arrived.

Inspect buildings to see their inventory and missing inputs. A connected, staffed workplace still needs its supplies before it can produce.

Cave expeditions produce valuable materials for equipment upgrades. A staffed forge unlocks armor and tool improvements. Leather supports armor production, and wizards can enchant armor. Cow sheds produce leather and milk; milk helps injured villagers recover and removes negative effects.

## Villagers and professions

Villagers have persistent names, professions, health, homes, workplaces and personal inventories. Inspect them directly or use the People panel to review their status and train professions.

Specialists include farmers, woodcutters, miners, explorers, smiths, animal keepers, traders, deliverymen and defenders. **Citizens** are general helpers who can assist workplaces at half specialist efficiency.

Workers travel to connected workplaces. Ordinary villagers return home at night, while defenders patrol and engage threats. Food, water, services and taxes affect village happiness.

## World, daylight and defense

Maps contain water, stepped terrain, grass and natural trees. Adjacent terrain heights differ by at most one block. Woodcutters harvest tree buildings, which must regrow before another harvest.

The clock uses a 24-hour day with continuously advancing minutes: **00:00 is midnight**, **06:00 sunrise**, **12:00 noon**, and **18:00 sunset**. By default, a full day lasts ten real minutes at 1× speed.

New villages receive five peaceful days before regular nighttime monsters begin. Boss waves default to every **30 days**. These rules can be adjusted in Options; existing saves retain their configured values.

Zombies and skeletons burn in exposed daylight unless protected by a helmet or shelter. Regular spawns can wear leather helmets; susceptible boss-wave enemies always do. Walls, gates, combat villagers and golems help defend the village. Torches provide light and villagers manage them around dusk and dawn; torches can be destroyed.

When all villagers are lost, the game pauses on a statistics screen. Reload a living save to retry, start a new village, or delete the selected save using the red confirmation flow. Defeat does not automatically overwrite your previous save.

## Interface and options

The interface uses pixel icons, a Minecraft-inspired hotbar and the locally supplied Mojangles font. Hover over icons for labels and help. The building inventory groups prefabs by category and supports search.

Inspection is the default mouse tool. Selecting another object closes unpinned panels; pinned panels remain open. Management and inspector panels can be moved and resized.

Options include language, UI scale, quality, shadows, water, frame limit, resolution and display mode. Windowed, fullscreen and borderless fullscreen modes are available. **Brazilian Portuguese is the default language**, with English included and additional languages supported through TOML files.

## Saving and loading

Saves and preferences live beside the executable:

```text
<executable directory>/UserData/
├── options.toml
└── <save name>.vcs
```

When using `dotnet run`, this is normally `VillageCraft/bin/Release/net10.0/UserData/`. The executable directory must be writable.

Use **Escape → Save Game**, **Save Game As**, or **Load Game**. Save As lets you choose a different name and confirms replacing an existing save.

Saves use versioned binary records with a `VCTY` header and SHA-256 integrity checks. They do not use `BinaryFormatter`. Supported older saves are migrated on load; keep backups before updating. Game definitions and preferences use TOML, while the external Minecraft asset index retains its original JSON format.

## Content and modding

Definitions are loaded and validated once at startup. Restart after editing. Keep IDs stable because models, scripts and saves refer to them.

| Location under `VillageCraft/Content/` | Purpose |
|---|---|
| `Prefabs/` | Buildings and roads: costs, jobs, inputs, outputs and entrance rules |
| `Models/` | Model parts, geometry, materials and texture UVs |
| `Professions/` | Worker appearance, eligible jobs, training and efficiency |
| `Units/` | Combat unit models and statistics |
| `Locales/` | Interface and content translations |
| `Scripts/` | JavaScript behavior and mod components |
| `Templates/` | Authoring examples; `.example` files are not active content |
| `game.toml` | New-game defaults, initial layout and simulation rules |
| `resources.toml`, `economy.toml` | Resources, prices and economic settings |
| `upgrades.toml` | Equipment recipes and effects |
| `terrain.toml`, `tutorial.toml` | Terrain and guided tutorial settings |
| `sunlight.toml`, `rendering.toml` | Sunlight damage and rendering configuration |
| `names.toml`, `ui_icons.toml` | Villager name pools and pixel interface icons |

JavaScript runs through **Jint**, with direct bindings to game objects. Scripts support lifecycle callbacks, world events, queries, queued spawning/deletion and per-instance tags. AI and building components can override behavior for specific types.

See the [content authoring guide](VillageCraft/Content/README.md), [JavaScript API](VillageCraft/Content/SCRIPTING.md), and [profession template](VillageCraft/Content/Templates/profession.toml.example).

`tools/New-Catalog.ps1` is an optional authoring utility. It regenerates definitions and **overwrites manual changes** to those files; the game does not run it automatically.

## Project structure

```text
VillageCraft/
├── VillageCraft.csproj
├── Program.cs
├── City*.cs                 # Simulation and world systems
├── GameUi*.cs               # Interface and menus
├── Scene.cs                 # World rendering
├── Assets.cs                # External asset discovery
├── BinarySave.cs            # Binary save format
├── Script*.cs               # JavaScript runtime and bindings
└── Content/                 # TOML definitions, scripts and guides
run.ps1                      # Development launcher
README.md
```

External asset directories and generated build outputs are separate from the source content.

## Publishing

For example, publish a self-contained Windows x64 build:

```sh
dotnet publish VillageCraft -c Release -r win-x64 --self-contained true -o dist/win-x64
```

Use an appropriate runtime identifier for other platforms and validate the result there. Distribute the **entire publish directory**, including native libraries and `Content/`.

**Minecraft assets are not bundled by publishing.** Each user must configure `VILLAGECRAFT_ASSETS` to their separately supplied asset directory. Review font and other third-party asset permissions before distributing a build.

## Credits and asset ownership

VillageCraft is an independent fan-made project inspired by Minecraft and city management games. Minecraft textures used during development come from **version 1.16** and remain the property of their respective owners. They are not included in this repository.

The game's C# systems, prefab geometry, configuration and scripting are maintained separately from those assets. Third-party libraries retain their own licenses. No permission to redistribute Minecraft textures, sounds or fonts is implied by this repository.


