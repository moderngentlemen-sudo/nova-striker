# Gameplay Complete Development Run

Branch: `dev/gameplay-complete`

## Objective

Build every gameplay/mechanics system that can be completed with Unity code,
generated data, and greybox assets before production modeling/rigging/animation
becomes the limiting dependency.

The preserved browser build remains a behavior reference, not a ceiling.
Approved Nova/Echo design direction and the developing Narrative Foundations
material are allowed to drive new mechanics where they improve the current game.

## Design constraints

- 1–4 simultaneous local players on one gameplay field.
- Canonical four-person Strike Team roles:
  - Tank
  - Striker 0
  - Striker 1
  - Support
- Striker 1 is team lead; Striker 0 is second-in-command.
- Nova and Echo share low-level infrastructure but have genuinely different
  Strike Suit combat identities.
- Gameplay timing is fixed-step and independent of render frame rate.
- Presentation systems consume gameplay state/events; animation/VFX do not own
  hit, counter, dash, Break, damage, or ability timing.
- Final art remains external to gameplay collision/authority.
- Cross-platform scalability must support the requested console, desktop,
  handheld, and mobile targets without changing simulation rules.
- Commerce is entitlement-driven and storefront-neutral.

## Implemented on this branch so far

### Four-player runtime

- four stable local player slots
- per-player controller isolation and reconnect identity
- Player 1 keyboard option
- shared one-screen camera framing
- enemy retargeting across combat-ready players
- player/player body collision suppression
- four-player downed/revive flow
- shared Synergy
- Pair / Formation / Full Strike Sync tiers
- Team Sync damage, Break, healing, invulnerability, and suit-energy rewards

### Character gameplay

- Nova/Echo movement and contextual Counter foundation
- Echo enemy/surface grapple split and geometry occlusion
- Strike Suit energy/ultimate framework
- Nova Sentinel direction:
  - Bulwark Pulse
  - Sentinel Lock
  - Sentinel Screen
  - Frontline Protocol ultimate
- Echo Pursuit direction:
  - Pursuit Mark
  - Reel Strike
  - Staff Burst
  - Pursuit Protocol ultimate
- player Guardian ability controller
- independent per-player weapon/Guardian loadouts

### Combat

- health, invulnerability, knockback
- healing and revive
- shields
- armor
- Break gauge/stagger
- burn, shock, slow, mark, vulnerable, exposed
- projectile ownership/cancellation/reflection
- damage-dealt / defeat-dealt actor events
- melee/counter/traversal damage integration
- per-player Style meter with variety/repeat logic and rank decay
- weapon mastery progression persisted in save data

### Arsenal / Guardians

- generated data for all 12 weapons
- generated data for all six Guardians
- player-usable Guardian ability implementations
- weapon status effects integrated through CombatState2D

### Enemies

- shared enemy brain
- Anchor, Skirmisher, Flanker, Artillery, Aerial roles
- browser-derived mapping for all 12 named archetypes
- archetype-specific runtime controller for preserved reactions
- four-player target selection
- Break/status integration

### Encounters / bosses

- scalable 1–4 player encounter controller
- spawned waves and preplaced enemy support
- player-count-based wave scaling
- camera arena locking
- encounter completion/Synergy events
- persistent checkpoint trigger
- four mini-boss gameplay controller:
  - Bulwark
  - Vector Hound
  - Cliff Stalker
  - Rail Sentinel
- shared six-Guardian boss controller with:
  - three health phases
  - multiplayer targeting
  - distinct attack families
  - weak-point windows
  - Guardian-specific defense profiles
  - Aegis, Cinder, Mycel, Rime, Tempest, Null behavior identities

### Campaign / progression

- six-sector / eighteen-act catalog
- sector hazards
- versioned save service with replaceable backend
- sector/act/checkpoint progression
- weapon unlock/mastery save structure
- Guardian unlock structure
- four-player cosmetic save structure

### Commerce / DLC

- storefront-neutral commerce catalog
- provider interface
- editor/offline mock provider
- persistent entitlements
- cosmetic ownership
- four-player cosmetic loadouts
- DLC/cosmetic entitlement gates
- sample generated cosmetic and expansion products
- gameplay-stat purchase flag kept separate from cosmetic/DLC ownership

### Platform scalability

- fixed 60 Hz gameplay simulation
- runtime presentation quality tiers
- scalable target render rate
- VFX density budget
- shadow/light/render-scale budget hooks
- mobile/legacy-console-oriented quality selection
- presentation scalability remains independent of mechanics

## Validation status

The earlier `dev/unity-gameplay` baseline was successfully imported/compiled in
Unity 6.6 and received user Play Mode validation for multiple core mechanics,
including the corrected wall slide.

The systems added on `dev/gameplay-complete` have **not yet received a fresh
Unity 6.6 compile/Play Mode pass**. Until that occurs, they are code-integrated,
not QA-approved.

## Remaining before the Blender boundary

Major remaining engineering passes include:

- Unity compile/error reconciliation for the expanded branch
- full mechanics-lab integration/telemetry for new systems
- dedicated boss/encounter test lab
- complete named-enemy prefab generation and balancing
- squad encounter tactics beyond individual-role behavior
- pickups/recovery/reward spawning
- secret routes, gauntlets, weapon trials, traversal courses
- sector setpiece runtime controllers
- act runtime/scene transition authority
- game-over / party-wipe / checkpoint respawn flow
- skill/perk runtime and unlock effects
- deeper four-player cooperative interactions/assist chains
- additional Nova/Echo identity tuning
- placeholders and data contracts for the eventual Tank/Support characters
- touch/mobile input adapter
- projectile/enemy pooling and hot-path allocation pass
- deterministic tests and editor validation tooling
- commerce restore/reconciliation hardening for native provider adapters
- final pre-art architecture and dependency audit

The development run stops only when remaining meaningful work primarily requires
production models, rigs, authored animation, textures/materials, environment art,
or other Blender/production-art deliverables.
