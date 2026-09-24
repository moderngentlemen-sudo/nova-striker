# Blender Source Workspace

This folder is the source-of-truth workspace for Nova Striker 3D modeling and authored skeletal animation.

Suggested structure:

```
Blender/
├── Characters/
│   ├── Nova/
│   └── Echo/
├── Enemies/
├── Guardians/
│   ├── Aegis/
│   ├── Cinder/
│   ├── Mycel/
│   ├── Rime/
│   ├── Tempest/
│   └── Null/
├── Weapons/
├── Environment/
│   ├── Skyport/
│   ├── EmberWorks/
│   ├── VerdantVault/
│   ├── CryoRelay/
│   ├── StormSpire/
│   └── EclipseCore/
├── Props/
└── Shared/
```

Do not put `.blend` files inside `UnityProject/Assets`.

Use `Docs/blender-unity-art-pipeline.md` for scale, naming, export, weak-point, rigging, and Unity integration rules.


## Current production branch

Blender work now proceeds on:

`dev/blender-production`

The validated gameplay baseline remains frozen on `dev/gameplay-complete`.

## Character production helpers

Available under `Blender/Tools/`:

- `ns_character_source_setup.py` — creates a non-destructive Nova/Echo source-scene scaffold, shared starter armature, scale guide, blockout markers, and the exact Unity-facing socket empties.
- `ns_export_character_fbx.py` — validates canonical armature/socket/object transforms and exports a Unity-facing FBX.

Production briefs:

- `Docs/nova-blender-production-brief.md`
- `Docs/echo-blender-production-brief.md`
- `Docs/blender-character-rig-contract.md`
- `Docs/blender-production-kickoff.md`

## First Blender action

Start with Nova.

From a new Blender scene, run the setup helper with `--character Nova`, load
the approved Nova visual references into the `NS_Nova_REFERENCE` collection,
replace the primitive proportion markers with the actual blockout, and save the
canonical source as:

`Blender/Characters/Nova/Nova_master.blend`

Do not begin final-detail modeling until the blockout has completed its first
FBX → Unity `NovaVisualRoot` round trip.
