# Unity Greybox Gameplay Implementation

This document describes the current playable-mechanics code on `dev/unity-gameplay` and the Unity scene setup required to exercise it.

## First run

1. Open Unity **6.6**. The project currently records editor version `6000.6.2f1`.
2. Open the repository's `UnityProject/` folder in Unity Hub.
3. Allow Package Manager to resolve `com.unity.inputsystem`.
4. If Unity asks to activate the new Input System backend, accept and let the Editor restart. If no prompt appears, use **Project Settings → Player → Active Input Handling** and select **Input System Package (New)** or **Both**.
5. After scripts compile, run:
   `Nova Striker → Greybox → Build / Refresh Mechanics Lab`
6. Open the generated scene if it is not already open:
   `Assets/Greybox/Scenes/NovaMechanicsGreybox.unity`
7. Press Play.

The builder creates six project layers used by the mechanics lab: World, OneWay, Player, Enemy, PlayerProjectile, and EnemyProjectile.

The generated assets are disposable test assets. Source mechanics remain under `Assets/Scripts`.

## Current implementation

The branch now contains:

- `NovaMotor2D`
  - run acceleration/deceleration
  - crouch movement
  - jump + one air jump
  - wall probe / wall slide / wall jump
  - wall regrab lockout
  - charged multidirectional dash
  - Quick / Burst / Velocity Break tiers
  - Powerslide with tier-specific speed and duration
  - one-way platform dropping using crouch + jump
  - per-player movement cues for animation/VFX/audio

- `NovaCombatController`
  - browser-reference charge thresholds: 0.40 / 0.92 / 1.58 seconds
  - uncharged through Tier-3 fire
  - initial spread / cyclone / Tier-3 beam shot patterns
  - three-step grounded melee sequence
  - two-step aerial melee sequence
  - downward dive melee
  - browser-reference melee active window
  - Circle / I contextual Counter system
  - close Reversal counter within 1.15 Unity units
  - mid-range Intercept counter within 2.40 Unity units
  - far/no-target Deflect counter
  - all counter modes preserve the projectile deflect window
  - perfect-deflect timing and projectile reflection
  - damage/knockback handoff

- `NovaTraversalDamage`
  - Velocity Break contact damage
  - Powerslide contact damage
  - one-hit-per-target traversal windows

- `Projectile2D`
  - per-player ownership
  - Player / Enemy faction routing
  - charged friendly shots cancel hostile projectiles and continue, matching the browser reference
  - parry reflection
  - perfect-opportunity reflection damage
  - piercing support

- `Damageable2D`
  - neutral/player/enemy faction checks
  - health
  - knockback
  - defeat event
  - gameplay cue output

- `GameplayEventHub`
  - presentation-independent gameplay signals
  - intended consumers: Animator, VFX, camera, audio, haptics, UI

- `NovaPlayerGameplay`
  - one input snapshot fans out to movement and combat
  - latches edge inputs safely across render/physics rates
  - keeps future controller/keyboard/touch adapters outside the mechanics code

- `NovaInputSystemAdapter`
  - Unity Input System keyboard + generic gamepad adapter
  - bindings cover DualShock/DualSense through standard Gamepad controls
  - R1 and Triangle both cycle weapons
  - L3 and D-pad Up both cycle Guardians
  - current greybox is single-player input routing; per-device pairing for co-op remains later work

- `NovaKeyboardDebugInput`
  - retained as a legacy development fallback but not attached by the current greybox builder

- `GreyboxHostileProjectileEmitter`
  - temporary hostile-shot generator for parry/perfect-parry testing

## Reference timings carried forward

### Dash charge

- Quick: under 0.30 s
- Burst: 0.30–0.85 s
- Velocity Break: 0.85 s+

### Fire charge

- Tier 0: under 0.40 s
- Tier 1: 0.40–0.92 s
- Tier 2: 0.92–1.58 s
- Tier 3: 1.58 s+

### Character-specific Context Counter

Circle / I is now a **character-specific Counter command** rather than one universal parry.

Selection priority when the button is pressed:

1. **Moving toward a nearby enemy + Counter → Throw** (Nova and Echo)
2. **Close enemy + Counter → Dodge + Counter** (Nova and Echo)
3. **Distance action**
   - **Nova → Deflect**
   - **Echo → Grapple / pull**

Current greybox ranges:

- close Dodge Counter: ≤ 1.15 Unity units
- Throw assist range: ≤ 1.35 Unity units while movement points toward the target
- Echo Grapple range: ≤ 4.25 Unity units
- Nova Deflect projectile radius: 0.72 Unity units

Close Dodge Counter grants a short evasive movement and temporary damage invulnerability, then strikes the target.

Throw is an advancing context action: holding movement toward the enemy when Counter is pressed takes priority over the close Dodge Counter and launches the enemy away/upward.

Echo's distance Grapple now uses **soft-lock acquisition**. On the same Circle / I press, Echo chooses the best valid grapple candidate based primarily on aim alignment and secondarily on distance.

Valid Echo grapple candidates are:

- **Enemy targets:** soft-lock and pull the enemy toward Echo.
- **Traversal GrapplePoints:** soft-lock and pull Echo toward the anchor for movement through the level.

Current greybox ranges:

- enemy grapple: ≤ 4.25 Unity units
- traversal GrapplePoint acquisition: ≤ 5.75 Unity units

Right-stick / arrow-key aim biases which candidate is selected. Without explicit aim, Echo uses his current facing/aim direction.

