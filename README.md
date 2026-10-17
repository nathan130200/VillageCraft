# VillageCraft 1.0.5-beta

This release adds Goods (labor/combat rewards converted to emeralds daily at 06:00), physical production storage and deliverymen, small/large warehouses, villager-operated torches, leather and milk, cow sheds, milk recovery, armor enchanting and persistent tutorial dismissal. Use Options > Reset tutorial to enable it again. Content and balance settings are documented in `VillageCraft/Content/README.md`.

New villages include a stocked small warehouse and two deliverymen. Connect new workplaces to roads and train additional deliverymen in People as the city grows. Production requires delivered local inputs; inspect a building to see its storage. Sell requests in Economy arrange delivery to the trading hall; complete the exchange once its stock reaches 20 units. Emeralds and Goods stay in the treasury.

# VillageCraft â€” The Living Overworld

A C# 3D village builder using Raylib and ImGui.NET. Place complete textured prefabs, manage specialists and citizens, breed animals, explore caves, upgrade equipment, and survive raids. The supplied Minecraft textures are mapped onto original voxel geometry.

## Run

Install the .NET 10 SDK and use a graphical desktop with OpenGL 3.3 support:

```sh
dotnet run --project VillageCraft -c Release
```

Windows graphics and simulation are tested locally. Linux x64 and macOS desktop remain targets through the included native dependencies but require validation on those systems. Linux may need distribution-specific OpenGL/GLFW/X11 libraries. Ship the entire publish directory, including native libraries, `Content`, and `assets`.

## Maps and controls

New villages default to **32x32 tiles**, starting in bright morning daylight. New Game allows dimensions from **32 through 512**, a terrain seed, and an optional guided tutorial. Maps include lakes, stepped mountains, grass and natural oak trees, with a level starter area. Neighboring elevations differ by at most one Minecraft block; villagers follow dry, traversable routes. Normal nighttime monsters begin on day six; Options allows changing this grace period. The skippable tutorial covers inspection, roads, forestry, water, housing and defense.

Woodcutters harvest individual tree buildings once, then wait for regrowth (three days by default). Trees can also be planted. Configure terrain in `Content/terrain.toml` and tutorial stages in `Content/tutorial.toml`.

| Input | Action |
|---|---|
| Left click | Build selected prefab, or inspect in Inspect mode |
| Right click | Inspect building |
| R | Rotate prefab 90 degrees |
| Tab | Toggle Build / Inspect |
| WASD | Pan camera |
| Q / E | Orbit camera (hold Shift for 3x speed) |
| Wheel | Zoom; maximum range scales with map size |
| Top-row 1 / 2 / 3 | Resume at 1x / 2x / 4x / 16x (not numpad) |
| Space | Pause / resume |
| Escape | Open/close paused game menu |

The bottom dock contains category tabs, search, prefab thumbnails, icon costs, and rotation controls. The top bar shows resource icons with names on hover. People, Economy, Defense, and Journal open management panels. Inspect buildings to change priorities, disable work, repair damage, or demolish. Occupied homes and the central square cannot be demolished manually.

Escape offers New Game, Load Game, Save Game, Save Game As, Options, Exit and Resume. Creating a new map replaces the current unsaved session, while saved villages remain available. Save As confirms replacement of an existing binary save. Closing the menu restores the prior pause state.

## Villagers and growth

Villagers face their actual walking direction, including corners and combat movement. Their final orientation is retained when they stop. Workers at a building face its entrance, and animal/guardian movement also updates facing.

**Citizen** is the generic profession. Citizens without a job help families attract residents: each idle citizen adds the support amount from `game.toml` to daily immigration progress. They contribute Goods and consume food like other residents. Citizens may assist **any workplace at 50% specialist efficiency**, retaining their citizen identity and plain appearance. Production cycles take twice as long, combat damage is halved, and husbandry/guardian service progress is also reduced. Tool upgrades affect both citizens and specialists, preserving their relative efficiency.

