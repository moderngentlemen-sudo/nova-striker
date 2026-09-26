# Blender Review Outputs

Automated Blender review artifacts are generated locally and are intentionally
not committed by default.

Current Nova articulation output:

`Blender/Reviews/Nova/Articulation/latest/`

The automated review runner renders front/side images for every articulation
marker and writes:

- `articulation-report.json`
- `summary.txt`

The runner works from a temporary copy of the canonical `Nova_master.blend`,
so headless review does not overwrite the artist's working master file.
