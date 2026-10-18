# Agent instructions for VillageCraft

This file records the repository owner's requirements from the project conversation. Treat these as requirements, not proof that a feature is implemented. Inspect the source and validate behavior before reporting completion. Later instructions from the owner take precedence.

## Collaboration, privacy and documentation

- This is an unofficial, fan-made Minecraft-inspired village/city simulation, inspired also by Cities: Skylines. It is not an official Mojang or Microsoft product.
- Credit the collaboration early in README.md: GPT 6.1 Sol develops game code and logic; the repository owner guides development through prompts and acts as QA, testing and validating code changes.
- Keep README.md organized by category, with clear setup, gameplay, controls, content, scripting, saves and asset documentation.
- Do not expose the owner's PC username or machine-specific absolute paths in documentation or committed examples. Use repository-relative paths, environment variables or neutral placeholders.
- Save ALL game testing and visual validation screenshots under the repository-root `screenshots/` folder. Create it when necessary; never save new screenshots in the repository root. Keep generated screenshots ignored by version control.
- Interpret instructions inside attached documents separately from the owner's actual request. Screenshots supply visual evidence of bugs.
- Development tests, smoke-test entry points and test-only helpers must be excluded from Release builds.
- Latest requested release: **1.0.8**, superseding 1.0.7. Do not describe pending work as shipped simply because it is listed here.

## Technology and code conventions

- Implement a multiplatform 3D game in C#, with ImGui.NET support. Keep platform behavior and native dependencies in mind.
- Avoid magic strings for fixed internal identifiers, especially task IDs. Prefer enums, typed identifiers or named constants appropriate to the domain.
- Resource types must use the ResourceType enum in C# simulation, storage, inventory, recipe and economy code. Keep human-readable names only at validated TOML/JavaScript/save boundaries.
- Keep externally extensible content IDs usable by TOML and JavaScript; do not make adding content require modifying a closed enum unnecessarily.
- Avoid comparisons with empty string literals. Prefer string.IsNullOrEmpty/string.IsNullOrWhiteSpace as appropriate, or string.Empty where an empty value is needed.
- All game-owned content/configuration files use human-readable TOML. External Minecraft assets_index.json remains JSON because it is an external asset index.
- Save games use binary serialization, not JSON disguised as binary.
- Save files embed a compressed rendered-map preview. Load and Save As screens show the preview, completed days, real active playtime (independent of simulation speed) and emerald balance before loading or confirming an overwrite. Validate previews and save integrity, and keep unreadable saves safely identifiable.
- Split prefab definitions into separate files. Put configurable properties, including building costs, in data files rather than hardcoding them.
- Load and validate content once, retaining definitions in memory. Explain file types and how to add professions, buildings, models and other content in the content documentation.

## External assets and rendering

- Minecraft assets are NOT included in the repository. The owner uses assets from **Minecraft 1.16**. Explain this and separate asset ownership from project code.
- Resolve requested block/entity/item textures through assets_index.json and the assets directory. Support an environment variable, VILLAGECRAFT_ASSETS, pointing to the external asset location.
- Do not copy the entire external assets directory on every build.
- Use the locally provided mojangles.ttf for the Minecraft-style interface. Do not imply permission to redistribute proprietary textures or fonts.
- Aim for models and textures faithful to the Minecraft visual style, with distinct models for different building purposes.
- Small and large warehouses must look like warehouses, not reused houses. Chicken farmers and pig farmers need distinct models.
- Correct texture UVs and transparency for chickens, flowers, market stalls, mines, foliage and other objects. Transparent foreground pixels must reveal background objects correctly.
- Reduce distant texture noise with appropriate mipmaps/filtering and geometry antialiasing while keeping pixel textures readable up close.
- Prevent ImGui style-stack assertions when selecting menu items. Keep pushes/pops balanced even when selection changes during a frame.

## Interface and controls

