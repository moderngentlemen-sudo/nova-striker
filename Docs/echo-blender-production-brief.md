# Echo — Blender Production Brief

Asset ID: `CHR-ECHO`
Canonical source: `Blender/Characters/Echo/Echo_master.blend`

## Design authority

Echo uses the approved **Pursuit Protocol / Variation 02** direction. He should
share Nova Striker's technological design family with Nova while remaining
immediately distinguishable in silhouette and equipment.

## Proportion target

- male
- target height: 1.83 m
- athletic, streamlined close-combat build
- narrower/faster visual rhythm than Nova
- silhouette should emphasize pursuit, forward motion, collar/scarf language,
  gauntlet tools, and staff/rifle equipment

## Material language

- white armor
- black/charcoal technical undersuit
- gray/titanium structural details
- deep amber-orange emissive accents
- semi-transparent amber visor

Do not make Echo a palette swap of Nova.

## Headgear states

Support three presentation states:

1. bare-face state
2. tactical/survival mask with the ears covered
3. full helmet with semi-transparent amber visor

All states must use the same underlying rig.

## Nano-adaptive scarf / collar

The scarf remains intact and integrated into the protective collar.

Model the base collar/scarf system so later presentation can support:

- normal drape
- close tactical wrap
- weather-protection wrap
- camouflage/field presentation

The scarf may be separately rigged or simulated later, but its geometry must not
control gameplay timing, grapple behavior, or collision.

## Equipment

### Gauntlet multi-tools

Plan detachable/interchangeable gauntlet presentation supporting:

- blade
- claw
- tracker dart
- shock
- grapple modules

### Combined staff/rifle

The primary multi-tool should support clear presentation modes:

- staff
- spear
- rifle

Back-mount and hand attachment must use stable sockets. Mechanical transformation
can be authored later, but the blockout must prove stored and deployed silhouettes.

## Gameplay deformation requirements

Echo's blockout must specifically survive:

- close melee chains
- aerial melee
- Dodge Counter
- advancing Throw
- enemy grapple
- traversal grapple
- wall movement
- dash/Powerslide
- rifle aiming
- staff two-handed poses
- downed/revive
- Pursuit Protocol pose range

## Socket requirements

Use the exact shared presentation socket names from
`GameplayPresentationContract.Sockets`.

Particular emphasis:

- `socket_back` for stored staff/rifle
- `socket_hand_l`
- `socket_hand_r`
- `socket_weapon_primary`
- `socket_muzzle_primary`
- `socket_ability_origin`

## First Blender milestone

Do not begin Echo final detailing until Nova has completed one successful
Blender → FBX → Unity visual-root round trip.

Echo's first blockout review must then prove:

- 1.83 m scale
- Pursuit Protocol silhouette
- collar/scarf volume
- all three headgear states
- gauntlet multi-tool volumes
- stored/deployed staff-rifle silhouettes
- melee/grapple articulation clearance
- successful Unity import at scale 1
