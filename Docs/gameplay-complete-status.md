# Gameplay-Complete Branch Status

Branch: `dev/gameplay-complete`

This branch is the full pre-Blender gameplay-development run. The goal is to complete every meaningful gameplay/runtime system that can be implemented and validated with generated/greybox assets before production modeling, rigging, animation, and environment art become the limiting dependency.

## Current code-complete / in-progress systems

### Players and four-player local co-op

- one-to-four simultaneous local player slots
- canonical Strike Team roles: Tank, Striker 0, Striker 1, Support
- Striker 1 lead / Striker 0 second-in-command metadata
- isolated per-player controller assignment
- keyboard support for Player 1
- mobile/touch virtual-input abstraction
- one-screen dynamic four-player camera framing
- player-player collision suppression
- nearest-combat-ready enemy targeting across four players
- shared Synergy meter
- Pair / Formation / Full Strike Sync tiers
- co-op downed and multi-contributor revive
- downed-player camera retention

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

### Combat

- health / healing / invulnerability / defeat / revival
- knockback and external velocity
- shields and shield break
- armor and armor break
- Break gauge and stagger
- mark / vulnerable / exposed
- cryo slow
- burn
- shock state
- projectile cancellation and reflection
- hostile perfect-opportunity projectiles
- melee, aerial melee, dive melee
- Dash / Velocity Break / Powerslide damage
- per-player Style meter with variety/repetition logic and decay
- shared weapon mastery progression

### Weapons and Guardian abilities

- 12 generated WeaponDefinition assets
- loadout cycling per player
- all established weapon behavior categories wired into the runtime
- boomerang, gravity-well, mine, spread, cyclone, beam/pierce/status behavior foundations
- six player-usable Guardian abilities
- six generated GuardianDefinition assets

### Enemy gameplay

- shared EnemyBrain2D
- Anchor, Artillery, Flanker, Skirmisher, Aerial roles
- named 12-archetype browser-reference catalog
- named archetype behavior controller
- four-player target selection
- stagger/slow integration
- mixed-role squad coordinator
- Advance / Fortify / Pincer / Crossfire positioning logic
- geometry-aware firing / grapple occlusion

### Encounter and campaign runtime

- six sectors / eighteen-act catalog
- sector hazards
- setpiece runtime for Train Rush, Furnace Surge, Vine Bridge, Ice Collapse, Lightning Chase, Null Warp
- scalable one-to-four-player encounter waves
- arena camera locking
- persistent checkpoint triggers
- health / Suit Energy / Synergy / Ultimate pickups
- Gauntlet / Weapon Trial / Dash Course secret-challenge framework
- campaign progression authority
- versioned save system with replaceable backend

### Boss framework

- four preserved mini-boss identities represented by a shared mini-boss runtime
- six Guardian boss identities represented by shared phase/weak-point runtime
- Guardian phase transitions
- Guardian-specific action families
- Guardian weak-point open/close gameplay events
- multi-player target acquisition

### Platform / performance architecture

- fixed 60 Hz gameplay simulation independent from render FPS
- low / medium / high / ultra runtime presentation budgets
- 60/120 render-target architecture
- scalable VFX/light/shadow/render-scale guidance
- storefront-neutral saves and commerce interfaces
- mobile/touch input bridge

### Commerce / DLC

- storefront-neutral commerce catalog/provider interface
- editor/offline mock commerce provider
- persistent entitlements
- per-player cosmetic loadout persistence
- suit / helmet / weapon / trail cosmetic slots
- entitlement-driven DLC content gates
- gameplay-stat purchase flag exists so commerce can remain cosmetic/content-oriented by default

## Not yet Unity-validated in this branch

The original Unity migration and approved wallslide fix were previously user-tested in Unity 6.6, but the large gameplay-complete expansion has **not** yet received a fresh full Unity 6.6 compile/Play Mode pass.

Therefore the systems above are code status, not QA approval.

## Major remaining pre-Blender engineering

- full Unity compile-error pass after the large branch expansion
- broader projectile pooling / spawn pooling for high-density encounters
- additional status interactions such as shock chaining
- deeper named enemy archetype tuning/reactions
- encounter composition for all 18 acts
- mini-boss and Guardian mechanics refinement after Play Mode feedback
- production-quality player join/leave flow and reconnect UX hooks
- skill/perk implementation and mastery reward effects
- campaign death/retry/party-wipe handling
- additional secrets/challenge/setpiece composition
- boss/encounter debug spawning and automated validation tooling
- platform-specific provider adapter interfaces where SDK access permits
- performance/allocation audit
- final presentation socket/Animator/VFX/audio/haptic handoff audit
- final pre-Blender dependency matrix

## Stop boundary

Do not mark this branch complete until all meaningful gameplay/runtime work that can be done with primitives/generated assets is exhausted. Final 3D models, rigs, authored character animation, production materials/textures, final environment meshes, and asset-dependent polish belong to the Blender/art-production phase.
