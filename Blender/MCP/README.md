# Nova Striker Blender MCP Automation

This folder provides the local Blender MCP automation layer for Nova Striker.

The version-controlled Nova Striker Blender scripts remain authoritative for
blockout generation, articulation definitions, validation, and export. MCP is
the live command transport into Blender; it does not replace those scripts.

## Security boundary

The bootstrap configures the Blender-side socket only on:

`127.0.0.1:9877`

Do not expose this port through router forwarding, a public bind, or an
untrusted tunnel. The bridge can execute Blender Python and should be treated as
local development tooling.

## Third-party MCP bridge

The default bootstrap uses:

`https://github.com/anhez/blender-mcp.git`

The checkout is stored outside this repository at:

`%LOCALAPPDATA%\NovaStriker\Tools\blender-mcp`

The third-party source is not vendored into Nova Striker.

## One-time setup

Pull `dev/blender-production`, close Blender, then double-click:

`Setup_Blender_MCP.bat`

The idempotent setup:

1. finds Blender,
2. finds Python 3.10+,
3. clones or fast-forward updates the local Blender MCP checkout,
4. creates an isolated Python virtual environment,
5. installs the MCP server into that environment,
6. copies and enables the Blender add-on,
7. writes local-only MCP environment variables,
8. creates an ignored local MCP client config,
9. launches `Nova_master.blend` with the deterministic `bpy.app.timers` pump,
10. waits for `127.0.0.1:9877`,
11. verifies a real Blender main-thread command round-trip.

The setup never configures a public listener.

## Normal local workflow

### Start Blender + MCP

Double-click:

`Start_Blender_MCP.bat`

This opens Nova's canonical master file and starts the local MCP bridge through
Nova Striker's deterministic main-thread command pump.

### Read-only connection test

Double-click:

`Test_Blender_MCP.bat`

The smoke test performs only:

- MCP ping,
- scene inspection,
- `RIG_Nova` inspection.

It deliberately does not create, delete, or modify Blender objects.

## MCP-driven Nova review — default

Double-click either:

- `Run_Nova_Automated_Review.bat`
- `Run_Nova_MCP_Review.bat`

Both launch the MCP-driven path:

`standard MCP stdio → execute_blender_code → live Blender → bpy`

The runner:

1. starts or reuses the live local Blender MCP session,
2. launches the third-party MCP `server.py` through the standard stdio
   protocol,
3. initializes an MCP `ClientSession`,
4. calls the MCP `ping` tool,
5. calls the MCP `execute_blender_code` tool,
6. creates a temporary in-memory articulation action inside the live Blender
   session,
7. renders all 13 front/side pose pairs,
8. writes structured contact diagnostics,
9. solves and measures world-space pose direction for Aim Forward, Aim Up,
   Aim Down, Cannon Fire, Wall Cling, and Wall Jump Prep,
10. writes direction-angle diagnostics with per-pose tolerances,
11. writes a visual review index showing both contact and direction status,
12. removes the temporary action/camera,
13. restores the user's frame, camera, render settings, active action, and
    display state,
14. does **not** save the open `.blend` file.

The live review output includes:

- 26 PNG renders,
- `articulation-report.json`,
- `summary.txt`,
- `review-index.html`,
- `mcp-run.json` — proof that the standard MCP stdio session called
  `execute_blender_code` against the connected Blender instance.

Current output:

`Blender/Reviews/Nova/Articulation/latest/`

The `latest` review artifacts are gitignored by design.

## Deterministic headless fallback

Double-click:

`Run_Nova_Headless_Review.bat`

This preserves the earlier headless workflow. It works from a temporary copy of
`Nova_master.blend` and does not use MCP. Use it if the live MCP bridge is
unavailable or when a no-UI deterministic run is preferred.

## MCP client configuration

After setup, an ignored machine-specific configuration is written to:

`Blender/MCP/.local/mcp-client.local.json`

It points an MCP client at the isolated Python environment and local
`server.py`. Blender hosts the TCP bridge at `127.0.0.1:9877`; the MCP client
launches the stdio server.

## ChatGPT connection

The local Blender bridge works immediately with local MCP clients. A cloud
ChatGPT session cannot directly reach a Windows-only `127.0.0.1` service.
Connecting it to ChatGPT requires a separately authorized secure MCP exposure
supported by the user's ChatGPT workspace.

Do not change the Blender listener from `127.0.0.1` as a workaround.

## Recovery

Running `Setup_Blender_MCP.bat` again is the normal repair/update path. It
updates the local MCP checkout, reinstalls the editable Python package,
recopies/enables the add-on, and refreshes local configuration.

If an update has non-fast-forward changes in the third-party checkout, setup
stops instead of discarding them.


## MCP-driven Nova production reference pack

After Nova Blockout V3 and the V3.2.1 articulation gate are approved, generate
the modeling-reference source pack with:

`Generate_Nova_Reference_Pack.bat`

This uses the same live MCP transport:

`standard MCP stdio → execute_blender_code → live Blender → bpy`

The generator renders the approved blockout in a neutral pose from five
consistent views:

- Front
- Front three-quarter
- Side
- Back three-quarter
- Back

Output:

`Blender/Reviews/Nova/ReferencePack/latest/`

The pack includes the five PNGs, `reference-pack.json`, `summary.txt`,
`reference-index.html`, `approval-checklist.md`, and `mcp-run.json`.

The live operation restores Blender state afterward and does not save
`Nova_master.blend`.

This generator is intentionally **not** an approval action. It leaves
`CHR-NOVA.sheet = in_progress`. Review and explicit production-owner approval
are required before the production manifest may advance the sheet gate.

See:

`Docs/nova-reference-sheet-workflow.md`
