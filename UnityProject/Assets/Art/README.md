# Unity Art Runtime Folder

This folder contains Unity-ready exported assets and Unity-owned presentation data.

Recommended runtime structure:

```
Art/
├── Models/
│   ├── Characters/
│   ├── Enemies/
│   ├── Guardians/
│   ├── Weapons/
│   ├── Environment/
│   └── Props/
├── Materials/
├── Textures/
├── Animations/
├── Prefabs/
└── VFX/
```

Native Blender files belong under the repository-level `Blender/` workspace, not here.

Unity owns final materials, animation controllers, prefabs, VFX, lighting, and scene assembly. Blender owns source geometry, rigs, UVs, baked assets, and authored animation clips.
