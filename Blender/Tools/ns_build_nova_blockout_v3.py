"""
Generate Nova production blockout V3.

V3 is the final automated silhouette/proportion pass before articulation review.

Safety:
- preserves RIG_Nova, sockets, guides, references, and GEO_FINAL
- deletes/rebuilds only objects whose names begin with BLOCKOUT_Nova_
- keeps the canonical 1.85 m character scale
- is not final topology, sculpting, UV, weighting, or animation work
"""

import math
import bpy


CHARACTER = "Nova"
HEIGHT = 1.85
RIG_NAME = "RIG_Nova"
COLLECTION_NAME = "NS_Nova_GEO_BLOCKOUT"
GENERATED_PREFIX = "BLOCKOUT_Nova_"


def collection_required(name):
    collection = bpy.data.collections.get(name)
    if collection is None:
        raise RuntimeError(f"Missing required collection: {name}")
    return collection


def clear_generated_blockout(collection):
    for obj in list(collection.objects):
        if obj.name.startswith(GENERATED_PREFIX):
            bpy.data.objects.remove(obj, do_unlink=True)


def material(name, base_color, metallic=0.0, roughness=0.45, emission=None):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)

    mat.diffuse_color = (*base_color, 1.0)
    mat.use_nodes = True

    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_color, 1.0)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness

        if emission is not None:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = (*emission, 1.0)

            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 2.0

    return mat


