# Pre-Blender Unity Validation Runbook

Branch: `dev/gameplay-complete`
Target editor: Unity `6000.6.2f1`

This runbook is the execution checklist for converting the current **code-integrated** gameplay branch into a genuinely **Unity-validated** pre-Blender baseline. Passing this checklist does not validate final production art, native console/store SDKs, certification, or shipped-platform performance.

## 1. Clean import / compile gate

1. Open `UnityProject` in Unity 6000.6.2f1.
2. Allow a clean import/assembly reload to finish.
3. Resolve every C# compile error before continuing.
4. Treat warnings as review items; do not silently reinterpret compile success as Play Mode success.
5. Confirm the Input System backend is active (`Input System Package (New)` or `Both`).

**Pass condition:** zero compile errors on the current `dev/gameplay-complete` head.

## 2. Rebuild and source/scene validation

Use:

`Nova Striker > Validation > Rebuild Mechanics Lab + Run Full Validation`

The command rebuilds generated greybox assets and then invokes, in order:

1. Gameplay Preflight
2. Structural Batch Validation
3. Asset + Presentation Contract Validation

The unified command is intentionally interactive-only until every validator exposes aggregate pass/fail state. For headless structural validation, the existing supported entry points remain:

- `NovaStriker.EditorTools.GameplayBatchValidator.RunForCommandLine`
- `NovaStriker.EditorTools.GameplayAssetContractValidator.RunForCommandLine`

**Pass condition:** no validator errors. Warnings must either be resolved or explicitly understood before the branch is called Unity-validated.

## 3. Local-player smoke matrix

Run the Mechanics Lab with the following player-count matrix. Use physical controllers for device-routing validation; keyboard remains a Player 1 fallback only.

| Players | Required checks |
| --- | --- |
| 1 | movement, crouch, powerslide, jump/double-jump, wall interactions, dash/Velocity Break, aim/fire/charge, melee/aerial melee, counter, Strike Suit ability, Guardian ability, weapon cycle |
| 2 | independent input ownership, camera framing, enemy targeting, Pair Sync, down/revive, join/leave |
| 3 | camera framing, Formation Sync, revive contribution, enemy scaling/target distribution, disconnect/reconnect |
| 4 | all four isolated inputs, Full Strike Sync, worst-case camera framing, revive contribution, encounter scaling, reconnect reservation, high-density combat |

For every count, verify that inactive/non-participating slots do not influence camera framing, target selection, encounter scaling, Sync requests, or party-wipe state.

**Pass condition:** no cross-control, duplicate actor IDs, lost player-slot ownership, stuck camera targets, false party wipes, or incorrect Sync tier/cost behavior.

## 4. Character identity checks

### Nova

Validate that Nova remains the ranged Sentinel/protector Striker and that production presentation is not required for gameplay authority:

- Deflect / perfect Deflect
- Bulwark Pulse
- Sentinel Lock
- Sentinel Screen
- Frontline Protocol hooks
- charged ranged combat and preventative spacing behavior

### Echo

Validate that Echo remains the aggressive Pursuit Protocol Striker:

- enemy grapple
- eligible-solid traversal grapple
- close Dodge Counter
- advancing Throw
- Pursuit Mark
- Reel Strike
- Staff Burst
- Pursuit Protocol hooks

**Pass condition:** Nova and Echo remain mechanically distinct, neither character depends on Animator/VFX timing for hit authority, and no production asset is required to execute the gameplay contract.

## 5. Combat/content coverage

Exercise each of the following at least once in Play Mode:

