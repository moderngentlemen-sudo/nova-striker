# Nova Striker — Gameplay Complete Branch

Branch: `dev/gameplay-complete`

This branch is the full pre-production-art gameplay run: complete as much of Nova Striker's one-to-four-player mechanics, combat, enemies, bosses, campaign, progression, commerce, and platform-scalability architecture as possible before Blender-authored production assets become the limiting dependency. The HTML build remains a behavioral reference rather than a ceiling.

## Preserved reference

The inherited root file:

`nova-striker-v0.10-visual-completion-playable-preview.html`

remains the rollback/reference implementation for movement, combat, enemies, bosses, co-op behavior, progression, and presentation.

## Engine and art direction

Nova Striker now uses a split production pipeline:

- **Unity:** gameplay runtime, physics, input, animation state machines, camera, VFX, lighting, UI, audio, co-op, progression, mobile/App Store packaging, and final scene assembly.
- **Blender:** character, enemy, Guardian, weapon, environment, prop, and mechanical modeling; rigging; UVs; baking; and source animation work where 3D animation is the better fit.
- **Gameplay:** movement, aiming, charge tiers, dash/Velocity Break, Powerslide, wall movement, melee, parry, weapons, enemies, Guardians, co-op, Style, Mastery, checkpoints, and progression.
- **Data:** ScriptableObjects for weapons, Guardians, enemy archetypes, tuning values, and progression.

The art pipeline intentionally keeps Blender source files separate from Unity-ready exported assets. Do not rely on Unity importing `.blend` files directly; export stable interchange files into the Unity project instead.

## Unity baseline

The repository-level Unity project is under `UnityProject/` and currently targets **Unity 6.6 (6000.6.2f1)** with Unity Input System 1.20.0.

The first mechanics lab is generated inside the Editor from repository code:

`Nova Striker > Greybox > Build / Refresh Mechanics Lab`

That creates the temporary Nova prefab, projectile prefab, Pulse weapon asset, test room, one-way platforms, damage dummies, parry projectile emitter, camera, and HUD under `Assets/Greybox/`.

## First milestone

The first milestone remains a greybox gameplay room with Nova's complete movement and combat feel before production art is introduced. Blender production can develop in parallel, but gameplay tuning should not wait on final models.

## Documentation

- `Docs/gameplay-migration.md` — gameplay migration sequence
- `Docs/blender-unity-art-pipeline.md` — Blender → Unity modeling, rigging, export, and naming conventions
- `Blender/README.md` — Blender source workspace conventions
- `UnityProject/Assets/Art/README.md` — Unity-facing art organization
- `Docs/asset-production-manifest.md` — human-readable asset pipeline and current status
- `Production/asset-manifest.json` — machine-readable production source of truth

- `Docs/gameplay-complete-run.md` — comprehensive status and remaining pre-Blender scope
