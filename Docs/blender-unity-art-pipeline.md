# Blender → Unity Art Pipeline

## Purpose

Blender is the source-DCC for Nova Striker's production models. Unity remains the runtime and final assembly environment.

The pipeline separates:

1. editable Blender source files,
2. baked texture source/output,
3. Unity-ready model exports,
4. Unity materials, prefabs, animation controllers, VFX, and scene integration.

This avoids a fragile dependency on Unity importing native `.blend` files.

## Coordinate and scale conventions

- Blender units: Metric, Unit Scale 1.0.
- Author at real-world scale where practical.
- 1 Blender unit = 1 meter.
- Unity import scale target = 1.
- Blender uses Z-up; Unity uses Y-up.
- For FBX export use:
  - Forward: `-Z Forward`
  - Up: `Y Up`
  - Apply Transform: enabled when it preserves the intended orientation
  - Apply Unit: enabled
- Apply object scale before export: `Ctrl+A → Scale`.
- Do not apply armature pose transforms destructively after animation work begins.

## Source layout

Use:

`Blender/Characters/`
`Blender/Enemies/`
`Blender/Guardians/`
`Blender/Weapons/`
`Blender/Environment/`
`Blender/Props/`
`Blender/Shared/`

Each production asset should have one canonical source `.blend` file.

Examples:

`Blender/Characters/Nova/Nova_master.blend`
`Blender/Guardians/Aegis/Aegis_master.blend`
`Blender/Environment/Skyport/Skyport_TransitSpine_master.blend`

## Unity exports

Export runtime assets under:

`UnityProject/Assets/Art/Models/`

Recommended interchange formats:

- **FBX** for skinned characters, rigs, and animation clips.
- **FBX or glTF/GLB** for rigid environment/prop meshes depending on the Unity package/tooling chosen.
- **PNG/TGA** for runtime textures.
- Keep Blender source textures outside the runtime folder when they are not needed by Unity.

Do not place native `.blend` files under `UnityProject/Assets`.

## Naming

Meshes:

- `CHR_Nova_Body`
- `CHR_Echo_Body`
- `ENM_Interceptor_Body`
- `BOS_Aegis_Body`
- `WPN_RailLance`
- `ENV_Skyport_GlassBridge_A`
- `PRP_Ember_PressureTank_A`

Armatures:

- `RIG_Nova`
- `RIG_Aegis`

Materials:

- `MAT_Nova_Armor`
- `MAT_Aegis_Shield`
- `MAT_Skyport_Glass`

Textures:

- `T_Nova_Armor_BaseColor`
- `T_Nova_Armor_Normal`
- `T_Nova_Armor_MRA` (metallic/roughness/AO packing if the selected Unity shader uses it)

Animations:

- `ANIM_Nova_Run`
- `ANIM_Nova_Dash_T3`
- `ANIM_Aegis_ShieldRush`

## Character runtime presentation contract

Unity now provides a `StrikerPresentationBridge` and separate `NovaVisualRoot` / `EchoVisualRoot` attachment points on the player prefab. Production character exports should be integrated under those roots rather than replacing the gameplay root.

The bridge:

- swaps the active Nova/Echo visual root from `NovaCombatController.Character`,
- forwards movement/combat state to optional Animator parameters,
- converts `GameplayEventHub` cues into optional Animator triggers,
- rotates the visual root for left/right facing,
- does not own physics, hitboxes, Counter windows, dash timing, or damage.

See `Docs/character-presentation-integration.md` for the full Animator parameter/trigger contract and integration checklist.

## Character modeling

Nova and Echo should be production 3D designs that preserve the gameplay silhouettes established in the HTML reference.

Priorities:

- readable shoulder/helmet silhouette,
- clearly visible weapon arm,
- readable energy core,
- clean crouch/Powerslide profile,
- geometry that deforms well at hips, shoulders, elbows, knees, and spine,
- distinct Nova/Echo silhouettes rather than color swaps.

Rigging should support:

- idle/run/backpedal,
- crouch,
- jump/fall/land,
- wall cling/slide/jump,
- omni-aim upper-body rotation,
- dash and Powerslide,
- three-hit ground melee,
- aerial melee,
- parry/perfect parry,
- hurt/knockback/downed/revive,
- Guardian ability activation,
- co-op Sync and finishers.

## Guardians

Each Guardian model must visually expose its gameplay mechanic:

- **Aegis:** central shield core and deployable defense structures.
- **Cinder:** reactor and side/rear heat vents.
- **Mycel:** bloom crown and node connection points.
- **Rime:** three removable crystal armor plates plus the inner crystal core.
- **Tempest:** wing/capacitor assemblies that can visibly charge and expose vulnerability.
- **Null:** segmented floating body around a singularity core.

Weak points should be separate named meshes or bones where practical so Unity can animate/material-swap them independently.

## Environment kit strategy

Build modular kits rather than one giant mesh per level.

For each sector include:

- floor/wall modules,
- one-way platform modules,
- foreground framing props,
- midground architecture,
- background landmark pieces,
- destructibles,
- hazard housings,
- traversal machinery,
- boss-arena-specific assets.

Unity owns collision. Blender meshes should not be treated as the authoritative gameplay collision source.

## LOD and mobile considerations

Nova Striker is intended to support mobile/App Store deployment, so production modeling should be efficient.

Start with:

- Hero/Guardian meshes: detail where silhouette and deformation matter.
- Standard enemies: lower complexity than heroes.
- Environment: modular and aggressively instanced.
- Avoid invisible geometry.
- Reuse materials where possible.
- Prefer baked detail over unnecessary geometry.
- Plan LODs for large environment pieces and Guardians when profiling demonstrates value.

Final triangle budgets should be set after a Unity device-performance baseline exists rather than guessed in advance.

## Materials and shaders

Blender materials are authoring previews only.

Unity owns final runtime shaders, including:

- energy emissives,
- shield refraction,
- Null distortion,
- ice/glass,
- molten surfaces,
- vegetation,
- damage-state material swaps,
- hit flashes and weak-point pulses.

Do not build gameplay assumptions around Blender material node graphs.

## Animation ownership

Use Blender for authored skeletal animation and Unity for:

- animation state machines,
- blending,
- procedural aim offsets,
- gameplay timing,
- event-driven VFX,
- hit reaction selection,
- animation cancellation logic.

Gameplay code must remain authoritative for hit windows, parry windows, dash duration, and damage timing.

## Version control

Binary art files should use Git LFS. The repository includes LFS attributes for common model and texture formats.

Before adding large assets, contributors should have Git LFS installed and initialized locally.

## Review gates

Before an asset is considered Unity-ready:

1. transforms and scale are clean,
2. pivot/origin is intentional,
3. mesh names follow conventions,
4. normals/tangents are correct,
5. UVs are valid,
6. rig hierarchy is stable,
7. animation clip ranges/names are documented,
8. exported asset imports into Unity at scale 1,
9. prefab/material setup is separate from the source model,
10. mobile performance is profiled before final complexity is locked.
