# Asset Production Manifest

This document is the human-readable production tracker for Nova Striker. The machine-readable counterpart is `Production/asset-manifest.json`.

## Authoritative pipeline

Every production asset advances through the same gates:

`Foundation → Concept Approved → Sheet Approved → Blender Blockout → Model Final → Rigged → Animation Ready → Exported → Unity Prefab → Gameplay Integrated → QA Approved`

Not every gate applies to every asset. Environment pieces, for example, generally do not require character rigging.

### Status vocabulary

- `foundation` — the asset exists as an established game concept, but its production specification is not locked.
- `not_started` — no work has begun for that gate.
- `in_progress` — active work is underway.
- `review` — work exists and is awaiting approval.
- `approved` — approved for downstream use.
- `scaffolded` — supporting code/data architecture exists, but the asset itself is not integrated.
- `reference_only` — represented only by the preserved browser prototype.
- `not_applicable` — that gate does not apply.

## Production rule

A later gate must not silently redefine an earlier approved gate.

Examples:

- Unity should not change Nova's proportions without returning the change to visual development.
- Blender should not invent a Guardian weak point that conflicts with the gameplay specification.
- Animation clips may exaggerate motion, but gameplay code remains authoritative for hit, dash, parry, charge, and damage timing.
- Lore changes that materially alter a model or environment should update the relevant production brief before Blender work is considered final.

## Current high-level status

### Protagonists

| ID | Asset | Concept | Sheet | Blender | Rig | Anim | Unity | Gameplay |
|---|---|---|---|---|---|---|---|---|
| CHR-NOVA | Nova | approved | in_progress | not_started | not_started | not_started | scaffolded | in_progress |
| CHR-ECHO | Echo | approved | in_progress | not_started | not_started | not_started | scaffolded | in_progress |

Nova and Echo now both have approved production directions, active reference-sheet preparation, active Unity gameplay work, and a scaffolded presentation handoff: separate character visual roots and a shared `StrikerPresentationBridge` for Animator/event integration. Blender production has moved to `dev/blender-production`. `scaffolded` in the Unity column does **not** mean a production model, rig, or character-art prefab exists.

### Guardians

| ID | Guardian | Concept | Sheet | Blender | Rig | Anim | Unity | Gameplay |
|---|---|---|---|---|---|---|---|---|
| BOS-AEGIS | Aegis | foundation | not_started | not_started | not_started | not_started | not_started | scaffolded |
| BOS-CINDER | Cinder | foundation | not_started | not_started | not_started | not_started | not_started | scaffolded |
| BOS-MYCEL | Mycel | foundation | not_started | not_started | not_started | not_started | not_started | scaffolded |
| BOS-RIME | Rime | foundation | not_started | not_started | not_started | not_started | not_started | scaffolded |
| BOS-TEMPEST | Tempest | foundation | not_started | not_started | not_started | not_started | not_started | scaffolded |
| BOS-NULL | Null | foundation | not_started | not_started | not_started | not_started | not_started | scaffolded |

`scaffolded` reflects the Guardian data architecture only; the six Unity behavior trees are not yet ported.

### Enemy archetypes

Unity now also contains a **generic modular enemy-runtime framework** (`EnemyBrain2D` plus Anchor, Skirmisher, Flanker, Artillery, and Aerial roles), mechanics-lab role-test enemies, and a source-derived `EnemyArchetypeCatalog` that maps all 12 preserved browser enemy names to those shared roles. This does not advance any named production enemy below beyond `reference_only`; it validates shared AI/combat architecture before a specific archetype is selected for production.

The current browser reference contains 12 enemy archetypes:

| ID | Asset | Concept | Sheet | Blender | Rig | Unity | Gameplay |
|---|---|---|---|---|---|---|---|
| ENM-WALKER | Walker | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-TURRET | Turret | foundation | not_started | not_started | not_applicable | not_started | reference_only |
| ENM-DRONE | Drone | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-SHIELD | Shield | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-HOPPER | Hopper | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-SNIPER | Sniper | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-CHARGER | Charger | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-ORBITER | Orbiter | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-HEAVY | Heavy | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-WALLHUNTER | Wall Hunter | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-GUARD | Guard | foundation | not_started | not_started | not_started | not_started | reference_only |
| ENM-INTERCEPTOR | Interceptor | foundation | not_started | not_started | not_started | not_started | reference_only |

### Weapons

| ID | Weapon | Concept | Sheet | Blender | Unity | Gameplay |
|---|---|---|---|---|---|---|
| WPN-PULSE | Pulse | foundation | not_started | not_started | not_started | reference_only |
| WPN-ARC-FAN | Arc Fan | foundation | not_started | not_started | not_started | reference_only |
| WPN-RAIL-LANCE | Rail Lance | foundation | not_started | not_started | not_started | reference_only |
| WPN-VOLT-DISC | Volt Disc | foundation | not_started | not_started | not_started | reference_only |
| WPN-CRYO-BURST | Cryo Burst | foundation | not_started | not_started | not_started | reference_only |
| WPN-NOVA-BEAM | Nova Beam | foundation | not_started | not_started | not_started | reference_only |
| WPN-PHOTON-SPEAR | Photon Spear | foundation | not_started | not_started | not_started | reference_only |
| WPN-GRAVITY-WELL | Gravity Well | foundation | not_started | not_started | not_started | reference_only |
| WPN-MAGMA-TALON | Magma Talon | foundation | not_started | not_started | not_started | reference_only |
| WPN-ARC-CYCLONE | Arc Cyclone | foundation | not_started | not_started | not_started | reference_only |
| WPN-ECHO-MINES | Echo Mines | foundation | not_started | not_started | not_started | reference_only |
| WPN-NULL-CANNON | Null Cannon | foundation | not_started | not_started | not_started | reference_only |

