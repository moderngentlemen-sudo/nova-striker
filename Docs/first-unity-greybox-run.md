# First Unity Greybox Run

The `dev/unity-gameplay` branch now contains an openable Unity project under `UnityProject/`.

## Baseline

- Unity target: **6.6**
- Recorded editor: `6000.6.2f1`
- Input package: `com.unity.inputsystem` `1.20.0`
- Physics step in the mechanics lab: 60 Hz

Nova Striker now targets Unity 6.6. The repository records `6000.6.2f1`; if you already have another compatible 6000.6.x patch installed, you can open the project with that version and allow Unity to update `ProjectVersion.txt` rather than installing 6.3.

## Open

In Unity Hub, choose **Add project from disk** and select:

`<repo>/UnityProject`

Package Manager will resolve the Input System and built-in physics modules.

## Input backend

The project uses the Unity Input System adapter for the greybox.

If Unity prompts to enable the new input backend, accept and restart.

If it does not prompt:

1. Edit → Project Settings → Player
2. Other Settings → Configuration
3. Active Input Handling
4. Choose **Input System Package (New)** or **Both**
5. Restart Unity if requested

The greybox builder also detects when the backend is inactive and opens Player Settings.

## Generate the mechanics lab

After compilation:

**Nova Striker → Greybox → Build / Refresh Mechanics Lab**

The command generates:

- `Assets/Greybox/Generated/NovaGreybox.prefab`
- `Assets/Greybox/Generated/ProjectileGreybox.prefab`
- `Assets/Greybox/Generated/Weapon_Pulse.asset`
- greybox materials
- `Assets/Greybox/Scenes/NovaMechanicsGreybox.unity`

The scene contains:

- Nova Player 1
- floor and containment walls
- three one-way platforms
- three ordinary overhead traversal beams on the normal World layer
- a light damage dummy
- a heavy damage dummy
- a hostile projectile emitter
- periodic perfect-parry opportunities
- orthographic camera
- mechanics status HUD

## Controls

Keyboard:

- WASD — move
- Arrow keys — aim
- Space — jump
- J — fire / charge
- K — dash / charge
- U — melee
- I — contextual Counter
- F — Guardian ability placeholder
- Q — weapon cycle placeholder
- R — Guardian cycle placeholder
- G — Sync placeholder

Gamepad / PlayStation:

- Left stick — move / crouch
- Right stick — aim
- Cross — jump
- R2 — fire / charge
- L2 — dash / charge
- Square — melee
- Circle — character-specific contextual Counter / Echo grapple
- L1 — ability
- R1 or Triangle — weapon cycle
- L3 or D-pad Up — Guardian cycle
- R3 — Sync

## First validation checklist

Run these before tuning values:

1. Walk, stop, reverse direction, and verify independent right-stick aim.
2. Jump, air jump, wall slide, wall jump, and wall regrab.
3. Hold down/crouch and press jump while standing on a cyan one-way platform.
4. Test Quick, Burst, and Velocity Break dash charge thresholds.
5. Crouch + dash at all three tiers and verify Powerslide damage.
6. Fire uncharged and all three charge tiers at both dummies.
7. Confirm charged shots cancel hostile shots.
8. With the greybox player set to **Nova**, press Counter at distance/no close enemy: it should select **Deflect**.
9. Perfect-deflect the periodic highlighted projectile by timing the 0.035–0.078 s perfect window.
10. Stand within 1.15 units of a dummy without moving toward it and press Counter: it should select **DodgeCounter**.
11. Hold movement toward a dummy inside 1.35 units and press Counter: it should select **Throw**.
12. In the Nova player's `NovaCombatController`, temporarily change **Character** from Nova to Echo.
13. With Echo selected and an enemy at distance, aim toward the enemy and press Counter: it should soft-lock the enemy and pull it toward Echo.
14. With Echo at substantial distance from an enemy—up to roughly **14 Unity units**—press **Circle / I without holding Up**: Echo should acquire the opponent and pull it toward him only when there is a clear line of sight.
15. Hold **Up + Circle / W + I** while under an ordinary solid beam or OneWay surface within roughly **8.5 Unity units**: Echo should grapple the overhead surface and travel toward it.
16. Repeat with **Up-Left + Circle** and **Up-Right + Circle** to verify the traversal direction selects the corresponding overhead surface.
17. Verify that ordinary solid traversal geometry does **not** need a GrapplePoint component or special marker.
18. Put an enemy and an overhead surface in similar range: without Up the enemy should be chosen; with Up/Up-diagonal input the traversal surface should be chosen.
19. Put a World or OneWay platform between Echo and an enemy, then press Circle / I without Up. Echo should **not** lock or pull that enemy through the platform. Moving to a clear line of sight should make the same enemy grappleable again.
20. Repeat close DodgeCounter and advancing Throw with Echo; those contexts still take priority over distance grappling.
21. Re-test wall jumps from both sides and compare the feel against the earlier behavior now that the temporary steering lock has been removed.
22. Test Quick, Burst, and Velocity Break while watching the HUD's active Dash / Slide tier indicators.
23. Test all three grounded melee hits, two aerial hits, and downward dive melee.
24. Connect a DualShock/DualSense and repeat core traversal and Counter checks.

## Greybox presentation validation

After pulling this revision, run **Nova Striker → Greybox → Build / Refresh Mechanics Lab** once so the regenerated player prefab receives the new `GreyboxMechanicsVisualizer`.

In Play Mode:

- Quick, Burst, and Velocity Break should leave progressively stronger movement trails.
- Powerslide uses the same tier visualization while preserving the crouched collision state.
- Echo enemy grapple should briefly draw a tether to the acquired opponent.
- Echo Up / Up-diagonal traversal grapple should draw the tether to the selected solid surface and keep the movement trail active during traversal.
- These effects are diagnostic only; disabling or deleting `GreyboxMechanicsVisualizer` must not change gameplay behavior.

## Dash / Counter telemetry pass

The greybox HUD now exposes additional runtime diagnostics for this tuning pass:

- live Rigidbody velocity
- current dash charge time
- the tier that the current dash charge will release into
- active Dash and Powerslide tiers
- Echo grapple lock type and lock distance
- whether Up + Counter traversal intent is active
- the last player gameplay cue, including tier, value, and action ID where available

Use the HUD to validate the dash boundaries directly:

- release before **0.30 s** → Quick
- release from **0.30–0.85 s** → Burst
- release at **0.85 s or later** → Velocity Break

For Powerslide, hold Down first so Crouch is active, then charge/release Dash. Confirm the HUD changes from `Dash` to `Slide` and reports the intended tier. `SlideHit` should appear as the last cue when the slide damages a target; `DashHit` should appear only for Velocity Break contact damage.

For Counter validation, use the HUD's lock type and distance together with the last cue:

- Nova distance Counter → `CounterDeflect` when a projectile is successfully reflected
- Echo distance Counter without Up → `CounterGrapple` / `echo-grapple-enemy`
- Echo Up or Up-diagonal + Counter → `CounterGrappleTraversal`
- close enemy → `CounterDodge`
- moving toward a close enemy → `CounterThrow`

## After the first successful import

Commit Unity-generated `.meta` files and any safe ProjectSettings changes made by the Editor. Do not commit `Library/`, `Temp/`, `Logs/`, or other ignored generated directories.

Unity 6.6 compile/import, Play Mode launch, and corrected wall-slide behavior have been user-confirmed. Do not mark the greybox as QA-approved until the broader checklist and a physical controller test pass.
