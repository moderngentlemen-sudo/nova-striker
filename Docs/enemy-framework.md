# Enemy Framework — Phase 2

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

The Aerial role remains future work.

## Mechanics-lab representative enemy

After regenerating the mechanics lab, the scene includes three generic framework-validation enemies:

- `Enemy_Skirmisher` — mid-range spacing behavior.
- `Enemy_Flanker` — faster pressure/reposition behavior.
- `Enemy_Artillery` — long-range salvo behavior.

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
13. Confirm damaging enemies does not trigger the player's Hurt/Defeated Animator cues.

## Next enemy-framework step

Once Skirmisher, Flanker, and Artillery are confirmed in Play Mode, the next engineering step is:

1. add the Aerial role module,
2. extract the preserved browser behavior of each named enemy archetype,
3. map those archetypes onto role + parameter combinations where the reference supports it,
4. create bespoke modules only where a named enemy genuinely requires behavior that the shared roles cannot express.

This avoids inventing behavior from archetype names alone and prevents the project from drifting away from the preserved gameplay reference.
