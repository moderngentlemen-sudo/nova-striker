# Nova Striker — Unity Gameplay Branch

Branch: `dev/unity-gameplay`

This branch is dedicated to migrating Nova Striker's gameplay systems into Unity while preserving the existing HTML build as a playable reference.

## Preserved reference

The inherited root file:

`nova-striker-v0.10-visual-completion-playable-preview.html`

remains the rollback/reference implementation for movement, combat, enemies, bosses, co-op behavior, progression, and presentation.

## Unity direction

The Unity branch separates gameplay logic from presentation:

- **Gameplay:** movement, aiming, charge tiers, dash/Velocity Break, Powerslide, wall movement, melee, parry, weapons, enemies, Guardians, co-op, Style, Mastery, checkpoints, and progression.
- **Graphics/runtime:** Unity 2D rendering, animation, particles/VFX, lighting, camera, UI, audio, and mobile/App Store packaging.
- **Data:** ScriptableObjects for weapons, Guardians, enemy archetypes, tuning values, and progression.

The first milestone is a greybox gameplay room with Nova's complete movement and combat feel before final art is introduced.

See `Docs/gameplay-migration.md` for the migration sequence.
