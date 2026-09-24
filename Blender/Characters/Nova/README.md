# Nova Blender Workspace

Canonical source file: `Nova_master.blend`

Production brief: `Docs/nova-blender-production-brief.md`

## First action in Blender

Open a new Blender scene and run:

`Blender/Tools/ns_character_source_setup.py -- --character Nova`

The helper creates:

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
