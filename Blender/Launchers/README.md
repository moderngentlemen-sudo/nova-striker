# One-Click Blender Launchers

Windows users should use these launchers instead of manually running Blender
Python scripts.

## Nova

Double-click:

- `Create_Nova_Master.bat` — finds Blender, creates
  `Blender/Characters/Nova/Nova_master.blend`, then opens it.
- `Open_Nova.bat` — opens the existing Nova master file; creates it first if
  it does not exist.
- `Repair_Nova_Master.bat` — safely removes Blender's factory Camera/Cube/Light
  from master files created by the earliest launcher revision, saves, then opens
  the repaired file.
- `Validate_Nova.bat` — runs the Blender → Unity source/export contract checks
  without writing an FBX.
- `Export_Nova_To_Unity.bat` — validates and exports
  `UnityProject/Assets/Art/Models/Characters/Nova/Nova.fbx`.

## Echo

Equivalent Echo launchers are included:

- `Create_Echo_Master.bat`
- `Open_Echo.bat`
- `Repair_Echo_Master.bat`
- `Validate_Echo.bat`
- `Export_Echo_To_Unity.bat`

Echo should still follow Nova's first successful Blender → Unity round trip.

## Blender discovery

The launcher searches, in order:

1. `BLENDER_EXE` environment variable
2. Blender available on PATH
3. normal `C:\Program Files\Blender Foundation\...` installations
4. Steam's common Blender install
5. Local AppData Blender Foundation installs

If Blender cannot be found, the launcher prints instructions and does not modify
the project.

## Safety

Creating a master file never overwrites an existing `*_master.blend`.
If the file already exists, the Create launcher simply opens it.

New master files now remove Blender's untouched factory Camera/Cube/Light before
the Nova Striker scaffold is created. The Repair launcher is only for master files
created before that cleanup was added; it verifies the Nova Striker character
marker before making changes.

The Export launchers currently allow the generated blockout meshes so the first
pipeline round trip can be tested before final character topology exists. Once a
final mesh is present in the canonical `*_GEO_FINAL` collection, the exporter
prefers it automatically.

The launchers are Windows convenience wrappers around
`NovaStriker_Blender.ps1`. The underlying Python tools remain available for
automation and advanced use.
