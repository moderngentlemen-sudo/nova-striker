# Unity Greybox Gameplay Implementation

This document describes the current playable-mechanics code on `dev/unity-gameplay` and the Unity scene setup required to exercise it.

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
  - parry startup/active/recovery state
  - perfect-parry timing
  - projectile reflection
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

- `NovaKeyboardDebugInput`
  - development-only keyboard adapter matching the browser controls
  - provides an immediate mechanics test path before the final Input System action map

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

### Parry

- startup: 0.035 s
- active: 0.035–0.145 s
- perfect: 0.035–0.078 s
- action ends: 0.405 s

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
- `PerfectParry` → gold-white VFX + hit stop presentation + audio
- `DropThrough` → crouch/drop animation

The gameplay code remains authoritative even if all presentation listeners are disabled.

## Known incomplete items

This branch is not yet a complete Unity build.

Still required:

- Unity project generated/opened in a chosen Unity 6 editor version
- committed Unity-generated `.meta` files
- Input System adapter and action asset
- physical DualShock/DualSense validation
- contact-enemy parry
- final Input System gamepad/touch adapters
- weapon-specific behaviors beyond initial shot patterns
- shields / armor / Break gauge
- Style meter
- Guardian abilities
- co-op join/revive/Sync
- checkpoints/save migration
- enemy AI and all Guardian behavior trees

No Unity compile or physical-controller test should be claimed until those are actually run.