- all 12 weapon definitions and behavior families
- all 6 Guardian abilities
- all 6 baseline skill/perk effects
- all 12 named standard enemy archetypes
- all 4 mini-boss identities
- all 6 Guardian boss identities, phases, defense profiles, and weak-point windows
- shields, armor, Break/stagger, burn, shock chaining, cryo slow, mark/vulnerable/exposed
- projectile cancel/reflection and hostile perfect-opportunity projectiles
- health, Suit Energy, Synergy, and Ultimate pickups
- Gauntlet, Weapon Trial, and Dash Course secret challenges
- Train Rush, Furnace Surge, Vine Bridge, Ice Collapse, Lightning Chase, and Null Warp setpieces
- party wipe, checkpoint retry, and respawn

**Pass condition:** every code-complete gameplay family can be reached and exercised without missing-reference exceptions, soft locks, or invalid state carry-over.

## 6. Campaign / encounter matrix

Walk representative combat beats from all six sectors and confirm the authored 18-act composition remains scalable from one to four players. Specifically verify:

- base-count plus additional-player scaling
- encounter completion and reward spawning
- arena camera lock/unlock
- hazards and setpieces do not strand players outside the shared field
- mini-boss endpoints on non-final acts
- Guardian endpoints on final acts
- pooled enemies fully reset health, combat state, rigidbody state, archetype state, and AI state on reuse

**Pass condition:** no act requires production geometry or presentation assets to preserve gameplay progression.

## 7. Save / progression / commerce / DLC checks

Using the editor/offline providers:

- earn and persist weapon mastery
- unlock/persist skill perks
- save/load campaign progression and checkpoint state
- persist per-player cosmetic loadouts
- purchase/reconcile/restore mock entitlements
- confirm DLC content gates respond only to entitlement state
- confirm gameplay-stat purchase flags remain exceptional and separable from cosmetic/content commerce
- restart Play Mode and re-check persistence

**Pass condition:** the gameplay layer remains storefront-neutral and no platform SDK is required for editor validation.

## 8. Performance / scalability matrix

The Mechanics Lab now auto-installs `GameplayPerformanceProbe` in Editor/Development Build sessions. Its rolling frame-time display and pool counts are triage signals only; Unity Profiler captures remain authoritative.

Capture representative traces for:

1. Low tier — one player / normal encounter
2. Low tier — four players / high-density encounter
3. Medium tier — four players / high-density encounter
4. High tier — four players / high-density encounter
5. Ultra tier — four players / high-density encounter where hardware supports the 120 fps target

Review at minimum:

- main-thread frame time
- scripts / physics cost
- GC allocations per frame
- projectile creation/reuse behavior
- encounter-enemy creation/reuse behavior
- camera and targeting cost at four players
- VFX/light/shadow budgets when presentation assets are later connected

Do not solve presentation bottlenecks by changing combat timing, enemy counts, damage logic, cooldowns, or other gameplay authority unless profiling proves the gameplay system itself is the bottleneck.

**Pass condition:** representative greybox gameplay is stable enough that remaining optimization depends on production presentation data or target-platform hardware profiling.

## 9. Frozen production contract check

Validate production integration against `GameplayPresentationContract` version `1.0.0-preblender`:

- Animator parameter names
- generic rig/socket names
- gameplay event IDs
- VFX event IDs
- audio event IDs
- semantic haptic routes

Production assets consume these identifiers. They do not own hit timing, movement timing, damage, cooldowns, encounter progression, or save/commerce authority.

## 10. Pre-Blender stop condition

The gameplay branch may be declared **pre-Blender gameplay complete and Unity-validated** only after Sections 1–9 pass or have an explicit documented waiver.

At that point, stop adding gameplay code merely to simulate missing production work. The remaining meaningful work should move to:

- Nova/Echo/Tank/Support production models
- Strike Suit topology/materials/textures/emissives
- enemy and Guardian models
- weapons/props
- skeletons, skinning, facial rigs, authored animation
- production environment kits and set dressing
- final VFX, audio, music, lighting, haptics, cinematics
- native Steam/Xbox/PlayStation/Nintendo/mobile provider adapters where licensed SDKs and credentials are available

Until the Unity run above is actually performed, the correct status remains **code-integrated, not freshly Unity-validated**.
