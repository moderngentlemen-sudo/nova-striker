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
