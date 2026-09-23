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
- Circle — character-specific contextual Counter
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
14. Aim upward toward any ordinary solid beam, wall edge, or OneWay surface above Echo and press Counter: Echo should soft-lock the struck surface and be pulled toward it.
15. Verify that the solid does **not** need a GrapplePoint component or special marker.
16. Put an enemy and overhead surface in similar range, then use right-stick/arrow aim to verify the intended candidate wins the soft-lock score.
17. Repeat close DodgeCounter and advancing Throw with Echo; those contexts still take priority over distance grappling.
18. Wall-jump away from a wall while still holding toward it; the initial horizontal launch should remain intact for roughly 0.10 s before air steering resumes.
19. Test Quick, Burst, and Velocity Break while watching the HUD's active Dash / Slide tier indicators.
20. Test all three grounded melee hits, two aerial hits, and downward dive melee.
21. Connect a DualShock/DualSense and repeat core traversal and Counter checks.

## After the first successful import

Commit Unity-generated `.meta` files and any safe ProjectSettings changes made by the Editor. Do not commit `Library/`, `Temp/`, `Logs/`, or other ignored generated directories.

Unity 6.6 compile/import, Play Mode launch, and corrected wall-slide behavior have been user-confirmed. Do not mark the greybox as QA-approved until the broader checklist and a physical controller test pass.
