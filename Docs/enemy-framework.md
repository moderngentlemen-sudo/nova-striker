# Enemy Framework — Phase 1

This phase begins the Unity enemy-runtime architecture without claiming that any of the 12 production enemy archetypes has been ported.

## Shared shell

`EnemyBrain2D` is the common runtime shell for standard enemies. It owns:

- actor identity,
- target reference,
- detection range,
- line-of-sight checks,
- shared Rigidbody2D movement helpers,
- damage / defeat observation,
- tactical-role selection.

It does **not** own player movement, weapon balance, art, animation, or final encounter logic.

## Tactical role modules

The common role vocabulary is:

- Anchor
- Artillery
- Flanker
- Skirmisher
- Aerial

Role behavior is composed through `EnemyRoleModule2D` components. A prefab can carry more than one role module and switch which role is active without replacing its health/collision/runtime shell.

Phase 1 implements:

- `EnemyAnchorModule2D` — stationary baseline role for validating the shell.
- `EnemySkirmisherModule2D` — first active representative role. It approaches when too far away, retreats when too close, maintains a mid-range spacing band, checks line of sight, and fires parryable enemy pulse projectiles.

Artillery, Flanker, and Aerial role modules remain future work.

## Mechanics-lab representative enemy

After regenerating the mechanics lab, the scene includes `Enemy_Skirmisher`.

Current greybox behavior:

- begins engaging the player inside its detection radius,
- moves toward / away from the player to maintain preferred range,
- uses World and OneWay geometry for line-of-sight occlusion,
- fires standard enemy projectiles only with clear line of sight,
- can be damaged, knocked back, defeated, thrown, and grappled through the shared combat interfaces.

This object is a **generic gameplay test enemy**, not a final Walker/Interceptor/etc. production archetype.

## Actor IDs and presentation cue correctness

`Damageable2D` now has its own receiver-side `actorId`.

`DamageTaken` and `Defeated` gameplay cues are emitted for the actor receiving the damage rather than for the source player. This prevents player presentation listeners from accidentally playing Hurt/Defeated reactions when the player damages an enemy.

Greybox assignments:

- Player 1: actor 0
- Target Light: actor 101
- Target Heavy: actor 102
- Enemy Skirmisher: actor 110

Production identity allocation can be formalized later when co-op, encounter spawning, and save/checkpoint systems are introduced.

## Validation

After pulling this phase:

1. Run **Nova Striker → Greybox → Build / Refresh Mechanics Lab**.
2. Enter Play Mode.
3. Verify `Enemy_Skirmisher` begins moving when the player is within detection range.
4. At long range, it should approach.
5. At very close range, it should back away.
6. In its preferred spacing band, it should settle instead of oscillating continuously.
7. Put a platform/wall between it and the player; it should stop firing through that geometry.
8. Move back into clear line of sight; firing should resume.
9. Confirm Nova can shoot/melee/Velocity Break/Powerslide the enemy.
10. Confirm Echo can enemy-grapple it only with unobstructed line of sight.
11. Defeat the enemy and confirm autonomous movement stops.
12. Confirm damaging the enemy does not trigger the player's Hurt/Defeated Animator cues.

## Next enemy-framework step

Once this representative shell is confirmed in Play Mode, the next engineering step is to add the Artillery and Flanker modules, then map the preserved browser enemy archetypes onto role + parameter combinations instead of creating 12 unrelated AI controllers.
