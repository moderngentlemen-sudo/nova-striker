# Pre-Blender Gameplay Boundary

Branch: `dev/gameplay-complete`

This document defines the point at which Nova Striker gameplay engineering is considered code-complete enough to hand off into production modeling, rigging, animation, environment art, VFX, audio, and platform-specific SDK integration without allowing presentation assets to become gameplay authority.

## Code-complete systems

### Four-player architecture

- one-to-four simultaneous local Striker slots
- canonical Strike Team roles: Tank, Striker 0, Striker 1, Support
- Striker 1 team-lead and Striker 0 second-in-command rules
- isolated controller assignment and reconnect reservation
- Player 1 keyboard support
- mobile virtual-input bridge
- one-screen multi-player camera framing
- player/player collision suppression
- four-player target acquisition, downed-state retention, revive contribution, Team Sync, and assist-chain systems
- art-independent Tank/Support gameplay contracts without inventing final characters or production abilities

### Nova and Echo

- Nova remains the ranged Sentinel/protector Striker with Deflect, Bulwark Pulse, Sentinel Lock, Sentinel Screen, and Frontline Protocol hooks
- Echo remains the aggressive Pursuit Protocol Striker with enemy grapple, traversal grapple, close counter, advancing throw, Pursuit Mark, Reel Strike, Staff Burst, and Pursuit Protocol hooks
- gameplay timing and hit authority remain independent of animation, VFX, and final meshes

### Combat and progression

- health, shields, armor, Break/stagger, healing, revive, invulnerability, knockback
- burn, shock chaining, cryo slow, marks, vulnerable/exposed states
- projectile cancellation/reflection, charged fire, melee/aerial/traversal damage
- per-player style meter
- twelve weapon definitions and mastery progression
- six Guardian abilities
- six baseline skill/perk unlocks with runtime effects
- shared Team Sync and cooperative assist-chain rewards

### Enemies, encounters, campaign, and bosses

- twelve named standard enemy archetypes
- shared enemy role modules and squad coordination
- eighteen campaign act gameplay compositions across six sectors
- eighteen-act level-variety matrix spanning topology, traversal emphasis, objective style, route choice, hazard rhythm, and climax
- modular level-variation bindings that let greybox and production geometry consume stable gameplay tags
- reusable act-objective authority and Zone/Node/Goal triggers with solo-safe sequential completion and optional co-op parallelism
- scalable one-to-four-player encounter waves
- enemy and projectile pooling foundations
- pickups, encounter rewards, secret challenges, hazards, and setpieces
- four mini-boss identities
- six Guardian boss identities with phase and weak-point systems
- checkpoint, party-wipe, retry, and campaign progression authority
- debug spawning and editor validation tooling

### Platform, save, commerce, and DLC architecture

- fixed 60 Hz gameplay simulation independent from render target
- scalable render/presentation quality budgets
- storefront-neutral platform runtime provider interface
- replaceable local/cloud save backend contract
- storefront-neutral commerce provider and reconciliation interfaces
- persistent entitlements and cosmetic loadouts
- entitlement-driven DLC gates
- gameplay-stat purchases remain explicitly separable from cosmetic/content commerce

### Production-facing presentation contract

- `GameplayPresentationContract` version `1.0.0-preblender` freezes gameplay-facing Animator parameter names
- generic production rig/socket names are defined centrally for camera focus, head/chest/back, hands, weapon/muzzle, ability origin, and feet
- every `GameplayCueType` receives stable gameplay/VFX/audio event-id namespaces
- semantic haptic routes are defined for movement, impact, counter, ability, Team Sync, boss, damage/break, defeat, and revive events
- production assets consume these identifiers; they do not own hit timing, damage, movement authority, cooldowns, or encounter progression

## Validation state

The branch has reached an **initial Unity 6000.6.2f1 compile and Play Mode milestone**, including a user-confirmed playable Mechanics Lab after live compatibility reconciliation. It is not yet comprehensively Unity-validated. Full aggregate validation, 1–4 player coverage, persistence/commerce checks, level-variety scene validation, and profiling remain.

The source-side pre-Blender gate now includes four editor validation layers:

- `Nova Striker/Validation/Run Gameplay Preflight` for scene/configuration checks
- `Nova Striker/Validation/Run Structural Batch Validation` for scene-independent campaign, enemy, Strike Team, and skill/perk contract checks
- `Nova Striker/Validation/Run Asset + Presentation Contract Validation` for weapon/Guardian/generated-asset integrity, commerce definitions, presentation identifiers, gameplay-cue routing, and scalability-tier contracts
- `Nova Striker/Validation/Validate Generated Level Variety Labs` for all eight generated topology scenes, including four-player/session/camera/objective/hazard/navigation runtime structure

The aggregate headless gate is `NovaStriker.EditorTools.PreBlenderValidationSuite.RebuildAndRunForCommandLine`. It rebuilds the Mechanics Lab and all eight topology labs, then runs all source/scene validators and exits non-zero on failure. The structural, asset/presentation, and level-variety validators also expose narrower command-line entry points. Generated greybox assets are part of this final source-side gate, but a passing result still does not substitute for Play Mode or profiler evidence.

## Remaining meaningful pre-Blender work

These tasks do **not** require production Blender assets and should be completed or explicitly waived before declaring the gameplay branch fully validated:

1. Fresh Unity 6.6 compile/error reconciliation for the current branch.
2. Rebuild the generated Mechanics Lab and run all three validation commands.
3. Play Mode smoke test with one, two, three, and four local players.
4. Exercise controller disconnect/reconnect and join/leave behavior.
5. Exercise all twelve weapons, six Guardian abilities, six skill/perk effects, named enemies, mini-bosses, Guardians, pickups, challenge types, setpieces, party wipe, checkpoint retry, save/load, entitlement restore, and DLC gates.
6. Realize representative greybox layouts from the 18-act level-variety matrix and verify that route choice, objective triggers, hazard staggering, and optional co-op splits remain solo-safe and shared-camera compatible.
7. Run profiler/allocation captures on representative low/high density encounters and tune pool capacities or presentation budgets if needed.
8. Connect native Steam/Xbox/PlayStation/Nintendo/mobile providers only where their SDKs and credentials are available; no gameplay code should depend directly on those SDKs.
9. Validate production Animator Controllers, VFX/audio event tables, and haptic adapters against the frozen `GameplayPresentationContract`. The identifier/socket design itself is now code-complete; production asset implementation is not.

## Blender / production-asset dependency boundary

The following work should not be simulated with additional gameplay code once the validation list above passes:

- final Nova and Echo models
- Strike Suit topology, materials, textures, and emissive treatment
- production Tank/Support character designs and meshes
- skeletons, skinning, facial rigs, authored animation, and animation polish
- final enemy and Guardian meshes
- final weapons and props
- production environment meshes, modular kits, collision-derived art replacement, and authored set dressing
- final VFX, lighting, particles, audio, music, voice, and haptics polish
- production cinematics and presentation-specific transitions

Once the aggregate source gate and the remaining empirical Unity checks pass, the correct next step is production asset creation and Unity presentation integration, not expansion of the gameplay authority layer. Until new Play Mode evidence reveals a concrete defect, further gameplay-code expansion should be treated as out of scope.