Specialists fill matching jobs before citizens fill vacancies, with one worker per workplace before extra slots. Farmers, woodcutters, miners, explorers, smiths, animal keepers, traders, warriors, archers and magic villagers have separate TOML definitions and appearance. Use a villager's profession dropdown in **People** to train them; costs are shown on hover and read from profession data. Specialists do not automatically work unrelated jobs. Train them as citizens if you need generic helpers.

Workers must travel along connected paths and reach the workplace before producing. A building entrance must face an adjacent path connected by edges to the central square. Its preview highlights that tile. At rotation 0 the front is +Z; 90Â° is -X, 180Â° is -Z, and 270Â° is +X. Removing a road can disconnect buildings. Long commuters finish a work cycle before heading home.

Food, water, parks and taxes affect happiness. Fed, watered, happy villages gain immigration progress when housing and population limits permit. Default days last ten real minutes, with ordinary villagers sleeping at night; production cycles default to two seconds. These values and raid/animal/population limits are adjustable under Options and saved per village.

## Caves and equipment

Place **Production â†’ Cave entrance** beside a connected path. Miners and explorers work there; citizens can help at half efficiency. Expeditions consume food and tools, producing iron, gold, diamonds and ancient debris. Miners have bonuses for iron/gold, while explorers get more diamonds/debris. Resource yields, inputs and profession bonuses are defined in TOML. Supplies are renewable production, not finite terrain deposits or a playable underground world.

Build and staff a **Blacksmith forge**, then use **Economy â†’ Equipment** to buy upgrades in order:

- Iron, diamond and netherite armor reduce incoming damage to villagers and guardians.
- Tempered, diamond and netherite tools increase workplace efficiency.

Recipes, levels and effects are in `Content/upgrades.toml`. Upgrades apply village-wide and are stored in the save. The ancient-debris resource uses the supplied pack's obsidian icon as a fallback representation; loose assets and resource definitions can replace it.

## Roads, animals and raids

Six road types share the same path network. Click an ordinary path with another road selected to resurface it; upgrades charge positive differences in resource costs, preserve identity, and retain entrance connectivity. Gates cannot be resurfaced as ordinary roads. Routing chooses the shortest route rather than optimizing travel time.

| Surface | Base speed | Profession multiplier (in addition to base) |
|---|---|---|
| Dirt | 1.00x | None |
| Cobblestone | 1.15x | Stonecutters 1.35x, smiths 1.25x, farmers 0.80x |
| Gravel | 1.05x | Woodcutters 1.40x, warriors 0.75x |
| Boardwalk | 1.15x | Animal keepers 1.30x, stonecutters 0.70x |
| Packed ice | 0.80x | Snow golems 2.80x, archers 1.40x |
| Soul sand | 0.65x | Magic villagers 2.80x, iron golems 0.60x |

Keepers breed sheep, pigs and chickens, using food and respecting pen limits. Babies mature over time; adults supply wool, food or eggs. Guardians require a staffed workshop, materials and accumulated service work. Citizen helpers accumulate that work more slowly.

By default, zombies, piglins and blazes attack every 30 days from a frontier near the starter village. Waves grow and respect the concurrent hostile cap. Warriors intercept, ranged professions shoot, and golems patrol. Walls and gates block routes and line of sight. Raiders can kill residents and destroy buildings. Repair with wood and stone. Blazes currently use ground routing; flying siege AI is not implemented.

## TOML content

All game-owned definition/configuration files are **TOML**. The game reads and validates content **once at startup**, then retains indexed definitions in memory. Restart after edits. Syntax/reference errors identify the offending file or definition. The old monolithic `prefabs.json` is no longer read.

