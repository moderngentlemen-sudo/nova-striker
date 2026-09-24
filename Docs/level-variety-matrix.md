# Nova Striker — 18-Act Level Variety Matrix

Branch: `dev/gameplay-complete`

This matrix is the final pre-Blender level-design gameplay contract. It gives each
campaign act a distinct topology, traversal emphasis, objective style, route
choice, hazard rhythm, and climax while preserving the existing six-sector /
eighteen-act campaign, one-to-four-player architecture, and art-independent
gameplay authority.

Production environment art should realize these contracts rather than redefine
them. Pair/team splits are optional co-op parallelism: every act must remain
completable by one player through sequential interactions or a solo route.

| Sector / Act | Topology | Traversal emphasis | Objective | Route choice | Hazard pattern | Climax |
| --- | --- | --- | --- | --- | --- | --- |
| Skyport 1 — Transit Spine | Moving convoy | Momentum | Advance | Upper / lower | Sweeping | Rail Sentinel intercept |
| Skyport 2 — Maintenance Interior | Split route | Precision | Disable nodes | Safe / fast | Alternating lanes | Vector Hound maintenance lock |
| Skyport 3 — Upper Skyline | Vertical ascent | Moving platforms | Survival | Combat / traversal | Pursuit pressure | Aegis skyline platform |
| Ember Works 1 — Furnace Walk | Linear run | Hazard timing | Hold zone | Risk / reward | Burst windows | Bulwark furnace gate |
| Ember Works 2 — Foundry Shaft | Vertical descent | Vertical | Survival | Upper / lower | Alternating lanes | Vector Hound foundry floor |
| Ember Works 3 — Core Forge | Looping arena | Momentum | Disable nodes | Dynamic | Pursuit pressure | Cinder core forge |
| Verdant Vault 1 — Root Access | Split route | Grapple | Advance | Combat / traversal | Standard | Cliff Stalker root chamber |
| Verdant Vault 2 — Canopy Engine | Layered arena | Grapple | Disable nodes | Upper / lower | Alternating lanes | Bulwark canopy engine |
| Verdant Vault 3 — Memory Garden | Reconfiguring space | Moving platforms | Multi-front | Dynamic | Reconfiguring | Mycel memory garden |
| Cryo Relay 1 — Frozen Array | Linear run | Momentum | Advance | Safe / fast | Alternating lanes | Rail Sentinel frozen array |
| Cryo Relay 2 — Coolant Vault | Vertical descent | Precision | Hold zone | Risk / reward | Burst windows | Bulwark coolant vault |
| Cryo Relay 3 — Glass Relay | Reconfiguring space | Hazard timing | Survival | Combat / traversal | Reconfiguring | Rime glass relay |
| Storm Spire 1 — Lower Conduit | Vertical ascent | Momentum | Advance | Upper / lower | Sweeping | Vector Hound conduit rise |
| Storm Spire 2 — Thunder Ring | Looping arena | Hazard timing | Multi-front | Risk / reward | Alternating lanes | Cliff Stalker thunder ring |
| Storm Spire 3 — Spire Crown | Moving convoy | Momentum | Pursuit | Dynamic | Pursuit pressure | Tempest spire crown |
| Eclipse Core 1 — Outer Shell | Layered arena | Team split | Multi-front | Upper / lower | Reconfiguring | Rail Sentinel outer shell |
| Eclipse Core 2 — Memory Lattice | Reconfiguring space | Precision | Disable nodes | Combat / traversal | Reconfiguring | Cliff Stalker memory lattice |
| Eclipse Core 3 — Dawn Engine | Reconfiguring space | Team split | Survival | Dynamic | Reconfiguring | Null dawn engine |

## Sector gameplay grammar

### Skyport

Skyport teaches movement through active infrastructure. Moving lanes, service
routes, lift chains, train gaps, and exposed crossfire create a fast transit
identity. Route splits should reward confident movement without making the
slower route feel like a punishment.

### Ember Works

Ember Works is about cadence and pressure. Vents, safe bays, crushers, shafts,
cooling windows, and forge loops make timing as important as raw speed. The
Core Forge recombines these ideas under Furnace Surge pressure.

### Verdant Vault

Verdant Vault emphasizes organic branching and Echo-friendly traversal.
Grappleable overhead structure, wall routes, canopy layers, shifting garden
paths, and spore bypasses should make the sector feel exploratory while
remaining readable for four players on one camera.

### Cryo Relay

