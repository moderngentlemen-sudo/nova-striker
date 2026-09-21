# Blender Source Workspace

This folder is the source-of-truth workspace for Nova Striker 3D modeling and authored skeletal animation.

Suggested structure:

```
Blender/
├── Characters/
│   ├── Nova/
│   └── Echo/
├── Enemies/
├── Guardians/
│   ├── Aegis/
│   ├── Cinder/
│   ├── Mycel/
│   ├── Rime/
│   ├── Tempest/
│   └── Null/
├── Weapons/
├── Environment/
│   ├── Skyport/
│   ├── EmberWorks/
│   ├── VerdantVault/
│   ├── CryoRelay/
│   ├── StormSpire/
│   └── EclipseCore/
├── Props/
└── Shared/
```

Do not put `.blend` files inside `UnityProject/Assets`.

Use `Docs/blender-unity-art-pipeline.md` for scale, naming, export, weak-point, rigging, and Unity integration rules.
