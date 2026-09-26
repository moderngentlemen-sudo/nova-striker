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
- `Build_Nova_Blockout.bat` — replaces only the generated
  `BLOCKOUT_Nova_*` guide geometry with the first recognizable full-body Nova
  silhouette/armor blockout, saves, then opens it.
- `Build_Nova_Blockout_V2.bat` — regenerates only the `BLOCKOUT_Nova_*`
  namespace with the refined military/Sentinel proportions: tapered torso,
  flatter pauldrons, narrower helmet/visor, clearer knee/leg rhythm, reduced
  torso depth, and a slimmer/longer integrated arm cannon.
- `Build_Nova_Blockout_V3.bat` — final automated silhouette/proportion pass
  before articulation review. It further reduces head/boot mass, lengthens the
  visual arm/leg rhythm, introduces angular pauldrons and a multi-piece chest,
  narrows the pelvis, tightens the back module, and integrates the arm cannon
  more deeply along the forearm.
- `Test_Nova_Articulation.bat` — creates the V2 dedicated, removable Blender
  articulation test with labeled timeline poses for neutral, forward/up/down
  aim, deep crouch, Powerslide, dash lean, wall cling, wall-jump preparation,
  cannon fire, downed, revive reach, and co-op Sync. V2 adds geometry-aware
  floor planting, calibrated pose limits, more realistic root/pelvis placement,
  and an `NS_Nova_Articulation_Report` Blender text block with contact-height
  diagnostics.
- `Clear_Nova_Articulation_Test.bat` — removes the generated V1/V2 articulation
  action/markers/report and restores the previous action when available, or a
  neutral pose otherwise.
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


## Nova production readiness

After the V3 blockout/articulation gate is approved, run:

`Check_Nova_Production_Readiness.bat`

The check reads `Production/asset-manifest.json` and requires all three entry
conditions before final production modeling:

- Nova concept = `approved`
- Nova reference sheet = `approved`
- Nova Blender blockout = `approved`

It does not modify Blender or the manifest. If a gate is still blocked, it
reports the blocker rather than silently advancing production status.
