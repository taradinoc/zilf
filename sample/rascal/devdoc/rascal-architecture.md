# Rascal architecture and subsystems

This document describes Rascal's core subsystems (the modules inserted by
rascal.zil) in terms of what they do, how they do it, and why they are
structured the way they are. Each section points at the most important
functions, globals, and data structures for quick navigation.

## Entry and top level flow

Rascal starts in `GO` in [rascal.zil](../rascal.zil). The entry routine exists
early in memory for Z-machine constraints, and it sets up the user experience
before the game loop begins. `CHECK-SCREEN` validates screen capabilities,
`SPLASH` handles the title screen, and `INIT` initializes state and per-run
randomization. Control then passes to `RASCAL-MAIN-LOOP`, the central loop
driving input, simulation, and rendering.

## Data model at a glance

Rascal's item and enemy data is object-centric. In
[objects.zil](../objects.zil), the `RASCAL-ITEM` and `RASCAL-ENEMY` templates
define per-object data using compact single-byte properties like `R-ITKIND`,
`R-ITID`, `R-ITLVL`, `R-ITENCH`, `R-ITAMT`, `R-X`, `R-Y`, `R-ETYPE`, and
`R-EHP`. This approach is used because Z-machine object properties are flexible
and make it easy to reuse the same objects in both dungeon and parser interiors.
The tradeoff is memory, so objects are pooled and reused via `ALLOC-RASCAL-ITEM`
/ `FREE-RASCAL-ITEM` and `ALLOC-RASCAL-ENEMY` / `FREE-RASCAL-ENEMY`, with
`CLEAR-RASCAL-ITEM` and `CLEAR-RASCAL-ENEMY` ensuring a clean slate when reusing
pooled objects.

The map grid is table-based. [dungeon.zil](../dungeon.zil) stores tiles in the
`MAP` byte table and room IDs in `ROOMIDS`, while room bounds and centers live
in `ROOMS-TABLE` with helpers such as `ROOM-GET`, `ROOM-SET`, and `MAP-INDEX`.
This keeps map data in tables for dungeon generation and fog-of-war reveal.

## Map generation overview

Map generation is a deterministic pipeline that starts with per-run and
per-floor seeding, then builds a layout, and finally populates it with content.
`ENTER-FLOOR` in [state.zil](../state.zil) reseeds the PRNG for the target
floor so revisits are stable, using the per-floor seed data stored on the floor
objects (see helpers like `FLOOR-SET-SEEDS` in [objects.zil](../objects.zil)
and the PRNG in [random.zil](../random.zil)). With the RNG set, dungeon
construction begins in [dungeon.zil](../dungeon.zil) through `BUILD-DUNGEON`:
`CLEAR-MAP` resets the tile grid, `PLACE-ROOMS` and `TRY-ADD-ROOM` carve
non-overlapping rooms, and `CONNECT-ROOMS` stitches them together with a
spanning tree plus optional loops. For floors 2+, the state pipeline can set
`FORCED-ENTRY?` so a specific entry room (the stored up-stair coordinates) is
placed consistently before the rest of the layout is carved.

Once the layout exists, [state.zil](../state.zil) performs floor-specific
placement. Stairs are placed or reused with `ENTER-FLOOR-PLACE-UP-STAIRS` and
`ENTER-FLOOR-PLACE-DOWN-STAIRS`, and static features (trader, interiors,
treasure placements) are loaded via `ENTER-FLOOR-LOAD-FEATURES`. On first visit,
item spawns are created using the loot subsystem (`SPAWN-WEAPONS`,
`SPAWN-FOOD`, `SPAWN-POTION` in [loot.zil](../loot.zil)), then
`GENERATE-TREASURE-ROOMS` runs, and enemies are loaded through
`LOAD-FLOOR-ENEMIES`.

## Typical turn flow

Each turn is anchored by the UI loop in [ui.zil](../ui.zil). `RASCAL-MAIN-LOOP`
reads input, translates it into an action via `HANDLE-INPUT`, and then advances
the simulation. A movement or attack typically updates player position and
triggers reveal logic (`REVEAL-AROUND` and `REVEAL-ROOM` in
[explore.zil](../explore.zil)), while item pickups or inventory actions route
through [inventory.zil](../inventory.zil) and the object pool helpers in
[objects.zil](../objects.zil). If the action is combat-related, damage
resolution and effects flow through [combat.zil](../combat.zil).

