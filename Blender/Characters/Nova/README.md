# Nova Blender Workspace

Canonical source file: `Nova_master.blend`

Production brief: `Docs/nova-blender-production-brief.md`

## First action

Recommended on Windows:

Double-click:

`Blender/Launchers/Create_Nova_Master.bat`

It automatically finds Blender, creates `Nova_master.blend` if necessary, and
opens it normally.

The underlying setup helper creates:

- Metric / 1-meter unit configuration
- `NS_Nova_REFERENCE`
- `NS_Nova_GUIDES`
- `NS_Nova_GEO_BLOCKOUT`
- `NS_Nova_GEO_FINAL`
- `NS_Nova_RIG`
- `NS_Nova_SOCKETS`
- `NS_Nova_EXPORT`
- starter `RIG_Nova`
- exact gameplay-facing socket empties
- 1.85 m height guide
- primitive proportion markers

Save manually as `Nova_master.blend`.

Do not treat generated blockout markers as approved anatomy, armor, or topology.
They only establish scale and source-scene organization.


## Early-launcher repair

If `Nova_master.blend` was created before factory-scene cleanup was added and
still contains Blender's default `Camera`, `Cube`, and `Light`, close Blender,
pull the latest `dev/blender-production`, and double-click:

`Blender/Launchers/Repair_Nova_Master.bat`

The repair checks that the file is actually marked as Nova Striker's Nova source
scene, removes only the untouched factory objects from Blender's default
`Collection`, saves the master file, and reopens it.


## Generate the first Nova silhouette blockout

After the source scene is clean, close Blender and double-click:

`Blender/Launchers/Build_Nova_Blockout.bat`

This safely replaces only generated `BLOCKOUT_Nova_*` objects and preserves:

- `RIG_Nova`
- all gameplay-facing sockets
- guides/reference collections
- `NS_Nova_GEO_FINAL`
- any non-generated work outside the `BLOCKOUT_Nova_*` namespace

The generated blockout includes a full-body proportion pass, Sentinel-oriented
chest/shoulder/boot armor massing, helmet/visor mass, back module, blue energy
core, restrained navy waist soft-goods marker, and the integrated right-arm
cannon volume.

It is intentionally a silhouette/proportion review asset, not final topology.


## Nova Blockout V2 refinement

After reviewing the first front/side silhouette, run:

`Blender/Launchers/Build_Nova_Blockout_V2.bat`

V2 preserves the same rig/socket/reference/final-geometry safety rules while
refining only generated `BLOCKOUT_Nova_*` objects.

The V2 silhouette intentionally pushes Nova toward the approved military
Sentinel direction:

- stronger chest-to-waist taper
- flatter tactical shoulder pauldrons
- narrower sealed combat helmet and visor
- lower torso/back depth
- clearer thigh/knee/shin/boot rhythm
- slimmer, longer integrated right-arm cannon
- retained blue energy-core/visor/emitter language

The scene is tagged with `nova_striker_nova_blockout_version = "2.0"`.


## Nova Blockout V3 — final automated silhouette pass

After reviewing V2 from front and side, run:

`Blender/Launchers/Build_Nova_Blockout_V3.bat`

V3 is intended to be the final scripted proportion pass before pose/articulation
testing. It still modifies only generated `BLOCKOUT_Nova_*` objects and
preserves the canonical rig, sockets, guides, references, and `GEO_FINAL`.

V3 targets the remaining silhouette issues identified in V2:

- approximately 10–15% less helmet/head mass
- longer-looking upper arms and thighs
- lower hand placement
- more angular, lower-profile shoulder pauldrons
- multi-piece chest architecture instead of a single rectangular breastplate
- narrower pelvis and an added lower-abdomen armor break
- reduced boot mass
- tighter back-module placement
- slimmer, longer forearm-integrated cannon
- subtle forward helmet/chest bias in side profile

The scene is tagged with:

`nova_striker_nova_blockout_version = "3.0"`

and:

`nova_striker_nova_blockout_final_automated_pass = True`

After V3, the next gate is articulation review rather than another scripted
silhouette iteration.
