# Enemy Framework — Phase 3

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

Implemented role modules:

- `EnemyAnchorModule2D` — stationary baseline role for validating the shell.
- `EnemySkirmisherModule2D` — approaches when too far away, retreats when too close, maintains a mid-range spacing band, checks line of sight, and fires parryable pulse shots.
- `EnemyArtilleryModule2D` — prefers a long-range firing band, retreats when pressured, and fires a slow, readable three-shot parryable salvo.
- `EnemyFlankerModule2D` — closes aggressively, uses short lateral reposition bursts, retreats when overcrowded, and fires faster close-to-mid-range parryable shots.
- `EnemyAerialModule2D` — maintains an offset above the player and supports two browser-derived motion families: `HoverBob` for Drone-like behavior and `Orbit` for Orbiter-like behavior.

## Mechanics-lab representative enemy

After regenerating the mechanics lab, the scene includes five generic framework-validation enemies:

- `Enemy_Skirmisher` — mid-range spacing behavior.
- `Enemy_Flanker` — faster pressure/reposition behavior.
- `Enemy_Artillery` — long-range salvo behavior.
- `Enemy_Aerial_HoverTest` — Drone-like hover/bob validation.
- `Enemy_Aerial_OrbitTest` — Orbiter-like circular-motion validation.

All three:

- begin engaging the player inside their configured detection radius,
- use World and OneWay geometry for line-of-sight occlusion,
- fire only with clear line of sight,
- can be damaged, knocked back, defeated, thrown, and grappled through the shared combat interfaces.

These are **generic gameplay test enemies**, not final Walker/Interceptor/etc. production archetypes.

## Actor IDs and presentation cue correctness

`Damageable2D` now has its own receiver-side `actorId`.

`DamageTaken` and `Defeated` gameplay cues are emitted for the actor receiving the damage rather than for the source player. This prevents player presentation listeners from accidentally playing Hurt/Defeated reactions when the player damages an enemy.

Greybox assignments:

- Player 1: actor 0
- Target Light: actor 101
- Target Heavy: actor 102
- Enemy Skirmisher: actor 110
- Enemy Flanker: actor 111
- Enemy Artillery: actor 112
- Enemy Aerial Hover Test: actor 113
- Enemy Aerial Orbit Test: actor 114

Production identity allocation can be formalized later when co-op, encounter spawning, and save/checkpoint systems are introduced.

## Validation

After pulling this phase:

1. Run **Nova Striker → Greybox → Build / Refresh Mechanics Lab**.
2. Enter Play Mode.
3. Verify `Enemy_Skirmisher` approaches at long range, retreats at very close range, and settles into a mid-range spacing band.
4. Verify `Enemy_Flanker` closes more aggressively and periodically performs a visibly faster lateral burst.
5. Verify the Flanker backs away if it gets too close rather than remaining embedded in the player.
6. Verify `Enemy_Artillery` tries to preserve a much larger spacing band than the other two enemies.
7. Verify the Artillery fires a readable three-shot spread/salvo rather than the Skirmisher's single shot.
8. Put World/OneWay geometry between each enemy and the player; none should fire through it.
9. Move back into clear line of sight; firing should resume.
10. Confirm Nova can shoot/melee/Velocity Break/Powerslide all three enemies.
11. Confirm Echo can enemy-grapple them only with unobstructed line of sight.
12. Defeat each enemy and confirm its autonomous movement/attacks stop.
13. Verify the Hover aerial test maintains a position above/offset from the player and visibly bobs rather than falling under gravity.
14. Verify the Orbit aerial test adds circular X/Y motion while still tracking the player.
15. Put World/OneWay geometry between an aerial enemy and the player; it should stop firing through the obstruction.
16. Confirm Echo can grapple airborne enemies only with clear line of sight.
17. Confirm damaging enemies does not trigger the player's Hurt/Defeated Animator cues.

## Named-archetype mapping

Phase 3 adds `EnemyArchetypeCatalog` and `Docs/enemy-archetype-mapping.md`.

The catalog mirrors the role mapping and concrete values present in the preserved browser reference. Browser movement/projectile values are stored explicitly as **reference units**, not silently reused as Unity-world tuning.

Source-defined role mapping:

- Anchor: Shield, Heavy, Guard
- Artillery: Sniper, Turret
- Flanker: Charger, Interceptor, Wall Hunter
- Skirmisher: Walker, Hopper
- Aerial: Drone, Orbiter

## Next enemy-framework step

After the five shared roles pass Play Mode validation, port archetype-specific behaviors that the shared role baseline does not cover:

1. Shield brace / shield regeneration.
2. Guard low-tier projectile reflection and nearby-melee counter.
3. Charger / Hopper / Interceptor charged-shot dodge response.
4. Interceptor predictive pursuit.
5. Wall Hunter vertical pursuit.
6. Sniper perfect-opportunity projectile treatment.
7. Heavy armor/contact-damage tuning.
8. Final Drone/Orbiter motion tuning.

Named enemies remain `reference_only` until those specific behaviors are actually translated and tested.