After the player acts, enemy behavior runs (via `STEP-ENEMIES` and related AI
helpers in [combat.zil](../combat.zil)), and global per-turn effects such as bee
swarm updates or timed status changes are applied. Enemy behavior only runs
when `HANDLE-INPUT` returns false (movement/combat/wait actions); stairs and
UI-only commands skip the enemy step. The loop then checks end-of-run conditions
using `CHECK-END`, updates stats as needed, and finally redraws the UI with
`DRAW`.

## Subsystems

### Combat and enemy AI

File: [combat.zil](../combat.zil)

Combat is responsible for enemy definitions, the player and enemy damage model,
AI movement, and the timing of special enemy behaviors. The module defines
enemy types (`ETYPE-*`) so other systems can reason about sprites, loot, and
balance. The main loop drives enemies via `STEP-ENEMIES`, which in turn uses
`STEP-ENEMY-AI` and pathing helpers like `TRY-ENEMY-MOVE` and
`STEP-ENEMY-TOWARD`. The player attack path goes through `PLAYER-ATTACK` and
`PLAYER-DAMAGE`, while enemies attack through `ENEMY-ATTACK-PLAYER`. The code
also defines short-lived visual feedback using `HIT-FLASH?` so the UI can signal
hits.

The module also owns special enemy logic because it needs access to movement
and combat primitives. Monkeys can steal or carry items, and bee swarm behavior
is handled by `START-BEE-SWARM`, `UPDATE-BEE-SWARM`, and `SPAWN-BEE-ENEMY`
(with the swarm triggered from the interior flow). This module also defines
tuning parameters such as `CHASE-RADIUS` and initial player stats like
`PLAYER-HP` and `PLAYER-STR`.

### Dungeon generation and map helpers

File: [dungeon.zil](../dungeon.zil)

This subsystem builds the dungeon layout and provides low-level accessors for
map tiles and room metadata. `BUILD-DUNGEON` orchestrates the generation pass:
it clears the map (`CLEAR-MAP`), places rooms (`PLACE-ROOMS` and
`TRY-ADD-ROOM`), and then connects rooms using a spanning tree plus extra
loops (`CONNECT-ROOMS`, `CONNECT-SPANNING-TREE`, `ADD-EXTRA-LOOPS`). This
produces a layout that is always navigable and includes optional loops.

Map queries such as `MAP-INDEX`, `TILE-AT`, `ROOMID-AT`, and `FLOOR?` are kept
here to avoid duplication and to keep map representation stable. The system
stores rooms in a compact table (`ROOMS-TABLE`) for quick access to centers and
bounds, which other modules use for player placement, stair placement, and room
reveal.

### Exploration and fog of war

File: [explore.zil](../explore.zil)

Exploration owns what the player can see and what they have already discovered.
Reveal state is stored as a bitset in `FLOOR-REVEALED`, which makes memory use
predictable and allows fast checks with `REVEALED?`. Updates happen through
`REVEAL-TILE`, `REVEAL-AROUND`, and `REVEAL-ROOM`, and room discovery is tracked
in `FLOOR-ROOM-DISCOVERED` with `COUNT-FLOOR-ROOMDISC` used to update the per-
floor discovered-room count on entry.

Stair traversal (`GO-UP`, `GO-DOWN`) lives here because it combines reveal state
and traversal rules, while delegating the heavy lifting of floor transitions to
`ENTER-FLOOR` in [state.zil](../state.zil).

### Items and static data tables

File: [itemdef.zil](../itemdef.zil)

This module defines the canonical constants and lookup functions for items.
It is effectively the data dictionary for item kinds (`ITEMKIND-*`), food,
weapons, potions, treasures, and key/lock types. The code in other modules
relies on `WEAPON-NAME` plus the derived weapon-profile helpers such as
`WEAPON-POWER`, `WEAPON-MIN-DMG`, `WEAPON-MAX-DMG`,
`WEAPON-CRIT-PCT`, and `WEAPON-AVERAGE-BASE-DMG` to compute damage,
ranking, and display behavior; `FOOD-NAME` and `FOOD-HEAL-AMT` determine
healing; and potion helpers like `POTION-DISPLAY-NAME` and
`POTION-ARTICLE` manage the discovery illusion by swapping color names for
effect names when identified.

### Object system and pooling

File: [objects.zil](../objects.zil)

