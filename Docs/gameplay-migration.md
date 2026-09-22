# Nova Striker Gameplay Migration Plan

## Goal

Rebuild Nova Striker's gameplay in Unity with presentation decoupled from mechanics. The HTML preview remains the behavioral reference while Unity becomes the long-term runtime and graphics platform.

## Milestone 1 — Nova greybox controller

Implementation status on `dev/unity-gameplay`:

1. ✅ Run with acceleration/deceleration.
2. ✅ Independent movement, facing, and 360-degree aim model.
3. ✅ Jump and one air jump.
4. ✅ Crouch and one-way platform drop.
5. ✅ Wall contact, slide, wall jump, and regrab lockout.
6. ✅ Dash charge:
   - Quick
   - Burst
   - Velocity Break
7. ✅ Directional air dash.
8. ✅ Powerslide from crouch + dash.
9. ✅ Ground and aerial melee state/hit logic.
10. ✅ Character-specific Circle Counter:
   - Nova distance Deflect
   - Echo distance Grapple/pull
   - shared close Dodge + Counter
   - advancing Throw
   - Nova perfect projectile deflection.
11. ✅ Charged fire tiers and initial projectile patterns.

Still required before this milestone is considered validated:

- Unity editor compile/import
- greybox scene assembly
- Input System adapter
- physical controller testing
- tuning against the browser reference

## Milestone 2 — Combat core

- projectile ownership and cancellation
- damage / stagger / launch / knockback
- charge tiers
- parry reflection
- enemy armor / shields
- Break gauge
- Style meter
- hit-stop and gameplay-facing events for VFX

## Milestone 3 — Arsenal

Move the 12 weapons to ScriptableObjects. Gameplay code should reference data assets rather than hard-coded switch statements wherever practical.

## Milestone 4 — Enemy framework

Create a common enemy brain with role modules:

- Anchor
- Artillery
- Flanker
- Skirmisher
- Aerial

Then port the 12 enemy archetypes.

## Milestone 5 — Guardians

Port all six Guardian behavior trees separately:

- Aegis
- Cinder
- Mycel
- Rime
- Tempest
- Null

Each Guardian keeps its bespoke weak-point condition and multi-phase behavior.

## Milestone 6 — Co-op

- independent Player 1 / Player 2 input
- drop-in Echo
- revive
- independent loadouts
- Synergy meter and team attacks

## Architecture rules

- Avoid graphics decisions in movement/combat code.
- Emit gameplay events for animation, VFX, haptics, and audio.
- Prefer ScriptableObjects for tunable content.
- Preserve fixed gameplay timing independent of frame rate.
- Keep input routing per-player; never use one global active-controller state.
- Build mechanics in greybox before importing production art.
