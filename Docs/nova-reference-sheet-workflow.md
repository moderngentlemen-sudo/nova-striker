# Nova Production Reference Sheet Workflow

Asset ID: `CHR-NOVA`
Approved geometry authority: Nova Blockout V3
Validated articulation authority: V3.2.1

## Purpose

The production reference sheet translates the approved Blender blockout into a
clear modeling reference without allowing concept-sheet drift to redefine the
approved character proportions.

The sheet is not approved merely because the automated source pack renders
successfully.

## Generate the source pack

With Blender MCP configured, run:

`Blender/MCP/Generate_Nova_Reference_Pack.bat`

The live MCP workflow renders Nova's approved V3 blockout in a neutral pose from:

1. Front
2. Front three-quarter
3. Side
4. Back three-quarter
5. Back

The generator:

- uses the approved `BLOCKOUT_Nova_*` geometry,
- neutralizes the starter rig for consistent turnaround views,
- keeps both feet on the ground,
- isolates the approved blockout from unrelated mesh geometry,
- uses a consistent orthographic presentation,
- restores Blender state afterward,
- does not save `Nova_master.blend`,
- does not update the production manifest.

Output:

`Blender/Reviews/Nova/ReferencePack/latest/`

Expected files:

- `01_Front.png`
- `02_FrontThreeQuarter.png`
- `03_Side.png`
- `04_BackThreeQuarter.png`
- `05_Back.png`
- `reference-pack.json`
- `summary.txt`
- `reference-index.html`
- `approval-checklist.md`
- `mcp-run.json`

## Locked design inheritance

The final sheet must preserve:

- 1.85 m production scale,
- military / Sentinel identity,
- approved V3 shoulder-to-waist proportion,
- narrower sealed-helmet proportion,
- segmented chest / abdomen hierarchy,
- integrated right-arm cannon silhouette,
- athletic lower-body proportions,
- reduced boot mass,
- compact back power/comms module,
- the V3.2.1 articulation-clearance envelope.

These should not be redrawn into materially different proportions during sheet
cleanup.

## Design details still requiring resolution

The reference pack intentionally leaves these as review items:

- final chest / abdomen panel seams,
- final helmet brow / cheek / jaw / crown / rear shell,
- final arm-cannon housing and modular attachment geometry,
- soft-goods construction and exact placement,
- boot / shin surface detail,
- back-module detail,
- functional fasteners / vents / service panels,
- open / unhelmeted presentation,
- visor face-visibility treatment,
- modular team / class marking locations.

These are surface/construction decisions, not permission to redesign the
approved silhouette.

## Approval process

1. Generate the MCP reference pack.
2. Review all five views together in `reference-index.html`.
3. Resolve the open design details above in the final reference-sheet artwork.
4. Check every item in `approval-checklist.md`.
5. Confirm the front/side/back views describe one internally consistent object.
6. Confirm the sheet does not contradict V3 proportions or V3.2.1 articulation.
7. Production owner explicitly approves the sheet.
8. Only then update:
   `CHR-NOVA.sheet = approved`.
9. Run:
   `Blender/Launchers/Check_Nova_Production_Readiness.bat`.
10. If all gates pass, begin `Docs/nova-production-modeling-plan.md`.

## Approval boundary

The automated generator may create review-ready source material, but it must not
change `sheet` from `in_progress` to `approved`.

Sheet approval is a human production decision.