The Unity branch already has the generic `WeaponDefinition` ScriptableObject, but individual weapon assets and behaviors are not yet ported.

### Environment acts

| ID | Sector / Act | Concept | Environment Sheet | Blender Kit | Unity Scene | Gameplay |
|---|---|---|---|---|---|---|
| ENV-SKYPORT-01 | Skyport — Transit Spine | foundation | not_started | not_started | not_started | reference_only |
| ENV-SKYPORT-02 | Skyport — Maintenance Interior | foundation | not_started | not_started | not_started | reference_only |
| ENV-SKYPORT-03 | Skyport — Upper Skyline | foundation | not_started | not_started | not_started | reference_only |
| ENV-EMBER-01 | Ember Works — Furnace Walk | foundation | not_started | not_started | not_started | reference_only |
| ENV-EMBER-02 | Ember Works — Foundry Shaft | foundation | not_started | not_started | not_started | reference_only |
| ENV-EMBER-03 | Ember Works — Core Forge | foundation | not_started | not_started | not_started | reference_only |
| ENV-VERDANT-01 | Verdant Vault — Root Access | foundation | not_started | not_started | not_started | reference_only |
| ENV-VERDANT-02 | Verdant Vault — Canopy Engine | foundation | not_started | not_started | not_started | reference_only |
| ENV-VERDANT-03 | Verdant Vault — Memory Garden | foundation | not_started | not_started | not_started | reference_only |
| ENV-CRYO-01 | Cryo Relay — Frozen Array | foundation | not_started | not_started | not_started | reference_only |
| ENV-CRYO-02 | Cryo Relay — Coolant Vault | foundation | not_started | not_started | not_started | reference_only |
| ENV-CRYO-03 | Cryo Relay — Glass Relay | foundation | not_started | not_started | not_started | reference_only |
| ENV-STORM-01 | Storm Spire — Lower Conduit | foundation | not_started | not_started | not_started | reference_only |
| ENV-STORM-02 | Storm Spire — Thunder Ring | foundation | not_started | not_started | not_started | reference_only |
| ENV-STORM-03 | Storm Spire — Spire Crown | foundation | not_started | not_started | not_started | reference_only |
| ENV-ECLIPSE-01 | Eclipse Core — Outer Shell | foundation | not_started | not_started | not_started | reference_only |
| ENV-ECLIPSE-02 | Eclipse Core — Memory Lattice | foundation | not_started | not_started | not_started | reference_only |
| ENV-ECLIPSE-03 | Eclipse Core — Dawn Engine | foundation | not_started | not_started | not_started | reference_only |

## Handoff requirements

### Visual-development → Blender

Before modeling begins, the asset should have:

- approved turnaround/reference sheet,
- proportion notes,
- material/color callouts,
- articulation notes,
- gameplay-critical silhouettes or weak points,
- known sockets/attachment points,
- damage-state requirements when applicable.

### Blender → Unity

Before export, the asset should satisfy:

- clean transforms,
- intended pivot/origin,
- stable naming,
- validated normals/tangents,
- valid UVs,
- approved rig hierarchy where applicable,
- documented animation clips,
- no native `.blend` dependency inside `UnityProject/Assets`,
- export at expected scale,
- no accidental collision assumptions baked into environment geometry.

### Unity → Gameplay QA

Before an asset reaches `gameplay_integrated`:

- prefab is stable,
- sockets are bound,
- hitboxes/colliders are Unity-authored,
- Animator/VFX hooks consume gameplay events rather than own gameplay timing,
- gameplay remains functional with visual presentation disabled,
- performance has been profiled on the current target baseline.

## Blender production kickoff

The current production branch is `dev/blender-production`.

Character preparation now includes:

- `Docs/blender-production-kickoff.md`
- `Docs/nova-blender-production-brief.md`
- `Docs/echo-blender-production-brief.md`
- `Docs/blender-character-rig-contract.md`
- `Blender/Tools/ns_character_source_setup.py`
- `Blender/Tools/ns_export_character_fbx.py`

Nova remains the first full round-trip asset.

## Initial production priority

Recommended order:

1. Nova sheet and Blender blockout.
2. Nova Unity prefab/rig integration against the greybox controller.
3. Echo sheet/blockout using the same technical lessons while preserving a distinct silhouette.
4. One representative standard enemy and Aegis as pipeline-validation assets.
5. One Skyport modular environment kit.
6. Expand only after the full Blender → Unity → gameplay round trip is stable.

This avoids producing dozens of final assets before the export, rigging, prefab, material, and gameplay contracts have been proven.