| Path under `VillageCraft/Content` | Contents |
|---|---|
| `game.toml` | Default map dimensions, category order, ground materials, initial stock, starter layout/professions, immigration and initial rules |
| `Prefabs/<id>.toml` | One complete building/road definition per file, including costs, jobs, recipes, homes, upkeep, health, entrance rules, profession access, model, and road effects |
| `Models/<id>.toml` | One model per file, with `[[Parts]]` box geometry, colors, textures and skin UV coordinates |
| `Professions/<id>.toml` | Appearance, eligible jobs, efficiency, training costs and resource bonuses |
| `Units/<id>.toml` | Combat model, health, speed, range, damage and cooldown |
| `resources.toml` | `[[Resources]]` IDs, icons and trade prices |
| `upgrades.toml` | `[[Upgrades]]` levels, recipes and armor/tool effects |
| `rendering.toml` | Alpha-cutout shader and discard threshold for foliage and entity skins |

The shipped catalog has **27 prefabs** and **11 villager professions**. Every prefab property is explicitly included in its file, even when empty or zero. Add another complete TOML prefab file with a unique `Id`, existing model and valid resource references to add a building without C# changes. New simulation mechanics still require code. IDs referenced by existing saves should remain stable.

Example recipe syntax:

```toml
Cost = { Emeralds = 180, Wood = 16, Stone = 32, Tools = 4 }
Input = { Food = 0.25, Tools = 0.1 }
Output = { Iron = 1.0, Gold = 0.4, Diamonds = 0.15, AncientDebris = 0.04 }
Professions = ["miner", "explorer"]
```

Model positions/sizes use tile units (four world metres per tile), +Y is up and +Z is the front. `Texture` names a logical asset such as `block/oak_planks.png`. `UvOrigin` and `UvSize` use pixels for skinned cuboids; empty arrays mean ordinary repeated block UVs. Geometry and entrances rotate together. Footprints remain one tile.

`tools/New-Catalog.ps1` is an optional authoring utility that regenerates the shipped TOML files. Running it **overwrites manual edits** to those definitions. The game never invokes it.

## Assets and graphics

The third-party **Minecraft `assets_index.json` remains in its original JSON format** as an input asset index, together with hashed `objects`; it is not a game configuration file. The resolver uses its local programmer-art archive for block/entity textures and item icons. Assets are external and are never copied by build/publish.

Loose PNGs in project/executable `assets` override indexed textures. Prefer logical paths such as `assets/minecraft/textures/block/oak_planks.png` or `assets/minecraft/textures/entity/villager/farmer.png`; simple filenames are also recognized. Set VILLAGECRAFT_ASSETS to the directory containing assets_index.json, objects, and/or assets. The configured directory takes precedence; development falls back to the project root. Restart to reload. Missing textures use the model part's fallback color.

Block/entity textures use mipmaps at every quality level, nearest-pixel magnification up close, and blended mip levels at distance. Medium adds 4x anisotropy and High 8x. MSAA covers geometry edges. Options exposes texture/shadow/water toggles, quality and 30/60/120/unlimited FPS. Generated lakes have animated water when enabled. Graphics preferences are written to executable-relative **`UserData/options.toml`**.

Models use two-sided alpha cutouts: empty skin/foliage pixels are discarded before writing depth, so objects behind them remain visible regardless of draw order. Placement tint alpha remains independent for ghost previews. Chicken parts use separate anatomical atlas regions, including the skin's cutout legs, and lumber-camp foliage uses the oak-leaf texture.

## Binary saves and compatibility

New saves use **`<executable directory>/UserData/<name>.vcs`**. When running with `dotnet run`, this is in the build output, e.g. `VillageCraft/bin/Release/net10.0/UserData`. The folder is created on save.

The format contains a `VCTY` header, version, payload length, SHA-256 checksum and explicit little-endian binary records. It is genuine binary serialization, not compressed or disguised JSON, and does not use `BinaryFormatter`. Length/count limits, integrity checks and semantic validation reject damaged or unsupported saves. Map size, professions, facing, equipment, growth, service work, rules, resources and living entities persist. Cached routes are rebuilt after loading.