Rascal uses Z-machine objects as its primary runtime data structure because
they are flexible and work in both dungeon mode and parser interiors. Objects
are pooled, since the Z-machine has a static object table. The module
defines the root objects (`RASCAL-OBJECTS`, `RASCAL-FLOORS`, `PLAYER-INVENTORY`)
and per-floor helpers (`FLOOR-OBJ`, `TRADER-OBJ`), then provides pool
operations like `ALLOC-RASCAL-ITEM`, `FREE-RASCAL-ITEM`,
`ALLOC-RASCAL-ENEMY`, and `FREE-RASCAL-ENEMY`. Cleanup helpers like
`FREE-RASCAL-ITEM-CHILDREN` and `FREE-RASCAL-ENEMY-CHILDREN` are provided for
clearing pooled objects when resetting state.

It also stores per-floor metadata such as seeds and stair coordinates in floor
objects using helpers like `FLOOR-SET-UP-STAIRS`, `FLOOR-SET-DOWN-STAIRS`, and
`FLOOR-SET-SEEDS`. The reason this lives here is to keep the data model in one
place and ensure that higher-level modules do not need to understand property
layouts.

### Inventory and equipment

File: [inventory.zil](../inventory.zil)

Inventory is modeled as items that are children of `PLAYER-INVENTORY`, which
keeps it consistent with the object model and makes it easy to move items
between the floor and the pack. Core inventory operations such as
`INV-COUNT`, `INV-NTH-OBJ`, `INV-TAKE-OBJ`, `INV-ADD`, `INV-ADD-WEAPON`, and
`INV-REMOVE` all operate on object relationships rather than custom arrays.
The UI leverages `POPUP-INVENTORY-GETCHAR` and `INV-NAME` to display a compact
in-game menu.

Equipment management is centered on `EQUIPPED-WEAPON`, with `TRY-EQUIP-WEAPON`
validating that the player can wield a weapon and `AUTO-EQUIP-WEAPON` choosing
an inventory option based on its internal heuristics, which now use
`WEAPON-AVERAGE-BASE-DMG` as the tie-breaker within a level. Combat reads the
global pointer to determine the active weapon.

### Loot and spawners

File: [loot.zil](../loot.zil)

Loot is responsible for randomized drops and per-game variability. `INIT-POTIONS`
randomizes the color-to-effect mapping using `POTION-COLOR-POOL` and
initializes discovery flags. Treasure scheduling is handled by `INIT-TREASURES`,
which assigns each treasure to a floor within allowed ranges
(`TREASURE-MIN-FLOOR`, `TREASURE-MAX-FLOOR`).

Drop helpers like `ADD-GOLD-PILE` and `DROP-GOLD-NEAR` avoid dropping on blocked
tiles and merge piles when possible. Weapon spawns use `ROLL-LOOT-WEAPON-LEVEL`
and `ROLL-LOOT-WEAPON-ENCH` to tie progression to floor depth.

### Utility helpers

File: [misc.zil](../misc.zil)

Misc contains small, shared primitives used by multiple modules. `ABS` and
`CLAMP` support common calculations, while `DIR8-DX`, `DIR8-DY`, `DIR9-DX`, and
`DIR9-DY` provide a common direction encoding for movement and adjacency checks.
`DIGIT-TO-SLOT` is a shared conversion for inventory and shop selection.

### Random number generator

File: [random.zil](../random.zil)

Rascal uses a deterministic PRNG to make seeded runs stable across
interpreters. `SEED-RNG-32` sets the 32-bit state (`RNG-STATE-HI` and
`RNG-STATE-LO`), and `RNG-STEP` advances it with an xorshift algorithm.
`RNG` provides a simple 1..N interface to callers, while `INIT-RNG-RANDOM`
seeds from the interpreter's built-in `RANDOM` when no explicit seed is set.

### Trader shop and economy UI

File: [shop.zil](../shop.zil)

The shop subsystem implements the per-floor trader and its economy. Trader
inventory is stored as item children under the per-floor trader object, and
`TRINV-COUNT`, `TRINV-NTH-OBJ`, and `TRINV-ADD-FLOOR` mirror the inventory API
so the UI can present it in the same slot-based format. Prices are computed by
`TRADER-BUY-PRICE` and `TRADER-SELL-PRICE`, which ensures a single source of
truth for value conversions in both directions.

`DRAW-TRADER-SHOP` and `TRADER-SHOP` handle the interaction loop, and
`INIT-TRADER-STOCK` uses `TRADER-ADD-RANDOM-STOCK` to construct a selection of
items per visit.

### Game state and floor transitions

File: [state.zil](../state.zil)