A `CounterGrappleLock` cue is emitted as soon as a candidate is acquired so the final UI can display a lock reticle/tether preview before the grapple resolves. The current greybox executes the grapple on the same button press after the Counter startup window; it does not require a second Circle press.

If no valid enemy or GrapplePoint is available, Echo still performs a grapple-whiff action cue so animation/VFX can play.

Nova's distance Deflect retains the defensive projectile-reflection timing:

- startup: 0.035 s
- deflect active: 0.035–0.145 s
- perfect deflect: 0.035–0.078 s
- action ends: 0.405 s

The close and Throw actions deliberately emit character-qualified action IDs such as `nova-dodge-counter`, `echo-dodge-counter`, `nova-throw`, and `echo-throw`. This lets Nova and Echo use completely different animations, poses, VFX, and later tuning even when the contextual category is shared.

### Melee

Ground:
- three-hit sequence
- base duration: 0.20 s
- combo reset: 0.38 s

Air:
- two-hit sequence
- base duration: 0.18 s
- dive duration: 0.22 s

Browser-reference hit-active window:
- remaining timer below 0.150 s
- remaining timer above 0.045 s

## Coordinate convention

Unity gameplay uses normal Unity world-space axes:

- +X = right
- +Y = up
- crouch/down input therefore uses negative Y

This differs from the browser canvas coordinate system, where screen Y increased downward.

## Required Unity layers

Create at least:

- `World`
- `OneWay`
- `Player`
- `Enemy`
- `PlayerProjectile`
- `EnemyProjectile`
- `GrapplePoint`

The exact layer numbers are not authoritative; the serialized LayerMasks on components are.

## Nova greybox GameObject

Recommended component stack:

```
Nova
├── Rigidbody2D
├── CapsuleCollider2D
├── NovaPlayerGameplay
├── NovaMotor2D
├── NovaCombatController
├── NovaTraversalDamage
├── NovaKeyboardDebugInput   (development only)
└── MuzzleSocket
```

Recommended Rigidbody2D setup:

- Body Type: Dynamic
- Freeze Rotation Z: on
- Collision Detection: Continuous for high-speed traversal testing
- Interpolate: Interpolate
- Gravity Scale: tune against the browser reference

Assign:

- `NovaMotor2D.worldMask` → World
- `NovaMotor2D.oneWayMask` → OneWay
- `NovaCombatController.damageableMask` → Enemy hurtbox layers
- `NovaCombatController.projectileMask` → EnemyProjectile
- `NovaCombatController.muzzleSocket` → child transform
- `NovaCombatController.projectilePrefab` → greybox projectile prefab
- `NovaCombatController.equippedWeapon` → a WeaponDefinition asset

## One-way platform setup

For the current implementation:

1. put the platform collider on the `OneWay` layer,
2. include OneWay in the motor's `oneWayMask`,
3. do not include OneWay in `worldMask`,
4. crouch/down + jump temporarily ignores the supporting one-way collider for 0.24 s.

A PlatformEffector2D may be used for normal one-way behavior, but the drop-through mechanic itself is driven by the player/platform collision-ignore window.

## Projectile prefab

Recommended:

- Rigidbody2D
  - Gravity Scale: 0
  - Collision Detection: Continuous
- CircleCollider2D
  - Is Trigger: on
- Projectile2D

The combat controller sizes the projectile object by charge tier. The prefab should therefore use a normalized base visual/collider size.

## Damage test dummy

Add:

- collider
- optional Rigidbody2D
- `Damageable2D`
- faction: Enemy

This is enough to validate shooting, melee, knockback, and defeat without enemy AI.

## Presentation integration

Do not put gameplay timing into animation clips.

Presentation systems should subscribe to `GameplayEventHub.CueRaised`. Examples:

- `DashStarted` → Animator trigger + VFX burst + haptics
- `ProjectileFired` → muzzle flash + recoil animation + audio
- `MeleeStarted` → animation selection
- `MeleeHit` → hit spark + camera impulse
- `CounterStarted` → choose character-specific Counter animation
- `CounterDodge` → Nova/Echo-specific close dodge-counter animation + camera impulse
- `CounterThrow` → character-specific throw animation
- `CounterGrappleLock` → show Echo target-lock reticle / tether preview
- `CounterGrapple` → Echo enemy grapple line/arm/weapon presentation and enemy pull feedback
- `CounterGrappleTraversal` → Echo-to-anchor tether, traversal camera/VFX, and arrival feedback
- `CounterDeflect` → Nova projectile-deflection presentation
- `PerfectParry` → gold-white perfect-deflect VFX + hit-stop presentation + audio
- `DropThrough` → crouch/drop animation

The gameplay code remains authoritative even if all presentation listeners are disabled.

## Known incomplete items

This branch now contains an openable Unity project baseline and a one-click mechanics-lab generator, but it has **not yet been compiled or played in a Unity Editor session from this chat**.

Still required:

- first Unity Editor import/package resolution
- generation and commit of Unity-created `.meta` files after the initial import
- execution of the greybox builder inside Unity
- compiler-error review after the first real import
- Play Mode validation
- physical DualShock/DualSense validation
- enemy attack-state awareness so close Dodge Counters can eventually require/grade actual incoming melee or rush attacks rather than relying only on proximity
- final Input System gamepad/touch adapters
- weapon-specific behaviors beyond initial shot patterns
- shields / armor / Break gauge
- Style meter
- Guardian abilities
- co-op join/revive/Sync
- checkpoints/save migration
- enemy AI and all Guardian behavior trees

No Unity compile or physical-controller test should be claimed until those are actually run.
