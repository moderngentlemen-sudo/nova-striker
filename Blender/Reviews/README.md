# Blender Review Outputs

Automated Blender review artifacts are generated locally and are intentionally
not committed by default.

Current Nova articulation output:

`Blender/Reviews/Nova/Articulation/latest/`

The default MCP-driven review writes:

- 26 front/side PNG renders,
- `articulation-report.json`,
- `summary.txt`
- `review-index.html`
- `mcp-run.json` when the live MCP path is used

Current V3.2 reviews include both floor/contact diagnostics and world-space
direction-angle diagnostics for the cannon aim and wall-reach poses.,
- `review-index.html`,
- `mcp-run.json` — proof that a standard MCP stdio client invoked
  `execute_blender_code` against the connected live Blender session.

The live MCP review uses a temporary in-memory action/camera, restores the open
Blender session afterward, and does not save the source `.blend` file.

`Run_Nova_Headless_Review.bat` remains available as a deterministic fallback.
That path works from a temporary copy of `Nova_master.blend` and does not use
MCP.
