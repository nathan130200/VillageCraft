# Minecraft audio

`audio.toml` defines all cues and playback limits. Only externally supplied Minecraft 1.16 OGG assets are used. The audio loader reads `minecraft/sounds/...` entries from `assets_index.json`, resolving their hashed objects through the existing asset resolver. It also supports loose `sounds/` files. No generated sounds or external sound library is bundled.

Referenced sound files join textures in executable-relative `UserData/cache.zip`. Clips decode once at startup, and simultaneous voices share the decoded data through sound aliases. Delete the cache while the game is closed after replacing source assets. Missing sounds or an unavailable output device are logged; gameplay continues silently for unavailable cues.

## Sound mapping

| Situation | Minecraft sounds |
|---|---|
| Goods converted to emeralds at 06:00 and 18:00 | Experience level-up, after a positive conversion |
| Boss wave starts | Totem of Undying, once per wave event |
| Houses | Wooden door open/close on resident departure/arrival; nearby villager voices |
| Farms | Crop planting and grass/dirt digging during productive work |
| Quarries | Stone digging during mining |
| Forges | Anvil use and occasional lava pop during productive work |
| Wells | Nearby flowing water |
| Villagers, animals and monsters | Matching idle, hurt and death samples where vanilla provides them |
| Combat | Bow shots, fireballs, iron-golem throws, melee strikes, creeper fuse/explosion |
| Movement | Grass, stone, wood, gravel, sand, snow or water footsteps based on actual surface |
| Daytime | Quiet Minecraft parrot calls near natural trees |
| Nighttime | Occasional quiet cave ambience near a cave building, plus actual nearby monsters |

The day/night soundscape is an arrangement of real Minecraft samples, not a claim that Java 1.16 has a generic Overworld day/night ambience loop. Creepers have no idle voice. Flying blazes have no floor footsteps. Iron/snow golems can add their characteristic locomotion sound to material footsteps.

## Playback policy

All scheduling uses a monotonic **real-time clock**. Simulation speed does not change sample pitch, step cadence limits or cooldown durations. A faster world can generate more events but cannot make them all audible.

- Default maximum: **12 active voices**, **8 starts per sliding real second**, with **2 starts reserved for alerts**.
- Requests are bounded and deduplicated by cue/source within a frame, sorted by priority, then discarded after processing. No delayed sound backlog or catch-up burst is replayed.
- Each cue and category has a real-time cooldown. Priorities below 40 also share ordinary start spacing; impacts at 40–69 bypass that spacing, while critical cues at 70+ also bypass category spacing. Per-cue cooldowns and the global start limit always apply.
- High-priority requests can replace lower-priority active voices when the voice pool is full.
- Footsteps require actual displacement and have per-entity and crowd-wide real-time limits. Building floors come from `BuildingSurfaces`, road surfaces from `RoadSurfaces`; exposed high terrain is stone, other dry terrain grass, and water uses swimming samples.
- Local effects attenuate with distance from camera focus and pan relative to camera rotation. Settlement/raid alerts are non-spatial. Moving the camera updates active voices.
- Pausing, opening a menu, defeat or muting suppresses new world sounds and fades active voices down. Loading another village stops previous voices and clears pending events.

Options provides master, effects/footsteps, creature, ambience and alert sliders. Save audio preferences persists them in `UserData/options.toml`. The F3 profiler includes audio scheduling time and clip/voice/start/rejection counters. Native mixing runs on the audio thread; the main-thread audio scope measures scheduling/submission, not mixer CPU time.

## Cue fields

Each `[[Cues]]` entry defines a unique `Id`, typed `Trigger` and `Category`, optional exact `Entity`/`Job`/`Prefab` filters, optional `Surface`, canonical OGG `Files`, `Volume` (0–1), real-second `Cooldown`, `Spatial` flag and `Priority` (0–100). `Files` is a random variant pool, not a playlist. Adding a file here makes it a referenced asset for startup decoding/cache inclusion. Restart after content edits.

New world callbacks include `onBossWaveStarted(wave)`, `onGuardianDamaged`, attack callbacks, `onMonsterPrimed` and `onExplosion`. `onBuildingWork(building, worker)` is emitted once through the common world event stream, allowing both scripts and audio to observe actual production.

## Validation

Debug-only `--audio-test` checks OGG resolution, rate limits under 1x/16x event storms, alert reservations and surface selection. `--audio-smoke` runs a quiet native-device playback check for settlement, boss alerts and footsteps; its screenshot goes to root `screenshots/`. These test entry points/classes are excluded from Release.
