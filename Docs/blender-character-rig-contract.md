# Shared Character Rig Contract — v1

This is the initial Blender-side rig convention for Nova Striker hero characters.
It is a production interoperability contract, not gameplay authority.

## Coordinate / scale

- Blender: Z-up
- Unity import: Y-up
- 1 Blender unit = 1 meter
- armature object transform before export:
  - location 0,0,0
  - rotation 0,0,0
  - scale 1,1,1
- character feet rest at Z = 0 in the source bind pose

## Canonical deformation skeleton

Initial shared bone names:

- `root`
- `pelvis`
- `spine_01`
- `spine_02`
- `spine_03`
- `neck`
- `head`
- `clavicle_l`
- `upperarm_l`
- `lowerarm_l`
- `hand_l`
- `clavicle_r`
- `upperarm_r`
- `lowerarm_r`
- `hand_r`
- `thigh_l`
- `calf_l`
- `foot_l`
- `toe_l`
- `thigh_r`
- `calf_r`
- `foot_r`
- `toe_r`

Additional twist, corrective, facial, scarf, armor, weapon, or mechanical bones may
be added downstream. Do not rename the shared core bones after animation production
has begun.

## Gameplay-facing sockets

The following names mirror `GameplayPresentationContract.Sockets` exactly:

- `socket_root`
- `socket_camera_focus`
- `socket_head`
- `socket_chest`
- `socket_back`
- `socket_hand_l`
- `socket_hand_r`
- `socket_weapon_primary`
- `socket_muzzle_primary`
- `socket_muzzle_secondary`
- `socket_ability_origin`
- `socket_foot_l`
- `socket_foot_r`

The setup helper creates these as bone-parented empties. A later rig implementation
may convert them to a different export-safe mechanism only if the Unity-facing names
and transforms remain stable.

## Bind-pose intent

Use a neutral A-pose or relaxed T/A hybrid appropriate for shoulder deformation.

Requirements:

- palms/forearms should not be twisted into extreme pronation
- clavicles should have neutral room for omni-aim
- hips and knees should support deep crouch and Powerslide
- feet should remain easy to plant for side-view locomotion
- spine must support upper/lower-body separation
- shoulder topology must support two-handed weapon poses for Echo
- Nova's right forearm must leave sufficient deformation/attachment room for the
  integrated arm weapon

## Armor/mechanical parenting

Hard armor should not be blindly skinned like cloth.

Preferred approaches:

- rigid or near-rigid weighting for hard plates
- controlled deformation zones around joints
- separate mechanical children for weapon parts/helmet modules when useful
- corrective shapes for extreme crouch, aim, and shoulder poses when required

The gameplay collider remains Unity-authored and is not derived from armor volume.

## Character-specific extensions

### Nova

Expected optional production bones may include:

- forearm weapon mechanism
- cannon shutters/barrel mechanics
- back comms/power module
- helmet components

### Echo

Expected optional production bones may include:

- nano-scarf chain
- protective collar mechanics
- gauntlet tool modules
- staff/rifle mechanical components
- helmet/mask components

These are presentation systems. They must not become authoritative for gameplay
damage, grapple timing, projectile spawn rules, or collision.

## Export gate

Before a character rig is considered ready for the first Unity round trip:

1. core shared bones exist and are uniquely named,
2. required sockets exist,
3. no unintended negative object scale remains,
4. armature object transform is clean,
5. mesh deforms through crouch/Powerslide/aim test poses,
6. FBX exports at Unity scale 1,
7. Unity can parent the art prefab under the appropriate visual root,
8. gameplay still works with the art prefab disabled.
