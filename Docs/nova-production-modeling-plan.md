# Nova Production Modeling Plan

Asset ID: `CHR-NOVA`
Current blockout baseline: Nova Blockout V3
Current articulation baseline: V3.2.1

## Entry gate

Production modeling begins when:

- `concept = approved`
- `sheet = approved`
- `blender_blockout = approved`

The blockout gate is now approved. The remaining entry blocker is the Nova
reference/turnaround sheet.

## Production modeling sequence

### Pass 1 — body and undersuit base

Build clean production body/undersuit forms around the approved V3 proportions.

Requirements:

- preserve 1.85 m overall scale,
- preserve shoulder-to-waist taper,
- preserve athletic rather than bodybuilder proportions,
- add believable chest, pelvis, thigh, and calf depth,
- maintain joint clearance proven by the V3.2.1 articulation review.

### Pass 2 — primary armor shell

Replace blockout plates with production armor:

- chest center and left/right breastplate architecture,
- abdomen segmentation,
- pelvis/waist protection,
- tactical shoulder pauldrons,
- forearm and shin armor,
- reduced-mass tactical boots,
- compact back power/comms module.

Do not merge pieces in ways that remove articulation clearance.

### Pass 3 — helmet system

Create modular head states:

- sealed combat helmet,
- visor,
- open/unhelmeted presentation interface,
- comms / AR-HUD language.

The helmet must remain swappable without changing the base skeleton.

### Pass 4 — integrated right-arm cannon

Build the cannon as a Strike Suit subsystem rather than a separate prop.

Support:

- precision configuration,
- higher-impact configuration,
- sensor/scope attachment,
- underbarrel attachment region,
- tactical-light region,
- EMP-module region.

Preserve exact gameplay socket authority in Unity.

### Pass 5 — secondary tactical details

Add restrained field-ready detail:

- navy soft-goods zones,
- belt/pouch language,
- titanium/dark structural parts,
- panel breaks,
- access/service panels,
- fasteners and functional seams.

Avoid decorative noise that weakens gameplay readability.

### Pass 6 — topology / deformation preparation

Before finalizing topology:

- test shoulders through full aim range,
- test elbows with cannon orientation,
- test hips/knees through crouch and Powerslide,
- test wall poses,
- test revive reach,
- verify helmet/chest clearance.

### Pass 7 — UV/material IDs

Prepare:

- clean UVs,
- consistent texel density,
- armor / undersuit / metal / soft-goods / emissive material IDs,
- modular team/class marking surfaces,
- no permanently baked Strike Team number.

### Pass 8 — production rig and skinning

Only after final mesh review:

- bind to the approved character skeleton or approved production successor,
- preserve exact socket names,
- skin for the validated movement envelope,
- repeat the V3.2.1 articulation gate on the skinned production mesh.

### Pass 9 — Unity round trip

Export with the project FBX conventions and validate:

- scale 1,
- correct axes,
- no leaf bones,
- stable socket hierarchy,
- `NovaVisualRoot` presentation integration,
- no change to gameplay authority.

## Non-negotiable inheritance from Blockout V3

Do not silently change:

- Nova's approved height,
- military/Sentinel identity,
- narrower helmet proportion,
- segmented chest/abdomen hierarchy,
- integrated arm-cannon silhouette,
- athletic lower-body proportions,
- reduced boot mass,
- compact back module,
- articulation clearance proven by V3.2.1.

Any material silhouette change should return to review before replacing the
approved blockout as production authority.