State ties the game together by managing persistent run data and the floor
entry pipeline. Global values like `CURRENT-FLOOR`, `PLAYER-X`, `PLAYER-Y`,
`PLAYER-GOLD`, `ROOM-COUNT`, `CURRENT-ROOM`, `GAME-OVER?`, and `YOU-WIN?`
represent the canonical runtime state. Floor transitions are orchestrated by
`ENTER-FLOOR`, which reseeds RNG for determinism, rebuilds the dungeon, places
stairs, positions the player, and triggers reveal. Helper routines such as
`ENTER-FLOOR-SET-FORCED-ENTRY`, `ENTER-FLOOR-PLACE-UP-STAIRS`,
`ENTER-FLOOR-PLACE-DOWN-STAIRS`, `ENTER-FLOOR-LOAD-FEATURES`, and
`ENTER-FLOOR-LOAD-STATE-AND-REVEAL` split this into manageable steps.

The reason this is centralized is that floor entry touches nearly every system
at once. Keeping the pipeline in one file helps keep traversal directions and
per-floor persistence like `FLOOR-ENEMY-INIT` and `TREASURE-ROOM-CREATED` in
one place.

### Statistics and scoring

File: [statistics.zil](../statistics.zil)

Statistics are tracked for end-of-run feedback and scoring. The subsystem keeps
per-run counters such as `STATS-POTIONS-DRANK`, `STATS-FOODS-EATEN`,
`STATS-ENEMIES-KILLED`, and `STATS-TREASURES-PICKED`, and exposes helpers like
`STATS-RESET` and `STATS-INC-WORD-TABLE`. `FINAL-SCORE` computes the numeric
score using gold, inventory value, stat upgrades, and a win bonus, while
`GAME-STATS` and its page routines (`DRAW-GAME-STATS-SUMMARY`,
`DRAW-GAME-STATS-CONSUMABLES`, `DRAW-GAME-STATS-COMBAT-LOOT`) display a
multi-page breakdown. Housing this in its own module keeps UI detail and
scoring rules isolated from core gameplay logic.

### UI, input, and rendering

File: [ui.zil](../ui.zil)

The UI subsystem is responsible for the main loop, input handling, rendering,
and player-facing messages. The game loop in `RASCAL-MAIN-LOOP` reads keys,
advances simulation, checks end conditions with `CHECK-END`, and redraws via
`DRAW`. Logging is centralized in `LOG` and `TELL/LOG`, which manage window
switching and consistent colors (`UI-LOG-COLOR`). Rendering uses tile constants
like `TILE-*` and color application via `APPLY-SPRITE-COLOR`, and layout
constants such as `MAP-ROW` and `UPPER-HEIGHT` keep the map and header aligned.

### Interiors and parser bridge

File: [interior.zil](../interior.zil)

Interiors are classic parser rooms that coexist with the roguelike grid. This
module defines entrance tables (`INTERIOR-ENTRANCE-*`) and the transition flow
through `LAUNCH-INTERIOR`, `INTERIOR-ENTER-SYNC`, `INTERIOR-EXIT-SYNC`, and
`INTERIOR-TELEPORT-PLAYER`. The synchronization step is important because it
keeps the parser inventory and the dungeon inventory aligned and avoids
duplicate or missing objects after the player exits.

It also adapts parser behavior to Rascal's object model. Overridden routines
such as `INDISTINGUISHABLE?` and `FAILS-HAVE-CHECK?` allow special cases like
addressing tamed monkeys or handling Rascal items in inventory logic. The
module tracks bee swarm state (`BEE-SWARM-*`) and triggers `START-BEE-SWARM`
when leaving the hive; swarm behavior itself is handled in combat.

### NPC rooms and interactions

File: [npc.zil](../npc.zil)

The NPC module defines the interior rooms and their behaviors. Rooms like
`INFO-BOOTH`, `BEE-HIVE`, `BLACKSMITH-SHOP`, and `CARROT-FARM` combine parser
descriptions with actionable hooks. For example, `INFO-MAN-F` handles
interaction with the info booth NPC, and `BLACKSMITH-ENCHANT-WEAPON`
implements the upgrade economy that spends gold and modifies weapon
enchantment. `CARROT-MAN-F` and related helpers drive the carrot farm's
purchase and identification services.

Keeping these behaviors isolated in one file allows narrative content and
special-case logic to evolve without touching the core roguelike systems. The
interior infrastructure in [interior.zil](../interior.zil) handles transitions,
so this module can focus on story and interaction.