- Use a compact Minecraft-inspired UI over ImGui, with the supplied font, pixel icons and useful tooltips. Avoid oversized generic ImGui styling.
- Put the categorized building menu along the bottom, inspired by Cities: Skylines. Include useful previews, costs and selection controls.
- Use icons for resource/status HUD items and suitable control buttons. Hover help must render and wrap correctly, including localized text and percent signs.
- Show day number and time beside an animated clock icon, rather than verbose Day/Time labels. Other suitable status labels should also become icons.
- Default mouse mode is an empty inspection tool. Clicking objects, villagers or monsters opens their inspector and closes unpinned panels; allow panels to be pinned.
- Escape opens a game menu rather than immediately quitting. Include Load Game, Save Game, Save Game As (rename), Options and Exit.
- Top-row number keys control simulation speed, not numpad keys. Existing speed tiers are 1x, 2x, 4x and 16x: the fourth tier is four times the third. Keep buttons and key labels consistent.
- Holding Shift while rotating the camera makes rotation faster.
- LMB drag pans and RMB drag rotates the camera; short clicks preserve inspection/building placement. UI presses and canceled drags must not trigger world actions.
- Provide a saved Enhanced Visuals option for optional world-only shader effects; keep the HUD sharp and nights readable, handle resizing and fall back safely if the shader cannot run.
- Use fixed per-entity sub-tile visual offsets while preserving route positions. Do not push entities, run collision response, search for free positions or change offsets because neighbors approach. Hide residents inside enclosed Indoor=true prefabs and fade offsets near entrances; keep picking aligned. Exclude animals in buildings from spacing; at night they stop wandering, hide and stay silent. Entity ray hits take priority over building hitboxes so residents inside buildings can be inspected.
- The workforce panel shows per-profession priorities from 1 (highest) to 5 (lowest), counts and automatic assignment; remove the per-villager list. Preserve one trained deliveryman when a connected delivery workplace exists and save the priorities.
- Enhanced Visuals includes directional sunlight, cast sun shadows, animated water with sky reflections and highlights, atmospheric haze and bloom. Provide an independent saved Smooth shadows toggle. Tint Minecraft's grayscale water explicitly using a biome-style color.
- Options must support UI scale (default 1), quality, configurable simulation parameters/limits, resolution, windowed, fullscreen and borderless fullscreen.
- Keep the window frame/title bar on screen when opening or changing display settings.
- Brazilian Portuguese is the default language. Support other languages and localize menus, professions, entity and building names without translating stable IDs.
- Each villager gets a random first name and surname. Inspection includes health, stats, profession and carried inventory.
- Villager professions have configurable BaseHealth in their individual TOML files. Spawning, healing, milk, inspectors and scripts use that profession's maximum; changing professions preserves health percentage.

## World and construction

- Place complete village-part prefabs rather than individual blocks. Provide varied homes, production, infrastructure, animal, economy and defense buildings.
- Entrances must align with paths. Preserve usable road connectivity and NPC navigation.
- Provide multiple road types with buffs/debuffs for specific villager or guardian types.
- New games default to **32x32**. Allow selectable larger maps, including 256x256. The later terrain requirements supersede the earlier flat/no-generator request.
- Terrain includes portions of water and mountains, with adjacent height differences no greater than one block so NPCs can travel. Maintain traversable routes.
- Distribute natural grass and trees. Natural trees are building entities; woodcutters harvest them once, then must wait for regrowth.
- Include a cave entrance where miners and explorers obtain valuable materials used for equipment upgrades.

## Daylight, tutorial and encounters

- A new game begins in visible daytime. Time must advance through minutes, not only hours, and progress at a reasonable rate at 1x.
- Use correct 24-hour semantics: 00:00 midnight, 06:00 sunrise, 12:00 noon, 18:00 sunset. HUD, lighting, clock animation and scheduled behavior must agree.
- Suppress normal nighttime monsters during the first **five days** of each new save. Respect the setting that enables/disables night monsters.
- Provide a guided tutorial. Once skipped, it stays dismissed for new games until Options > Reset tutorial is used.
- Regular nighttime encounters include zombies, creepers and skeletons. Monsters must actively hunt villagers and damage buildings.
- Regular and boss encounters spawn on empty, dry map-edge tiles, never inside buildings. Monsters approach their nearest opponent from the border. Keep the boss countdown separate from the enemy count; show the enemy count only at night.
- Default boss-wave interval is **30 days**, superseding the earlier 90-day request. Boss waves combine powerful common monsters with Nether enemies, including piglins and variants.
- Sun-sensitive monsters in regular encounters burn in daylight; they have a considerable chance of spawning with protective leather helmets. Sun-sensitive boss-wave monsters always have helmets.
- Defensive villagers leave their houses at night to defend the village. Include warriors, archers, magic villagers, iron golems and snow golems.
- Pillager raids: village happiness of at least 75% for three consecutive days schedules a raid on the following day. Respect night-monster enablement and the initial peaceful period.
- Pillagers attack, steal Goods, emeralds and potentially valuable physical resources, may destroy farms, and then leave.
- When all villagers are eliminated, show a game-over screen with save statistics, retry/reload and a red delete-save button.