Cryo Relay changes how players think about momentum and footing. Stable side
routes contrast with faster icy routes, fragile ledges, temporary platforms,
and collapse cycles. The Glass Relay should feel like the sector progressively
removing safe assumptions.

### Storm Spire

Storm Spire is velocity under electrical pressure. Vertical conduit climbs,
energy rails, moving danger lanes, circular arenas, shortcuts, and chase
geometry culminate in the Spire Crown pursuit.

### Eclipse Core

Eclipse Core deliberately recombines and destabilizes mastered rules. Layered
fronts, optional pair splits, route shifts, false paths, Null-grid changes, and
formation pressure create the campaign's highest systemic density without
introducing art-dependent gameplay authority.

## Runtime implementation

`ActLevelVarietyCatalog` owns the eighteen gameplay plans.

`ActLevelVariationController2D` lets a greybox or production scene bind
GameObject groups to stable module tags. On act configuration it enables the
matching modules, disables unmatched modules when configured to do so, and
stagger-resets sector hazards using the act's hazard phase stride.

`ActObjectiveController2D` provides reusable authority for:

- encounter-clear objectives
- advancing to a goal
- timed hold zones
- sequential or parallel node activation
- pursuit checkpoints
- survival timers
- protected-asset encounter gates
- multi-front node objectives

`ActObjectiveTrigger2D` supplies scene-side Zone, Node, and Goal triggers.
Node objectives never require multiple simultaneous players: co-op teams may
split and activate them in parallel, while solo players may complete them
sequentially.

`CampaignActDirector2D` now resolves both `ActGameplayCatalog` and
`ActLevelVarietyCatalog`, then configures level variation and objective
controllers alongside hazards, setpieces, encounters, secrets, and bosses.

## Production boundary

The following remain Blender/environment-production work rather than additional
gameplay-code tasks:

- final modular environment meshes
- architectural silhouettes and set dressing
- production moving machinery
- final collision replacement derived from approved greyboxes
- textures, materials, emissives, lighting, particles, destruction visuals
- authored environmental animation

The gameplay contract should be changed only when Play Mode testing demonstrates
a level-design problem, not simply to accommodate a production-art preference.


## Generated playable topology labs

The Editor now exposes:

`Nova Striker > Greybox > Level Variety > Build All Topology Labs`

This generates eight representative scenes under:

`Assets/Greybox/LevelVariety/Scenes/`

The current lab set is:

1. Linear Run — Frozen Array
2. Moving Convoy — Transit Spine
3. Vertical Ascent — Upper Skyline
4. Vertical Descent — Foundry Shaft
5. Split Route — Maintenance Interior
6. Layered Arena — Canopy Engine
7. Looping Arena — Core Forge
8. Reconfiguring Space — Memory Lattice

Each scene clones the current Mechanics Lab runtime shell so the four-player
session, camera, input, progression services, and playable Striker prefabs stay
consistent. The builder removes the original test-room geometry and replaces it
with topology-specific primitives, moving platforms, alternating platform
groups, hazards, and objective triggers.

These labs are representative topology tests, not final act layouts. Their job
is to answer pre-production questions such as:

- does the shared camera tolerate the route shape?
- can one player complete the route without co-op?
- can two-to-four players divide routes without becoming stranded?
- do moving/reconfiguring surfaces remain readable during combat?
- does the traversal grammar feel materially different from the Mechanics Lab?
- do objective triggers remain legible without production presentation?

The generated scenes should be rebuilt freely as the level contract is tuned.
Final Blender environment work should replace their visual geometry rather than
move gameplay authority into production art.


## Topology lab validation and feedback

The generated labs now retain a representative combat set from the Mechanics
Lab so geometry can be evaluated under light skirmisher, flanker, artillery,
and aerial pressure rather than as empty traversal courses.

Each generated scene also contains `GreyboxLevelVarietyHUD`, which displays
the active act key, topology, traversal emphasis, objective style, route-choice
contract, hazard pattern, pair-split support, objective progress, and completion
state.

After building the labs, run:

`Nova Striker > Validation > Validate Generated Level Variety Labs`

The validator checks that all eight scenes exist and contain:

- exactly one level-variety bootstrap
- exactly one act-objective controller
- exactly one level-variety HUD
- all four Strike Team player objects
- the shared Strike Team camera
- sector-hazard volumes
- the required Zone / Node / Goal objective triggers
- moving-platform runtime where required
- reconfiguring-space phase-toggle runtime where required
- representative enemy pressure

This is structural validation only. A passing result does not prove that a
route feels good, that a moving platform carries players correctly, or that the
shared camera remains readable during actual four-player Play Mode.
