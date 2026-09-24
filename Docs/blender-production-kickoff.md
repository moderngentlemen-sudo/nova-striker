# Blender Production Kickoff

Branch: `dev/blender-production`
Source gameplay baseline: `dev/gameplay-complete`

This branch begins Nova Striker production-art work after the gameplay branch reached
its terminal source-side pre-Blender boundary and passed the aggregate Unity
source/scene validation gate.

## Production rule

Do not move gameplay authority into art.

Blender owns:

- production meshes
- UVs
- high/low-poly source work
- rigging and skinning
- authored skeletal animation
- baked texture source
- environment/prop source geometry

Unity continues to own:

- Rigidbody2D / gameplay colliders
- movement and combat timing
- hit / parry / dash windows
- camera and co-op framing
- runtime materials/shaders
- VFX and haptics routing
- Animator state machines and gameplay parameter driving
- save, commerce, progression, and encounter authority

## First production round trip

The first goal is not to finish the entire art catalog. It is to prove one complete
production path:

1. lock Nova's Blender production brief,
2. create Nova source scene and blockout,
3. establish the shared humanoid rig/socket convention,
4. export Nova to FBX,
5. import under Unity's `NovaVisualRoot`,
6. confirm gameplay remains unchanged with production presentation attached,
7. apply lessons to Echo,
8. then validate one standard enemy, Aegis, and one Skyport modular environment kit.

Do not expand to the full asset roster until this round trip is stable.

## Character order

### 1. Nova

Nova is the first pipeline-validation hero.

Preserve:

- male, approximately 185 cm / 6'1"
- streamlined but clearly military / frontline-soldier Strike Suit
- protective Sentinel identity rather than hunter styling
- white/gray ceramic armor over a black/carbon technical undersuit
- restrained navy fabric/soft-goods accents
- titanium/metal structural details
- blue emissive energy treatment and blue-tinted visor
- full sealed combat helmet with integrated comms / AR-HUD language
- readable unhelmeted/open presentation state
- integrated right-arm ranged weapon/cannon architecture
- tactical equipment that supports precision-to-high-impact ranged combat
- strong shoulder, forearm, boot, and torso silhouette that reads clearly at gameplay distance
- deformation clearance for crouch, Powerslide, wall movement, omni-aim, dash, downed/revive, and co-op Sync

Do not reintroduce the discarded hunter-like Nova direction.

### 2. Echo

Echo follows once Nova's rig/export/prefab path is proven.

Preserve:

- male, approximately 183 cm / 6'0"
- approved Pursuit Protocol / Variation 02 direction
- streamlined masculine athletic silhouette
- white/black/gray armor with deep amber-orange emissive accents
- protective collar with intact nano-adaptive scarf
- bare-face state
- tactical mask state that covers the ears
- full helmet with semi-transparent amber visor
- utility belt and field-ready equipment language
- detachable gauntlet multi-tools
- combined staff/rifle multi-tool with staff, spear, and rifle presentation modes
- close-combat / pursuit silhouette distinct from Nova's ranged Sentinel shape

## Team-marking constraint

Do not bake a permanent Strike Team number or Striker 0/1 assignment into geometry,
normal maps, or irreplaceable decals during this first character pass. Team assignment
and class markings should remain modular material/decal layers until the roster
presentation is explicitly locked.

## Source files

Canonical character sources:

- `Blender/Characters/Nova/Nova_master.blend`
- `Blender/Characters/Echo/Echo_master.blend`

Unity-ready exports:

- `UnityProject/Assets/Art/Models/Characters/Nova/`
- `UnityProject/Assets/Art/Models/Characters/Echo/`

Native `.blend` files must never be placed inside `UnityProject/Assets`.

## Tooling

### Recommended Windows workflow

Use the one-click files under `Blender/Launchers/`:

- `Create_Nova_Master.bat`
- `Open_Nova.bat`
- `Validate_Nova.bat`
- `Export_Nova_To_Unity.bat`

Equivalent Echo launchers are included for the second character round trip.

The launchers automatically discover Blender and wrap the technical scripts
below.

### Underlying tools

`Blender/Tools/ns_character_source_setup.py`

Creates the non-destructive source-scene scaffold, unit setup, canonical collection
layout, starter humanoid rig, scale guide, and gameplay-facing socket empties.

`Blender/Tools/ns_export_character_fbx.py`

Validates transform/socket basics and exports a Unity-facing FBX using the project's
axis/scale conventions.

These scripts are production helpers, not substitutes for character modeling,
topology, skinning, or animation review.

## Gate to begin final modeling

Before a blockout advances to final model work:

- approved visual references are loaded into the Blender source scene,
- overall height and silhouette match the character brief,
- armor articulation has been checked in crouch/Powerslide/aim poses,
- helmet/open-head states are planned as modular presentation pieces,
- weapon/equipment attachment strategy is explicit,
- required gameplay sockets exist and remain stable,
- the blockout imports into Unity at scale 1 under the correct visual root.

The first milestone is **Nova Blender blockout review**, not a final textured model.