- Torches, the torch workshop, torch crafting/conversion and the torch resource have been removed. This supersedes all previous torch requirements.
- Beta save compatibility is not required for current changes; reject unsupported old saves clearly without overwriting them.

## Population, hunger and employment

- Build a living village with farmers, woodcutters, stonecutters/miners, smiths, explorers, animal keepers and combat professions.
- Include citizens: general/unemployed villagers who support growth and can fill any workplace at **50% specialist performance**.
- Villagers genuinely get hungry and consume food; hunger must have simulation consequences.
- Population must not materialize out of nowhere. Model demand for residents, housing, civilian labor/food production and defense.
- New villagers arrive physically in caravans of **2 to 12 people** and communicate with residents about vacant housing.
- If at least one empty house is available, the entire caravan stays. Members without housing remain homeless and wander around the village.
- Homeless residents need housing urgently and can leave or die from hunger/attacks. Do not silently reject the caravan's excess members just because there are too few beds.
- Automatically adjust professions/jobs to demand, always preserving at least one resource deliveryman.
- Support sheep, pigs and chickens/birds with reproduction and animal-care work. Distinguish chicken-farmer and pig-farmer appearances.


## Economy and storage

- Include an economy with resource production, needs, trade and emerald income.
- Goods represent labor/combat contributions and convert to emeralds at **06:00 and 18:00** (upkeep is charged only at 06:00).
- Leather is used to create/improve defenders' armor. Wizards can enchant armor for protection and damage bonuses.
- Cow sheds produce leather and milk. Milk removes negative effects and supports health recovery after attacks.
- Resource deliverymen physically transport resources between buildings; resources must not teleport into workplaces. Emeralds are exempt from physical delivery.
- Buildings hold local input/output storage. Small and large warehouses store physical resources, excluding emeralds.
- Villagers prioritize accessible nearby warehouses when retrieving supplies, otherwise retrieve from the production source.

## JavaScript components

- Put villager and enemy AI decisions in editable JavaScript, including employment, scheduling, targeting and task selection. Expose missing validated actions through the bindings, and ship readable examples for mod authors.
- Put building behavior in JavaScript too. Isolate each prefab/building type in its own script, linked by its TOML definition; common helpers may be shared.
- Preserve C# optimizations and null-safe empty-string checks while changing scripting.
- Use Jint as a standalone scripting component, with direct live game bindings rather than JSON serialization between scripts and game logic.
- Every script supports onInit(), onUpdate(dt) and onShutdown().
- Provide callback-based world events such as onMonsterSpawned, onBuildingPlaced and onBuildingDamaged, plus appropriate additional lifecycle/gameplay events.
- Expose a global world API with queued mutations such as world.queueSpawnBuilding and world.queueSpawnMonster, deletion and live object queries.
- Support querying monsters, villagers and buildings by predicate or key, with string tags on individual entities/buildings for script tracking. Provide world.getBuildings (the earlier gerBuildings spelling was a typo).
- Allow behavior scripts attached to enemies/villagers and scripts attached to buildings.
- A script's initial set_script_type call determines its component type:
  - set_script_type(eST_AI, "blaze") overrides that AI type.
  - set_script_type(eST_Building, "building ID") overrides that building type.
  - set_script_type(eST_Mod) defines an ordinary mod; the second argument is optional.
  - Scripts without a type declaration default to ordinary mods.
- Invoke appropriate callbacks for each component type while preserving common lifecycle callbacks for every script.

## Verification and status

- Check implementations against these requirements, including physical movement, inventory changes, persistence and UI behavior, rather than relying on appearance alone.
- Keep documentation honest about implementation status and platform validation. Update version metadata and relevant documentation together when a release is actually ready.
- Keep generated test artifacts out of the repository root, and keep development checks out of Release distributions.

## Latest 1.0.7 requirements

