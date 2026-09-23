# Gameplay-Complete Safe Handoff

Branch: `dev/gameplay-complete`

This file marks a deliberate safe stopping point for continuing the full pre-Blender gameplay run in a new chat.

## Resume objective

Continue building every meaningful gameplay/runtime system that can be implemented with generated/greybox assets. Stop only when production 3D assets, rigs, authored animation, final materials/textures, or environment art become the limiting dependency.

Use:
- `Docs/gameplay-complete-status.md` as the current status source of truth.
- `Docs/gameplay-migration.md` for inherited migration context.
- `Docs/enemy-framework.md` and `Docs/enemy-archetype-mapping.md` for enemy behavior context.
- `Docs/character-presentation-integration.md` for the gameplay-to-art presentation contract.

## Immediate next engineering priorities

1. Run a fresh Unity 6.6 compile/import pass for the large gameplay-complete expansion and fix all compile errors before calling any new subsystem Unity-validated.
2. Rebuild the Mechanics Lab and smoke-test four local players, per-device input, camera framing, revive, Synergy/Sync, Style, Strike Suit abilities, loadouts, Guardian abilities, enemy squad behavior, encounters, bosses, progression, saves, commerce mock flow, and scalability services.
3. Add projectile/enemy pooling and run a hot-path allocation/performance audit for high-density four-player encounters.
4. Deepen named enemy reactions and status interactions, including shock chaining and the remaining browser-derived defensive behaviors.
5. Expand encounter composition across all 18 acts using the campaign catalog, hazards, setpieces, secrets, checkpoint and boss frameworks already on the branch.
6. Refine mini-boss and Guardian mechanics based on Play Mode findings.
7. Complete party-wipe/death/retry, join/leave/reconnect hooks, skill/perk effects, mastery rewards, debug spawning, validation tooling, and final platform/provider abstractions.
8. Finish the final pre-Blender dependency matrix and art/presentation socket audit.

## Design constraints to preserve

- Support 1–4 simultaneous players on one gameplay field.
- Canonical Strike Team roles: Tank, Striker 0, Striker 1, Support.
- Striker 1 is team lead; Striker 0 is second-in-command.
- Nova and Echo are mechanically distinct Strikers.
- Nova follows the Sentinel/protector/ranged-prevention direction.
- Echo follows the Pursuit Protocol/grapple/melee-hunter direction.
- New mechanics may extend beyond the browser prototype when they serve the current project direction.
- Gameplay remains authoritative; animation/VFX/audio observe gameplay events.
- Fixed 60 Hz gameplay simulation; rendering scales independently.
- Target matrix includes Switch/Switch 2, Xbox One/Series, PS4/PS5, Steam Deck, Linux, Android, iOS/iPadOS, macOS, Windows.
- Commerce architecture supports cosmetics, microtransactions and DLC without requiring gameplay-stat purchases.
- Do not claim console-native SDK/store/certification integration without actual licensed SDK access.
- Do not mark systems QA-approved until they have been validated in Unity 6.6 and, where relevant, on physical controllers.

## Safe stop state

All work in this handoff is committed to `dev/gameplay-complete`. There are no intentionally pending partial edits from this handoff.
