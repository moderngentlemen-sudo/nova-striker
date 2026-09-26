# Nova Blockout V3 Approval

Asset ID: `CHR-NOVA`
Branch: `dev/blender-production`
Approved blockout: `Nova Blockout V3`
Articulation baseline: `TEST_Nova_Articulation_v3_2_1`

## Approval scope

This approval locks Nova's current blockout for downstream production reference:

- overall 1.85 m character scale,
- front/side silhouette,
- helmet/body/boot mass relationships,
- segmented torso/abdomen architecture,
- integrated right-arm cannon direction,
- shoulder/hip/knee articulation clearance,
- crouch/Powerslide movement envelope,
- grounded pose contact behavior,
- cannon aim direction,
- wall-cling / wall-jump directional intent,
- downed/revive pose space,
- co-op Sync pose space.

This does **not** approve:

- final production topology,
- final surface detail,
- UVs,
- production materials/textures,
- skin weighting,
- production animation clips,
- Unity export,
- final rigging,
- final character reference sheet.

## Validation evidence

The approved review was run through the live Blender MCP path:

`standard MCP stdio -> execute_blender_code -> live Blender`

Environment:

- Blender 5.2.2 LTS
- Blender MCP add-on 1.2.0
- Blender MCP server 1.2.0
- source file remained unsaved during review
- 13 articulation poses
- 26 front/side renders
- 6 directional targets
- 0 direction failures

Contact results:

- all applicable grounded/body-contact poses passed,
- Dash Lean, Wall Cling, and Wall Jump Prep correctly use non-grounded contact modes.

Direction results:

- Aim Forward: 0.00 degrees error
- Aim Up: 0.00 degrees error
- Aim Down: 0.00 degrees error
- Cannon Fire: 0.00 degrees error
- Wall Cling: 11.45 degrees error within 18 degree tolerance
- Wall Jump Prep: 11.20 degrees error within 22 degree tolerance

## Production status

`blender_blockout = approved`

`model_final = not_started`

The next formal modeling gate is still blocked by the reference-sheet requirement.
`sheet` remains `in_progress` in the production manifest.

Final modeling may begin only after the Nova turnaround/reference sheet is marked
`approved` or the production owner explicitly revises that gate.
