# Enemy Archetype Mapping — Browser Reference → Unity Roles

This document records the enemy-role mapping and concrete behavior that are explicitly present in the preserved browser reference `nova-striker-v0.10-visual-completion-playable-preview.html` on `main`.

It is intentionally conservative: where the browser reference does not specify a behavior, this document does not invent one.

## Source-defined role mapping

The browser reference explicitly groups the twelve archetypes as follows:

| Unity role | Browser archetypes |
|---|---|
| Anchor | Shield, Heavy, Guard |
| Artillery | Sniper, Turret |
| Flanker | Charger, Interceptor, Wall Hunter |
| Skirmisher | Walker, Hopper |
| Aerial | Drone, Orbiter |

This mapping is now mirrored in `EnemyArchetypeCatalog`.

## Source-defined archetype facts

| Archetype | Role | HP | Shield | Armor | Browser movement reference | Fire reference | Special behavior explicitly present |
|---|---|---:|---:|---:|---|---|---|
| Walker | Skirmisher | 65 | 0 | 0 | Direct horizontal pursuit at 55 reference units/s | 320 speed, 11 damage, 2.2 s interval | — |
| Turret | Artillery | 65 | 0 | 0 | No base locomotion in the preserved update loop | 320 speed, 11 damage, 1.4 s interval | — |
| Drone | Aerial | 65 | 0 | 0 | Vertical sine hover; phase rate 1.5 and 28 reference movement magnitude | 320 speed, 11 damage, 2.2 s interval | Aerial squad positioning above/offset from target |
| Shield | Anchor | 110 | 90 | 55 | No base locomotion in the preserved update loop | 320 speed, 11 damage, 2.2 s interval | Braces against incoming fire; shield can regenerate slightly during brace |
| Hopper | Skirmisher | 65 | 0 | 0 | No unique base locomotion shown in the preserved update switch | 320 speed, 11 damage, 2.2 s interval | Dodges incoming Tier-2+ shots in the advanced reaction layer |
| Sniper | Artillery | 75 | 0 | 0 | No unique base locomotion in the preserved update switch | 470 speed, 11 damage, 2.05 s interval | Uses the browser's gold/perfect-opportunity projectile type |
| Charger | Flanker | 65 | 0 | 0 | Direct horizontal pursuit at 150 reference units/s | 320 speed, 11 damage, 2.2 s interval | Dodges incoming Tier-2+ shots |
| Orbiter | Aerial | 65 | 0 | 0 | Circular X/Y orbit; phase rate 2.0 and 38 reference movement magnitude | 320 speed, 11 damage, 2.2 s interval | Aerial squad positioning above/offset from target |
| Heavy | Anchor | 150 | 0 | 90 | Slow horizontal pursuit at 32 reference units/s | 320 speed, 15 damage, 2.65 s interval | 18 contact damage; highest preserved standard-enemy health |
| Wall Hunter | Flanker | 65 | 0 | 0 | Vertical pursuit toward target Y at 88 reference units/s | 320 speed, 11 damage, 2.2 s interval | Flanker squad positioning |
| Guard | Anchor | 105 | 40 | 65 | No unique base locomotion shown in the preserved update switch | 320 speed, 11 damage, 2.2 s interval | Reflects low-tier frontal projectiles; can counter nearby melee |
| Interceptor | Flanker | 65 | 0 | 0 | Predictive horizontal pursuit using target X + target VX × 0.35 at 115 reference units/s | 320 speed, 11 damage, 2.2 s interval | Dodges incoming Tier-2+ shots |

All movement and projectile-speed values above are **browser-reference units**, not Unity-world units.

## Shared squad behavior from the browser reference

The preserved advanced AI layer also assigns role-level desired positions:

- Anchor: approximately 145 reference units to one side of the target.
- Artillery: attempts to maintain larger separation, moving toward roughly 500 reference units of spacing when too close.
- Flanker: targets approximately ±190 reference units from the player, expanding to ±260 during a pincer tactic.
- Skirmisher: targets approximately 110 reference units of side spacing.
- Aerial: targets approximately ±130 horizontal and 150 vertical reference units above the player.

The browser squad system can choose tactics such as `fortify`, `pincer`, `crossfire`, and `advance`. Those group-level tactics are not yet ported into Unity.

## Unity status

Current Unity role coverage:

- Anchor — implemented baseline module.
- Skirmisher — implemented.
- Flanker — implemented.
- Artillery — implemented.
- Aerial — implemented with two motion patterns:
  - `HoverBob`, intended to support Drone-like behavior.
  - `Orbit`, intended to support Orbiter-like behavior.

The mechanics lab includes generic role-validation enemies. These are **not yet named production archetypes**.

## Porting rule for named enemies

A named enemy should only move from `reference_only` to active Unity gameplay work when its specific browser behavior has been translated deliberately.

The role catalog provides the shared baseline. Archetype-specific additions still required include, where supported by the reference:

- Shield brace / shield regeneration response.
- Guard projectile reflection and melee counter.
- Charger / Hopper / Interceptor charged-shot dodge reaction.
- Interceptor predictive pursuit.
- Wall Hunter vertical pursuit.
- Sniper perfect-opportunity shot treatment.
- Heavy armor/contact-damage profile.
- Drone and Orbiter motion-pattern tuning.

This preserves the browser behavior as the reference while still allowing the Unity AI architecture to remain modular.