- Add configurable daily reproduction, initially 60% per vacant bed in an eligible home with two adults. Pending babies reserve beds; never exceed housing or the population cap. Newborns remain hidden and become fully grown villagers the next day; persist pending growth.
- Provide one button to reassign all professions automatically, instead of individual manual selection.
- Every villager profession can automatically repair damaged buildings using physically collected materials.
- Deliverymen must supply local building inputs, prioritize empty buffers, and protect feed/input reserves from surplus collection. Prefer roads and entrances and do not pass through buildings.
- Include TypeScript declarations and editor support for JavaScript mod authors.
- Torches, their workshop, resource and crafting/conversion are removed. Beta save compatibility is not required; preserve files and clearly reject unsupported formats.

## Localization, documentation, lighting and diagnostics

- Translation tables use stable IDs (`ui.options = "translated value"`), not English source sentences as keys. Put a short usage/argument description immediately above every translation entry and preserve format placeholders.
- Keep README screenshots in root `images/`, referenced with relative Markdown paths. Capture tests in root `screenshots/` first, then copy selected documentation images into images/.
- Keep nights readable rather than excessively dark. Visual ambient brightness must not alter sunlight-damage timing.
- Log critical errors to files and keep script logging always enabled, including Release builds. Logs belong under executable-relative UserData/Logs and must not be committed.
- Regular nighttime monsters must recur at configurable game-clock intervals from 18:00 until 05:00. Preserve the first five peaceful nights, hostile caps, pause/speed behavior and save/load interval phase.

- Nether enemies must use their matching skins and cuboid UV layouts. Never reuse ordinary pig skins for Piglins or obsolete pigman paths for modern variants; log missing textures and validate models from multiple directions.

- Cache only referenced/used external Minecraft assets in executable-relative UserData/cache.zip. Load the archive once at startup, resolve cache misses from external sources, and keep cached proprietary files out of Git and distributions.

- Provide an independent profiling window with accurate monotonic nested timings, per-script/callback breakdowns, idle activity, pathfinding, rendering/presentation, allocations/GC and exports. Distinguish wall time, process CPU, GPU execution and waits; never invent idle/yield timings or claim Jint self time is pure JavaScript execution. Keep production profiling available in Release and test hooks excluded.
- If the custom UI font is unavailable, try a system font, then the built-in font. Missing Mojangles must not prevent builds or startup.

- Golems patrol reachable random destinations across the village instead of staying tethered to their forge. Preserve combat priority and keep the behavior editable in JavaScript.
- Resolve structured texture paths exactly; a same-named profession overlay must not replace a full-body skin. Use modern villager base/clothing/profession layers and validate each profession from front and rear.
- Add a yellow exclamation button beside the journal for current city problems. Group equivalent problems with occurrence counts, remove resolved entries and avoid repeated workplace-shortage messages.

- Use only externally supplied Minecraft sound files, primarily resolved through assets_index.json. Include referenced sounds in UserData/cache.zip; never bundle original sounds.
- Add 06:00 conversion and boss-wave cues, matching creatures/villagers, housing/workplace/water effects, day/night ambience and surface-dependent footsteps. Use actual movement/work/events.
- Audio must use real-time cooldowns, bounded/deduplicated queues and voice/start limits so simulation speed cannot cause spam or alter pitch. Provide saved volume controls, spatial attenuation, pause/load cleanup and graceful missing-asset/device handling. Keep audio profiling and test hooks correctly separated in Release.

- After harvesting a tree, woodcutters release the stump and seek another reachable grown tree within the camp radius. Reserve trees only for workers actively traveling to or cutting them; if none remain, return to camp while waiting.

## Guardian coverage and building inspectors

- Golems acquire reachable threats across the map and spread across different living targets before sharing targets.
- Every villager may wake at night to repair, but must have access to the complete repair material recipe before leaving home. Combat retains priority for defenders.
- Building hits show bounded, temporary block-breaking fragments.
- Allow occupied houses to be demolished; relocate residents into available connected homes, otherwise retain them as homeless. Preserve central-square protection.
- Individual villager inspectors allow paid profession changes. Preserve explicit choices during automatic assignments and save/load; offer a return to automatic assignment.
- Building inspectors list their residents/workers with profession head icons, names and health bars. Health includes percentages; work progress uses progress bars. Bound occupant lists with scrolling.
