# Nova — Blender Production Brief

Asset ID: `CHR-NOVA`
Canonical source: `Blender/Characters/Nova/Nova_master.blend`

## Design authority

Nova is the ranged Sentinel/protector Striker. His final production direction is
a streamlined frontline-soldier / tactical-offense Strike Suit. The earlier
hunter-like revision is not part of the production direction.

## Proportion target

- male
- target height: 1.85 m
- athletic military build rather than exaggerated bodybuilder mass
- shoulder line should read confidently at side-view gameplay scale
- armor mass must not obscure hip/knee/elbow articulation
- boots and forearms may carry slightly stronger silhouette weight to reinforce
  soldier/protector identity

Do not lock exact triangle density until the first Unity import/profile pass.

## Material language

Primary:

- white / light-gray ceramic armor plates
- black / carbon composite technical undersuit
- restrained navy soft goods
- titanium / dark metal structural components

Energy/presentation:

- blue emissive channels
- blue-tinted visor
- emissive treatment should support gameplay readability without becoming the
  gameplay authority for charge state or hit timing

## Helmet and head states

Plan the head assembly modularly:

- unhelmeted/open state
- sealed full combat helmet
- integrated comms / AR-HUD visual language
- visor must preserve face readability where the approved presentation calls for it

Helmet geometry should be swappable without changing the base skeleton.

## Strike Suit silhouette

Prioritize:

- clean military chest architecture
- readable clavicle/shoulder armor
- protected but mobile abdomen/hip transition
- forearm/gauntlet construction suitable for weapon integration
- heavy-duty boots that still deform cleanly for crouch and Powerslide
- compact back comms/power module
- belt/pouch language that supports a field-ready soldier without excessive noise

Avoid:

- capes/scarves
- hunter trophies
- overlong coat tails
- excessively knight-like armor
- bulky shapes that compromise wall slide, crouch, or aim silhouettes

## Integrated weapon

Nova's primary weapon language is an integrated arm cannon / arm weapon.

The model should support modular presentation of:

- precision configuration
- higher-impact configuration
- scope/sensor attachment
- underbarrel attachment region
- tactical-light region
- EMP-module region

Do not encode fire rate, charge timing, projectile origin authority, or damage in
the mesh. Unity gameplay remains authoritative.

The exported character must provide:

- `socket_weapon_primary`
- `socket_muzzle_primary`
- `socket_muzzle_secondary`
- `socket_ability_origin`

## Gameplay deformation requirements

Blockout review must test at minimum:

- neutral idle
- run/backpedal
- deep crouch
- Powerslide silhouette
- jump/fall
- wall cling/slide
- wall jump
- multidirectional dash
- upper-body omni-aim
- charged ranged firing pose
- melee/counter fallback poses
- hurt/knockback
- downed/revive
- Guardian ability activation
- Team Sync pose space

The armor must tolerate these poses without major plate collision or silhouette
collapse.

## Socket requirements

Required socket names are defined by
`GameplayPresentationContract.Sockets` and must remain exact:

- `socket_root`
- `socket_camera_focus`
- `socket_head`
- `socket_chest`
- `socket_back`
- `socket_hand_l`
- `socket_hand_r`
- `socket_weapon_primary`
- `socket_muzzle_primary`
- `socket_muzzle_secondary`
- `socket_ability_origin`
- `socket_foot_l`
- `socket_foot_r`

## First Blender milestone

The first review file should be a **clean proportion/silhouette blockout**, not a
detailed sculpt.

It should demonstrate:

- 1.85 m scale
- armor mass distribution
- helmet/open-head modularity
- arm-cannon volume
- shoulder/hip articulation clearance
- crouch/Powerslide readability
- stable rig and socket placement
- successful Unity FBX import at scale 1
