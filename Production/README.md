# Production Tracking

`Production/asset-manifest.json` is the machine-readable source of truth for major Nova Striker asset status.

The human-readable view and gate definitions live in:

`Docs/asset-production-manifest.md`

## Updating status

When an asset advances:

1. update the JSON manifest in the same change as the relevant production work when practical,
2. use only the defined status vocabulary,
3. do not mark a downstream gate approved when a required upstream gate is still unresolved,
4. preserve the browser reference status until the equivalent Unity mechanic is actually implemented and tested,
5. treat `gameplay_integrated` as a functional milestone, not merely an imported model/prefab.

The manifest is intentionally simple JSON so future tooling can generate dashboards, GitHub issue views, Unity editor checks, or Blender export validation from the same data.
