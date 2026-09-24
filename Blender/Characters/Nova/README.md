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