Load Game also discovers existing v2 `.json` saves. Loading imports them as 32Ã—32 flat villages without changing their files; the next Save writes a `.vcs` beside the original. Original v1 saves are unsupported. Imported villagers without a profession become citizens. Changing content IDs referenced by a save can prevent loading; keep a backup before editing those IDs.

## Verification and packaging

```sh
dotnet build VillageCraft -c Release
dotnet run --project VillageCraft -c Release -- --self-test
dotnet run --project VillageCraft -c Release -- --ui-smoke
dotnet run --project VillageCraft -c Release -- --cave-smoke
dotnet run --project VillageCraft -c Release -- --newmap-smoke
dotnet run --project VillageCraft -c Release -- --models-smoke
dotnet run --project VillageCraft -c Release -- --demo
dotnet publish VillageCraft -c Release -r win-x64 --self-contained true -o dist/win-x64
```

Checks cover TOML parsing and cached lookup, custom prefabs, map sizes/large-map A*, walking direction, exact citizen half-output, growth, training, cave yields, equipment effects, service efficiency, roads, animals, combat, binary integrity, named saves and read-only legacy import. Graphics smoke tests use the real renderer; `--ui-smoke` repeatedly changes cards at near/far zoom to guard against the previous ImGui style-stack assertion.

The map surface uses constant-count draws instead of one draw per tile; building/path lookups are cached, routes use A*, and creatures cache routes. This is still a city-builder foundation without item-carrying logistics, traffic simulation, multiplayer, generated terrain, skeletal animations or multi-tile prefabs.


## Daylight and JavaScript

Daylight changes terrain, model lighting and the sky. Three common monsters arrive per night by default: zombies, skeletons and creepers. Night monsters remain after dawn; exposed undead burn unless protected by a helmet or shade. Every 30th day (configurable), a Nether warlord leads common monsters, piglins, brutes, zombified piglins and blazes. The hostile cap applies to both encounters. Escape > Options controls nighttime encounters, count, day length and raid interval.

Jint 4.17 binds JavaScript directly to live game APIs, without JSON conversion. Behaviors call ctx.move(), ctx.attack() and ctx.explode(); building scripts can repair structures, inspect villagers, transfer inventory and manipulate resources through ctx.game. See [the scripting API](VillageCraft/Content/SCRIPTING.md).

Binary format v9 stores enemy helmets, defeat state and lifetime statistics alongside physical storage, cargo, effects and 06:00 settlement state as well as terrain, seed, tree regrowth and tutorial progress alongside entity IDs, tags, behavior attachments, villager identities and inventories alongside night and AI state. Older v3/v4/v5/v6/v7/v8 binary and v2 legacy JSON saves remain readable.

PowerShell: set $env:VILLAGECRAFT_ASSETS to the directory containing assets_index.json and objects, then run dotnet run --project VillageCraft -c Release. Or run ./run.ps1, which defaults the process asset path to this repository. Distributed builds need the variable pointing to a separate asset directory.

The default day/night cycle lasts 10 real minutes at 1x (Options: 1–60 minutes). Old saves using the former 12-second default migrate to 10 minutes while preserving clock phase. Hold either Shift with Q/E for 3x camera rotation speed. Monster movement and villager pursuit now cover the village instead of requiring immediate melee proximity.


## Português, conteúdo e aldeões

Português brasileiro é o idioma padrão; Opções > Idioma permite mudar para inglês ou um idioma adicionado em Content/Locales. Nomes de conteúdo são traduzidos sem alterar IDs. Aldeões recebem nome e sobrenome persistidos e um inventário próprio com comida e ferramentas retiradas do estoque. Clique direito em um aldeão ou selecione-o em Moradores para inspecionar vida, estatísticas, profissão, moradia, trabalho e inventário.

[Guia de tipos TOML e criação de profissões](VillageCraft/Content/README.md) · [API JavaScript direta](VillageCraft/Content/SCRIPTING.md) · [Modelo de nova profissão](VillageCraft/Content/Templates/profession.toml.example)


