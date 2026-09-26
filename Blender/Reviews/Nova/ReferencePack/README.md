# Nova Reference Pack

Generated output for Nova's production-turnaround review is written locally to:

`Blender/Reviews/Nova/ReferencePack/latest/`

Run:

`Blender/MCP/Generate_Nova_Reference_Pack.bat`

The `latest` folder is ignored by Git and is regenerated on each run.

The pack uses the live Blender MCP path and produces six neutral orthographic
source views (Front, Front 3/4, Side, Back 3/4, Back, Top), metadata, an HTML review page, an approval checklist, and MCP
execution proof.

Generating this pack does **not** approve `CHR-NOVA.sheet`. The production
manifest remains `in_progress` until the production owner explicitly approves
the completed reference sheet.
