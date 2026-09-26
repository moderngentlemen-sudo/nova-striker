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

## First Blender action — recommended

Start with Nova.

On Windows, open:

`Blender/Launchers/`

and double-click:

`Create_Nova_Master.bat`

That launcher finds Blender automatically, creates and saves the canonical Nova
source scene if needed, and opens it in the normal Blender interface.

The manual Python setup path remains available for advanced use, but it is no
longer the default workflow.

After Blender opens, load the approved Nova visual references into the
`NS_Nova_REFERENCE` collection and replace the primitive proportion markers
with the actual blockout.

When ready, use:

- `Validate_Nova.bat`
- `Export_Nova_To_Unity.bat`

Do not begin final-detail modeling until the blockout has completed its first
FBX → Unity `NovaVisualRoot` round trip.

## Blender MCP automation

The Windows-first local MCP and automated-review layer lives under:

`Blender/MCP/`

Start with:

`Blender/MCP/Setup_Blender_MCP.bat`

The bootstrap keeps the Blender control socket on `127.0.0.1:9877`, installs
the third-party MCP bridge outside the repository, and creates an ignored local
client configuration.

The default automated review now genuinely uses MCP:

`Blender/MCP/Run_Nova_Automated_Review.bat`

Its execution path is:

`standard MCP stdio → execute_blender_code → live Blender → bpy`

The review creates only temporary in-memory camera/action state, renders all
front/side articulation views, restores the open Blender session, does not save
the source `.blend`, and writes `mcp-run.json` as transport proof.

The earlier deterministic no-MCP fallback remains available as:

`Blender/MCP/Run_Nova_Headless_Review.bat`

See `Blender/MCP/README.md` for setup, security, outputs, recovery, and client details.


## Nova reference-sheet source pack

With Nova Blockout V3 approved, the next visual-development handoff can be
generated directly from the approved Blender geometry through MCP:

`Blender/MCP/Generate_Nova_Reference_Pack.bat`

This creates neutral Front / Front 3-4 / Side / Back 3-4 / Back source renders
plus an approval checklist under:

`Blender/Reviews/Nova/ReferencePack/latest/`

Generation does not approve the sheet or save changes to the canonical
`Nova_master.blend`.

Use `Docs/nova-reference-sheet-workflow.md` for the approval procedure.