## Event-based script components

Each world owns a standalone ScriptRuntime with an engine per script. Scripts register once with set_script_type(eST_AI, "blaze"), set_script_type(eST_Building, "well"), or eST_Mod (also the default). All types support onInit, onUpdate(dt), and onShutdown; mods receive world events, while AI/building components receive callbacks for their instances.

The global world API supports queued spawn/delete/damage, live queries by JavaScript predicate, ID, type or tag, and persisted per-instance tags and behavior attachment. See Content/SCRIPTING.md and the non-active *.js.example templates under Content/Templates. Default queued building placement follows normal cost/path rules.


## Compact Minecraft UI

The game uses the provided mojangles.ttf at 16px, pixel icons, square beveled controls, a compact hotbar and an icon HUD. The clock uses all 64 Minecraft clock textures and follows simulation time (including pause and speed changes). Labels and help are available on hover.

The default mouse tool is inspection. Left-click an object, villager, monster, guardian or animal to inspect; empty terrain clears unpinned panels. Right-click also inspects and cancels construction. Category buttons open the prefab inventory; selecting a prefab activates construction. The magnifying-glass button returns to the empty tool. Tab cancels an active build or toggles the inventory while inspecting.

Inspector and management windows can be moved/resized and pinned with the pushpin icon. Pinned windows survive later selections; close their window or unpin them to dismiss them. Pin state is session UI state and resets when changing worlds.

Custom control/status icons are editable 16x16 pixel grids in VillageCraft/Content/ui_icons.toml. A dot is transparent; the palette maps characters to colors. UI tooltips wrap at viewport-safe widths and use unformatted text so percent signs and accented text display correctly. The interface supports windows down to 1024x640.

The HUD clock displays HH:MM: all 1,440 in-game minutes advance continuously over the configured day length. Daylight also follows continuous day progress, rather than discrete hourly ticks. At the default 10-minute day, each in-game minute lasts about 0.42 real seconds at 1x. Options shows this conversion; pause freezes the simulation clock and its clock sprite.

Options > Display provides resolution, Windowed, Fullscreen, and Borderless fullscreen. Apply display settings applies and saves the selection in UserData/options.toml. Borderless uses desktop resolution. Windowed startup and mode changes fit and center the client area with room for desktop panels and window decorations, keeping the title bar reachable.

Older saves retain their original flat terrain and do not restart the new-game peaceful-night period.

UI scale is available in Escape > Options (1.0x default, up to 2.0x) and saved automatically in UserData/options.toml. Speed keys 1/2/3/4 select 1x/2x/4x/16x; the fourth speed is four times the third. Defender villagers patrol outside at night and engage enemies on sight.

In existing saves, supplies migrate into a warehouse. Train deliverymen in People to keep workplaces supplied after upgrading to 1.0.5-beta.

Warehouses use distinct loading-depot models with pallets and stacked crates. The digital 24-hour clock, Minecraft clock icon, lighting, monster spawns and defender patrols agree: midnight is 00:00, sunrise 06:00, noon 12:00 and sunset 18:00. Ordinary workers finish at 17:00; torches are lit from dusk until dawn.

New games default to boss waves every 30 days. Existing saves retain their configured interval; change it in Options if desired. Torches are omitted from the resource HUD.

Zombies and skeletons burn in exposed daylight. Regular spawns have a 65% leather-helmet chance; boss-wave versions always have helmets. Helmets prevent sunlight burning, and roof/canopy shade provides shelter. Night monsters stay after dawn. Configure unit susceptibility in Units TOML and rates in sunlight.toml.

Losing every villager opens a frozen game-over statistics screen. Reload a living save to retry, start a new village, or use the red Delete save button (with confirmation). The last saved snapshot is not overwritten by defeat. Binary v9 preserves helmets, boss flags, defeat state and statistics, with v3-v8 compatibility.
