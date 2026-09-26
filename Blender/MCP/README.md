# Nova Striker Blender MCP Automation

This folder provides an optional local automation layer for Blender production.

It does **not** replace the canonical Nova Striker Blender scripts. The existing
version-controlled tools remain authoritative for blockout generation,
articulation review, validation, and export.

## Security boundary

The bootstrap intentionally configures the Blender-side socket only on:

`127.0.0.1:9877`

Do not expose this port through router forwarding, a public bind, or an
untrusted tunnel. The selected MCP bridge can execute Blender Python and should
be treated as local development tooling.

## Third-party MCP bridge

The default bootstrap uses:

`https://github.com/anhez/blender-mcp.git`

The checkout is stored outside this repository at:

`%LOCALAPPDATA%\NovaStriker\Tools\blender-mcp`

The third-party source is not vendored into Nova Striker.

## One-time setup

Pull `dev/blender-production`, close Blender, then double-click:

`Setup_Blender_MCP.bat`

The setup is idempotent. It:

1. finds Blender,
2. finds Python 3.10+,
3. clones or fast-forward updates the local Blender MCP checkout,
4. creates an isolated Python virtual environment,
5. installs the MCP server into that environment,
6. copies the Blender add-on into the current Blender user add-ons directory,
7. enables the add-on and saves Blender preferences,
8. writes user-level local-only MCP environment variables,
9. creates an ignored local MCP client config under `.local/`,
10. launches `Nova_master.blend` with the Nova Striker deterministic
    `bpy.app.timers` command pump,
11. waits for `127.0.0.1:9877`,
12. verifies a real Blender main-thread command round-trip before reporting the
    MCP connection healthy.

The setup never configures a public listener.

## Normal local workflow

### Start Blender + MCP

Double-click:

`Start_Blender_MCP.bat`

This opens Nova's canonical master file and starts the local MCP socket through
Nova Striker's deterministic `bpy.app.timers` bootstrap. This avoids relying on
the third-party add-on's UI modal-timer autostart path during Blender startup.

### Read-only connection test

Double-click:

`Test_Blender_MCP.bat`

The smoke test performs only:

- MCP ping,
- scene inspection,
- `RIG_Nova` inspection.

It deliberately does not create, delete, or modify Blender objects.

### Automated Nova review

Double-click:

`Run_Nova_Automated_Review.bat`

This does not edit the artist's open master file. The runner:

1. copies `Nova_master.blend` to a temporary review file,
2. opens the copy with headless Blender,
3. ensures the current Nova articulation V3 review action exists,
4. visits all 13 review poses,
5. renders front and side orthographic images for every pose,
6. records pose bounds and structured contact diagnostics, including absolute
   floor error and pair-contact spread,
7. writes `articulation-report.json`,
8. writes `summary.txt`,
9. opens the local review output folder.

Current output:

`Blender/Reviews/Nova/Articulation/latest/`

The `latest` review artifacts are gitignored by design.

## MCP client configuration

After setup, an ignored machine-specific configuration is written to:

`Blender/MCP/.local/mcp-client.local.json`

It points the MCP client at the isolated Python environment and the local
`server.py` checkout. Use that file as the source when configuring a desktop
MCP client.

The MCP stdio server should be launched by the client. Blender itself hosts the
local TCP bridge at `127.0.0.1:9877`.

## ChatGPT connection

The local Blender bridge is useful immediately with local MCP clients. A cloud
ChatGPT session cannot directly reach a Windows-only `127.0.0.1` service.
Connecting this same toolchain to ChatGPT requires a separately authorized
secure MCP exposure supported by the user's ChatGPT workspace.

Do not change the Blender listener from `127.0.0.1` as a workaround.

## Recovery

Running `Setup_Blender_MCP.bat` again is the normal repair/update path. It
updates the local MCP checkout with a fast-forward pull, reinstalls the editable
Python package, recopies the add-on, reenables it, and refreshes local config.

If an update has non-fast-forward changes in the third-party checkout, setup
stops instead of discarding them.
