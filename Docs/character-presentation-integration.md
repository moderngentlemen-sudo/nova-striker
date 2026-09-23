# Character Presentation Integration

This phase establishes the Unity-side handoff contract for Nova and Echo production art without changing authoritative gameplay.

## Runtime hierarchy

The generated player prefab now follows this structure:

```
Nova_Greybox
├── gameplay / physics components
├── StrikerPresentationBridge
├── NovaVisualRoot
│   └── Nova production model / rig / Animator
├── EchoVisualRoot
│   └── Echo production model / rig / Animator
└── MuzzleSocket
```

The current mechanics lab uses simple placeholder capsules under the two visual roots. They are not production character designs.

Changing `NovaCombatController.Character` between Nova and Echo causes the presentation bridge to enable the matching visual root and disable the other one. Gameplay collision, movement, combat, and Counter logic remain on the player root and do not move into the art prefab.

## Production model handoff

When a Blender character export is ready:

1. Export the rigged FBX to the appropriate Unity art folder.
2. Create or update a Unity character-art prefab containing the imported model, materials, rig, and Animator.
3. Parent or instantiate that art prefab under `NovaVisualRoot` or `EchoVisualRoot`.
4. Remove or disable the corresponding placeholder capsule.
5. Assign the character Animator to `StrikerPresentationBridge` if it is not automatically found under the visual root.
6. Keep the gameplay `Rigidbody2D`, `CapsuleCollider2D`, `NovaMotor2D`, `NovaCombatController`, and damage systems on the player root.

Production art must not become the authoritative source for gameplay collision or hit timing.

## Continuous Animator parameter contract

The bridge checks the active Animator for each parameter before setting it, so incomplete Animator Controllers do not need to implement the full contract immediately.

Supported continuous parameters:

| Parameter | Type | Meaning |
|---|---|---|
| `Speed` | Float | Absolute horizontal speed |
| `VelocityX` | Float | World-space horizontal velocity |
| `VelocityY` | Float | World-space vertical velocity |
| `AimX` | Float | Current normalized aim X |
| `AimY` | Float | Current normalized aim Y |
| `Facing` | Float | -1 left, +1 right |
| `Grounded` | Bool | Ground contact |
| `Crouching` | Bool | Crouched state |
| `WallSliding` | Bool | Active wall slide |
| `Dashing` | Bool | Active normal dash |
| `Sliding` | Bool | Active Powerslide |
| `GrappleTraversal` | Bool | Echo traversal-grapple movement |
| `Countering` | Bool | Counter action is active |
| `FireCharge` | Float | Current fire-charge seconds |
| `DashTier` | Int | Quick=1, Burst=2, Velocity Break=3 |
| `MeleeStep` | Int | Current melee sequence step |
| `CounterMode` | Int | Deflect=0, Grapple=1, DodgeCounter=2, Throw=3 |
| `Character` | Int | Nova=0, Echo=1 |

## Cue-driven Animator triggers

The bridge also translates gameplay cues into optional Animator triggers:

| Trigger | Gameplay source |
|---|---|
| `Jump` | Jump |
| `WallJump` | WallJump |
| `Dash` | DashStarted |
| `Slide` | SlideStarted |
| `FireChargeStart` | FireChargeStarted |
| `FireRelease` | FireChargeReleased |
| `Melee` | MeleeStarted |
| `Counter` | CounterStarted |
| `Deflect` | CounterDeflect |
| `PerfectDeflect` | PerfectParry |
| `Grapple` | CounterGrapple |
| `GrappleTraverse` | CounterGrappleTraversal |
| `DodgeCounter` | CounterDodge |
| `Throw` | CounterThrow |
| `Hurt` | DamageTaken |
| `Defeated` | Defeated |

Animation clips may visually anticipate or follow through an action, but they must not redefine gameplay-active windows.

## Facing

The bridge can rotate the active visual root around Y to represent left/right facing. Default values are:

- facing right: Y = 0°
- facing left: Y = 180°

These values are presentation settings and can be adjusted per imported character orientation without changing movement code.

## Greybox validation

After pulling this phase, regenerate the mechanics lab with:

**Nova Striker → Greybox → Build / Refresh Mechanics Lab**

Then:

1. Start as Nova and confirm the blue placeholder is active.
2. Change `NovaCombatController.Character` to Echo and confirm the Echo placeholder becomes active.
3. Verify movement, wallslide, dash, Powerslide, Counter, firing, and grapple behavior are unchanged.
4. Change back to Nova and confirm the visual swaps back without changing gameplay state.
5. If an Animator is added, verify only parameters actually present in that controller are driven.

## Current production status

This bridge is Unity presentation scaffolding only. It does not mean either Nova or Echo has a finished Blender model, rig, animation set, or production Unity prefab.
