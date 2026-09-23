# Gameplay-Complete Branch Status

Branch: `dev/gameplay-complete`

This branch is the full pre-Blender gameplay-development run. The intent is to exhaust meaningful gameplay/runtime engineering that can be implemented with Unity code, generated data, and greybox content before production modeling, rigging, animation, environment art, final VFX/audio, or native platform SDK work becomes the limiting dependency.

## Current code-complete systems

### Four-player local co-op

- one-to-four simultaneous local player slots
- canonical Strike Team roles: Tank, Striker 0, Striker 1, Support
- Striker 1 lead / Striker 0 second-in-command metadata and rules
- art-independent Tank/Support gameplay contracts
- isolated per-player controller assignment
- remembered controller identity and reconnect reservation
- explicit join/leave hooks
- Player 1 keyboard support
- mobile/touch virtual-input abstraction
- one-screen dynamic four-player camera framing
- player-player collision suppression
- nearest-combat-ready enemy targeting across four players
- shared Synergy meter and Pair / Formation / Full Strike Sync tiers
- co-op downed state, multi-contributor revive, assist-chain rewards, and downed-player camera retention

### Nova and Echo gameplay identity

- Nova ranged Sentinel/protector direction
- Echo Pursuit Protocol direction
- Nova Deflect / perfect Deflect
- Echo enemy grapple and solid-surface traversal grapple
- close Dodge Counter and advancing Throw
- character-specific Strike Suit energy / secondary ability / Ultimate framework
- Nova Bulwark Pulse, Sentinel Lock, Sentinel Screen, Frontline Protocol hooks
- Echo Pursuit Mark, Reel Strike, Staff Burst, Pursuit Protocol hooks
- character-specific gameplay cues remain presentation-independent

### Combat and progression

- health / healing / invulnerability / defeat / revival
- knockback and external velocity
- shields and shield break
- armor and armor break
- Break gauge and stagger
- mark / vulnerable / exposed
- cryo slow
- burn
- shock and shock chaining
- projectile cancellation and reflection
- hostile perfect-opportunity projectiles
- melee, aerial melee, dive melee
- Dash / Velocity Break / Powerslide damage
- per-player Style meter with variety/repetition logic and decay
- shared weapon mastery progression
- six baseline skill/perk unlocks with runtime effects

### Weapons and Guardian abilities

- 12 generated WeaponDefinition assets
- loadout cycling per player
- established weapon behavior categories wired into runtime
- boomerang, gravity-well, mine, spread, cyclone, beam/pierce/status behavior foundations
- projectile pooling foundation
- six player-usable Guardian abilities
- six generated GuardianDefinition assets

### Enemy gameplay

- shared EnemyBrain2D
- Anchor, Artillery, Flanker, Skirmisher, Aerial roles
- full named 12-archetype catalog
- named archetype behavior controller
- four-player target selection
- stagger/slow/status integration
- mixed-role squad coordinator
- Advance / Fortify / Pincer / Crossfire positioning logic
- geometry-aware firing / grapple occlusion
- pooled encounter-enemy foundation

### Encounter and campaign runtime

- six sectors / eighteen-act gameplay catalog
- authored scalable combat compositions for all eighteen acts
- sector hazards
- setpiece runtime for Train Rush, Furnace Surge, Vine Bridge, Ice Collapse, Lightning Chase, Null Warp
- scalable one-to-four-player encounter waves
- arena camera locking
- persistent checkpoint triggers
- health / Suit Energy / Synergy / Ultimate pickups
- encounter reward spawning
- Gauntlet / Weapon Trial / Dash Course secret-challenge framework
- campaign progression authority
- party-wipe, retry, and checkpoint-respawn flow
- versioned save system with replaceable local/cloud backend contract

### Boss framework

- four preserved mini-boss identities represented by shared mini-boss runtime
- six Guardian boss identities represented by shared phase/weak-point runtime
- Guardian phase transitions
- Guardian-specific action families and defense profiles
- Guardian weak-point open/close gameplay events
- multi-player target acquisition

### Platform / performance architecture

- fixed 60 Hz gameplay simulation independent from render FPS
- low / medium / high / ultra runtime presentation budgets
- 60/120 render-target architecture
- scalable VFX/light/shadow/render-scale guidance
- storefront-neutral platform runtime provider interface
- cloud-save backend extension contract
- mobile/touch input bridge

### Commerce / DLC

- storefront-neutral commerce catalog/provider interface
- editor/offline mock commerce provider
- purchase restore and entitlement-reconciliation extension contract
- persistent entitlements
- per-player cosmetic loadout persistence
- suit / helmet / weapon / trail cosmetic slots
- entitlement-driven DLC content gates
- gameplay-stat purchase flag exists so commerce can remain cosmetic/content-oriented by default

### Validation / developer tooling

- Mechanics Lab greybox builder
- boss/enemy/debug spawning support
- scene/configuration Gameplay Preflight validator
- scene-independent Structural Batch validator
- structural validation covers campaign composition, 12-enemy coverage, four-role Strike Team command contracts, and skill/perk catalog invariants
- batch validator can return a non-zero Unity command-line exit code on structural failure

## Unity validation status

The earlier `dev/unity-gameplay` baseline was imported/compiled in Unity 6.6 and received user Play Mode validation for multiple core mechanics, including the corrected wall slide.

The expanded `dev/gameplay-complete` branch is **code-integrated but not yet freshly Unity-validated**. No code-complete claim in this branch should be interpreted as a successful Unity 6.6 compile, Play Mode QA pass, profiler pass, or platform certification result.

## Remaining meaningful pre-Blender work

The branch is now close to the pre-Blender boundary. The remaining material work is primarily validation, profiling, integration verification, and tuning rather than missing gameplay architecture:

- fresh Unity 6.6 compile/error reconciliation
- regenerate the Mechanics Lab and run both validation tools
- one/two/three/four-player Play Mode smoke passes
- join/leave and controller disconnect/reconnect validation
- end-to-end validation of all weapons, Guardians, perks, named enemies, bosses, pickups, challenges, setpieces, retries, saves, commerce restore, and DLC gates
- representative profiler/allocation captures and pool/budget tuning
- final gameplay-facing presentation socket / Animator / VFX / audio / haptic contract freeze
- native provider adapters only where Steam/Xbox/PlayStation/Nintendo/mobile SDK access and credentials permit

See `Docs/pre-blender-boundary.md` for the explicit dependency matrix and stop condition.

## Stop boundary

Do not add gameplay authority merely to compensate for missing production art. Once the validation/profiling list above passes, remaining meaningful work should move to Blender/production assets and Unity presentation integration: final models, rigs, authored animation, materials/textures, environment meshes, final VFX/audio/lighting, and production polish.
