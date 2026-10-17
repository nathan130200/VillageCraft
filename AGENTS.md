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
- Latest requested release: **1.0.6**, superseding 1.0.5-beta. Do not describe pending work as shipped simply because it is listed here.

## Technology and code conventions

- Implement a multiplatform 3D game in C#, with ImGui.NET support. Keep platform behavior and native dependencies in mind.
- Avoid magic strings for fixed internal identifiers, especially task IDs. Prefer enums, typed identifiers or named constants appropriate to the domain.
- Keep externally extensible content IDs usable by TOML and JavaScript; do not make adding content require modifying a closed enum unnecessarily.
- Avoid comparisons with empty string literals. Prefer string.IsNullOrEmpty/string.IsNullOrWhiteSpace as appropriate, or string.Empty where an empty value is needed.
- All game-owned content/configuration files use human-readable TOML. External Minecraft assets_index.json remains JSON because it is an external asset index.
- Save games use binary serialization, not JSON disguised as binary.
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
- Options must support UI scale (default 1), quality, configurable simulation parameters/limits, resolution, windowed, fullscreen and borderless fullscreen.
- Keep the window frame/title bar on screen when opening or changing display settings.
- Brazilian Portuguese is the default language. Support other languages and localize menus, professions, entity and building names without translating stable IDs.
- Each villager gets a random first name and surname. Inspection includes health, stats, profession and carried inventory.

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
- Default boss-wave interval is **30 days**, superseding the earlier 90-day request. Boss waves combine powerful common monsters with Nether enemies, including piglins and variants.
- Sun-sensitive monsters in regular encounters burn in daylight; they have a considerable chance of spawning with protective leather helmets. Sun-sensitive boss-wave monsters always have helmets.
- Defensive villagers leave their houses at night to defend the village. Include warriors, archers, magic villagers, iron golems and snow golems.
- Pillager raids: village happiness of at least 75% for three consecutive days schedules a raid on the following day. Respect night-monster enablement and the initial peaceful period.
- Pillagers attack, steal Goods, emeralds and potentially valuable physical resources, may destroy farms, and then leave.
- When all villagers are eliminated, show a game-over screen with save statistics, retry/reload and a red delete-save button.

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

## Economy, storage and torches

- Include an economy with resource production, needs, trade and emerald income.
- Goods represent labor/combat contributions and convert to emeralds daily at **06:00**.
- Leather is used to create/improve defenders' armor. Wizards can enchant armor for protection and damage bonuses.
- Cow sheds produce leather and milk. Milk removes negative effects and supports health recovery after attacks.
- Resource deliverymen physically transport resources between buildings; resources must not teleport into workplaces. Emeralds are exempt from physical delivery.
- Buildings hold local input/output storage. Small and large warehouses store physical resources, excluding emeralds.
- Villagers prioritize accessible nearby warehouses when retrieving supplies, otherwise retrieve from the production source.
- Torches are placeable light objects, **not a resource to show in the top bar**. This supersedes the original request describing them as a resource.
- Torches require no fuel or lifetime expiration. Villagers light them before sleep and extinguish them at dawn; these tasks must not trap villagers in stalled loops.
- Torches ARE destructible, superseding the original indestructible-torch request.

## JavaScript components

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