def move_to_collection(obj, collection):
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def apply_object_transform(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(
        location=False,
        rotation=True,
        scale=True,
    )
    obj.select_set(False)


def parent_to_bone(obj, rig, bone_name):
    if bone_name not in rig.data.bones:
        raise RuntimeError(f"Missing bone {bone_name} on {rig.name}")

    world = obj.matrix_world.copy()
    obj.parent = rig
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    obj.matrix_world = world


def add_cube(
    name,
    location,
    dimensions,
    mat,
    collection,
    rig,
    bone=None,
    bevel=0.025,
    rotation=(0.0, 0.0, 0.0),
):
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions

    if bevel > 0:
        modifier = obj.modifiers.new("BlockoutBevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2

    # Apply dimensions before rotation so angled armor keeps predictable local
    # proportions rather than using an already-rotated world bounding box.
    apply_object_transform(obj)
    obj.rotation_euler = rotation
    apply_object_transform(obj)
    move_to_collection(obj, collection)

    if mat:
        obj.data.materials.append(mat)

    if bone:
        parent_to_bone(obj, rig, bone)

    return obj


def add_uv_sphere(
    name,
    location,
    scale,
    mat,
    collection,
    rig,
    bone=None,
):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=24,
        ring_count=12,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale

    apply_object_transform(obj)
    move_to_collection(obj, collection)

    if mat:
        obj.data.materials.append(mat)

    if bone:
        parent_to_bone(obj, rig, bone)

    return obj


def add_cylinder(
    name,
    location,
    radius,
    depth,
    mat,
    collection,
    rig,
    bone=None,
    rotation=(0.0, 0.0, 0.0),
):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name

    apply_object_transform(obj)
    move_to_collection(obj, collection)

    if mat:
        obj.data.materials.append(mat)

    if bone:
        parent_to_bone(obj, rig, bone)

    return obj


def build_nova_v3():
    scene_character = bpy.context.scene.get("nova_striker_character", "")
    if scene_character != CHARACTER:
        raise RuntimeError(
            f"Scene marker is '{scene_character}', expected '{CHARACTER}'."
        )

    collection = collection_required(COLLECTION_NAME)
    rig = bpy.data.objects.get(RIG_NAME)

    if rig is None or rig.type != "ARMATURE":
        raise RuntimeError(f"Missing canonical armature: {RIG_NAME}")

    clear_generated_blockout(collection)

    armor = material(
        "MAT_Nova_Blockout_Armor",
        (0.74, 0.78, 0.83),
        metallic=0.15,
        roughness=0.32,
    )
    dark = material(
        "MAT_Nova_Blockout_Undersuit",
        (0.035, 0.045, 0.06),
        metallic=0.0,
        roughness=0.58,
    )
    metal = material(
        "MAT_Nova_Blockout_Metal",
        (0.16, 0.19, 0.23),
        metallic=0.72,
        roughness=0.28,
    )
    navy = material(
        "MAT_Nova_Blockout_Navy",
        (0.045, 0.085, 0.15),
        metallic=0.0,
        roughness=0.52,
    )
    blue = material(
        "MAT_Nova_Blockout_Emissive",
        (0.03, 0.28, 0.65),
        metallic=0.15,
        roughness=0.22,
        emission=(0.05, 0.45, 1.0),
    )

    h = HEIGHT

    # Athletic core: preserve protection while reducing mannequin-like bulk.
    add_cube(
        f"{GENERATED_PREFIX}TorsoCore",
        (0.0, -0.008 * h, 0.715 * h),
        (0.235 * h, 0.124 * h, 0.270 * h),
        dark,
        collection,
        rig,
        "spine_02",
        0.038,
    )

    add_cube(
        f"{GENERATED_PREFIX}PelvisCore",
        (0.0, 0.0, 0.515 * h),
        (0.180 * h, 0.118 * h, 0.132 * h),
        dark,
        collection,
        rig,
        "pelvis",
        0.032,
    )

    # Multi-piece chest creates a military V-shape instead of a single box.
    chest_tilt = math.radians(7.0)

    add_cube(
        f"{GENERATED_PREFIX}ChestCenter",
        (0.0, -0.040 * h, 0.752 * h),
        (0.120 * h, 0.096 * h, 0.145 * h),
        armor,
        collection,
        rig,
        "spine_03",
        0.035,
    )

    for side, sign in (("L", 1.0), ("R", -1.0)):
        add_cube(
            f"{GENERATED_PREFIX}ChestPlate_{side}",
            (sign * 0.085 * h, -0.032 * h, 0.765 * h),
            (0.145 * h, 0.096 * h, 0.138 * h),
            armor,
            collection,
            rig,
            "spine_03",
            0.035,
            rotation=(0.0, sign * chest_tilt, 0.0),
        )

    add_cube(
        f"{GENERATED_PREFIX}AbdomenUpper",
        (0.0, -0.026 * h, 0.655 * h),
        (0.205 * h, 0.086 * h, 0.090 * h),
        armor,
        collection,
        rig,
        "spine_02",
        0.026,
    )

    add_cube(
        f"{GENERATED_PREFIX}AbdomenLower",
        (0.0, -0.020 * h, 0.590 * h),
        (0.175 * h, 0.078 * h, 0.065 * h),
        armor,
        collection,
        rig,
        "spine_01",
        0.022,
    )

    add_cube(
        f"{GENERATED_PREFIX}ChestEnergyCore",
        (0.0, -0.095 * h, 0.735 * h),
        (0.052 * h, 0.016 * h, 0.052 * h),
        blue,
        collection,
        rig,
        "spine_03",
        0.010,
    )

    add_cube(
        f"{GENERATED_PREFIX}WaistSoftGoods",
        (0.0, 0.004 * h, 0.548 * h),
        (0.195 * h, 0.115 * h, 0.042 * h),
        navy,
        collection,
        rig,
        "pelvis",
        0.018,
    )

    # Smaller head and helmet with a subtle forward combat bias. The shell
    # remains inside the canonical 1.85 m envelope.
    add_uv_sphere(
        f"{GENERATED_PREFIX}HeadCore",
        (0.0, -0.012 * h, 0.930 * h),
        (0.068 * h, 0.060 * h, 0.062 * h),
        dark,
        collection,
        rig,
        "head",
    )

    add_cube(
        f"{GENERATED_PREFIX}HelmetShell",
        (0.0, -0.016 * h, 0.930 * h),
        (0.145 * h, 0.112 * h, 0.130 * h),
        armor,
        collection,
        rig,
        "head",
        0.032,
    )

    add_cube(
        f"{GENERATED_PREFIX}Visor",
        (0.0, -0.078 * h, 0.940 * h),
        (0.098 * h, 0.014 * h, 0.035 * h),
        blue,
        collection,
        rig,
        "head",
        0.008,
    )

    # Tighter power/comms package.
    add_cube(
        f"{GENERATED_PREFIX}BackModule",
        (0.0, 0.072 * h, 0.730 * h),
        (0.145 * h, 0.045 * h, 0.165 * h),
        metal,
        collection,
        rig,
        "spine_03",
        0.025,
    )

    sides = [
        (
            "L",
            1.0,
            "upperarm_l",
            "lowerarm_l",
            "hand_l",
            "thigh_l",
            "calf_l",
            "foot_l",
        ),
        (
            "R",
            -1.0,
            "upperarm_r",
            "lowerarm_r",
            "hand_r",
            "thigh_r",
            "calf_r",
            "foot_r",
        ),
    ]

    pauldron_tilt = math.radians(11.0)

    for side, sign, upperarm, lowerarm, hand, thigh, calf, foot in sides:
        shoulder_x = sign * 0.145 * h
        upperarm_x = sign * 0.185 * h
        forearm_x = sign * 0.295 * h
        hand_x = sign * 0.365 * h
        leg_x = sign * 0.078 * h

        upperarm_tilt = math.radians(-sign * 50.0)
        forearm_tilt = math.radians(-sign * 39.0)

        # Angular, lower-profile pauldrons.
        add_cube(
            f"{GENERATED_PREFIX}Shoulder_{side}",
            (shoulder_x, -0.003 * h, 0.792 * h),
            (0.115 * h, 0.086 * h, 0.050 * h),
            armor,
            collection,
            rig,
            upperarm,
            0.018,
            rotation=(0.0, sign * pauldron_tilt, 0.0),
        )

        # Slightly longer-looking arms and lower hand position.
        add_cube(
            f"{GENERATED_PREFIX}UpperArm_{side}",
            (upperarm_x, 0.0, 0.745 * h),
            (0.070 * h, 0.080 * h, 0.180 * h),
            dark,
            collection,
            rig,
            upperarm,
            0.022,
            rotation=(0.0, upperarm_tilt, 0.0),
        )

        add_cube(
            f"{GENERATED_PREFIX}ForearmArmor_{side}",
            (forearm_x, -0.004 * h, 0.635 * h),
            (0.075 * h, 0.086 * h, 0.150 * h),
            armor,
            collection,
            rig,
            lowerarm,
            0.022,
            rotation=(0.0, forearm_tilt, 0.0),
        )

        add_cube(
            f"{GENERATED_PREFIX}Hand_{side}",
            (hand_x, -0.003 * h, 0.565 * h),
            (0.055 * h, 0.064 * h, 0.070 * h),
            dark,
            collection,
            rig,
            hand,
            0.016,
        )

        # Longer, leaner lower-body rhythm while keeping canonical height.
        add_cube(
            f"{GENERATED_PREFIX}Thigh_{side}",
            (leg_x, 0.0, 0.390 * h),
            (0.103 * h, 0.118 * h, 0.220 * h),
            dark,
            collection,
            rig,
            thigh,
            0.032,
        )

        add_cube(
            f"{GENERATED_PREFIX}ThighArmor_{side}",
            (leg_x, -0.036 * h, 0.415 * h),
            (0.116 * h, 0.052 * h, 0.160 * h),
            armor,
            collection,
            rig,
            thigh,
            0.024,
        )

        add_cube(
            f"{GENERATED_PREFIX}KneePlate_{side}",
            (leg_x, -0.040 * h, 0.267 * h),
            (0.094 * h, 0.045 * h, 0.052 * h),
            armor,
            collection,
            rig,
            calf,
            0.016,
        )

        add_cube(
            f"{GENERATED_PREFIX}Shin_{side}",
            (leg_x, 0.0, 0.185 * h),
            (0.095 * h, 0.108 * h, 0.200 * h),
            dark,
            collection,
            rig,
            calf,
            0.028,
        )

        add_cube(
            f"{GENERATED_PREFIX}ShinArmor_{side}",
            (leg_x, -0.038 * h, 0.185 * h),
            (0.108 * h, 0.052 * h, 0.150 * h),
            armor,
            collection,
            rig,
            calf,
            0.022,
        )

        add_cube(
            f"{GENERATED_PREFIX}Boot_{side}",
            (leg_x, -0.036 * h, 0.050 * h),
            (0.110 * h, 0.175 * h, 0.082 * h),
            metal,
            collection,
            rig,
            foot,
            0.022,
        )

    # Forearm-integrated cannon: its long axis now follows the right forearm
    # instead of pointing straight out of the character's front silhouette.
    cannon_tilt = math.radians(39.0)

    add_cylinder(
        f"{GENERATED_PREFIX}ArmCannonBody",
        (-0.315 * h, -0.020 * h, 0.610 * h),
        0.045 * h,
        0.240 * h,
        metal,
        collection,
        rig,
        "lowerarm_r",
        rotation=(0.0, cannon_tilt, 0.0),
    )

    add_cylinder(
        f"{GENERATED_PREFIX}ArmCannonEmitter",
        (-0.400 * h, -0.020 * h, 0.505 * h),
        0.033 * h,
        0.032 * h,
        blue,
        collection,
        rig,
        "lowerarm_r",
        rotation=(0.0, cannon_tilt, 0.0),
    )

    bpy.context.scene["nova_striker_nova_blockout_version"] = "3.0"
    bpy.context.scene["nova_striker_nova_blockout_generated"] = True
    bpy.context.scene["nova_striker_nova_blockout_final_automated_pass"] = True

    bpy.ops.object.select_all(action="DESELECT")
    generated = [
        obj
        for obj in collection.objects
        if obj.name.startswith(GENERATED_PREFIX)
    ]

    for obj in generated:
        obj.select_set(True)

    if generated:
        bpy.context.view_layer.objects.active = generated[0]

    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)

    print(
        f"[Nova Striker] Nova silhouette blockout V3 generated: "
        f"{len(generated)} objects. "
        "V3 is the final automated proportion pass before articulation review."
    )


if __name__ == "__main__":
    build_nova_v3()
