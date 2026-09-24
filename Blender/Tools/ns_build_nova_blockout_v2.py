"""
Generate Nova production blockout V2.

This refinement pass:
- preserves RIG_Nova, sockets, guides, references, and GEO_FINAL
- deletes/rebuilds only objects whose names begin with BLOCKOUT_Nova_
- pushes Nova toward the approved military / Sentinel silhouette
- is not final topology, sculpting, UVs, weights, or production modeling
"""

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
    bevel=0.03,
):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions

    if bevel > 0:
        modifier = obj.modifiers.new("BlockoutBevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2

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
        vertices=20,
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


def build_nova_v2():
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

    # Stronger V-shaped torso with less front/back bulk.
    add_cube(
        f"{GENERATED_PREFIX}TorsoCore",
        (0.0, 0.0, 0.705 * h),
        (0.26 * h, 0.145 * h, 0.29 * h),
        dark,
        collection,
        rig,
        "spine_02",
        0.045,
    )

    add_cube(
        f"{GENERATED_PREFIX}PelvisCore",
        (0.0, 0.0, 0.505 * h),
        (0.215 * h, 0.135 * h, 0.15 * h),
        dark,
        collection,
        rig,
        "pelvis",
        0.04,
    )

    add_cube(
        f"{GENERATED_PREFIX}ChestPlate",
        (0.0, -0.028 * h, 0.755 * h),
        (0.36 * h, 0.12 * h, 0.17 * h),
        armor,
        collection,
        rig,
        "spine_03",
        0.055,
    )

    add_cube(
        f"{GENERATED_PREFIX}AbdomenPlate",
        (0.0, -0.020 * h, 0.635 * h),
        (0.21 * h, 0.10 * h, 0.12 * h),
        armor,
        collection,
        rig,
        "spine_01",
        0.035,
    )

    add_cube(
        f"{GENERATED_PREFIX}ChestEnergyCore",
        (0.0, -0.095 * h, 0.735 * h),
        (0.058 * h, 0.018 * h, 0.058 * h),
        blue,
        collection,
        rig,
        "spine_03",
        0.012,
    )

    add_cube(
        f"{GENERATED_PREFIX}WaistSoftGoods",
        (0.0, 0.008 * h, 0.545 * h),
        (0.22 * h, 0.13 * h, 0.05 * h),
        navy,
        collection,
        rig,
        "pelvis",
        0.02,
    )

    # Narrower tactical helmet and visor.
    add_uv_sphere(
        f"{GENERATED_PREFIX}HeadCore",
        (0.0, 0.0, 0.925 * h),
        (0.082 * h, 0.072 * h, 0.105 * h),
        dark,
        collection,
        rig,
        "head",
    )

    add_cube(
        f"{GENERATED_PREFIX}HelmetShell",
        (0.0, -0.002 * h, 0.936 * h),
        (0.175 * h, 0.135 * h, 0.19 * h),
        armor,
        collection,
        rig,
        "head",
        0.04,
    )

    add_cube(
        f"{GENERATED_PREFIX}Visor",
        (0.0, -0.080 * h, 0.942 * h),
        (0.120 * h, 0.016 * h, 0.045 * h),
        blue,
        collection,
        rig,
        "head",
        0.01,
    )

    # Compact back power/comms module.
    add_cube(
        f"{GENERATED_PREFIX}BackModule",
        (0.0, 0.090 * h, 0.728 * h),
        (0.16 * h, 0.055 * h, 0.18 * h),
        metal,
        collection,
        rig,
        "spine_03",
        0.03,
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

    for side, sign, upperarm, lowerarm, hand, thigh, calf, foot in sides:
        shoulder_x = sign * 0.200 * h
        upperarm_x = sign * 0.250 * h
        forearm_x = sign * 0.325 * h
        hand_x = sign * 0.382 * h
        leg_x = sign * 0.080 * h

        # Flatter tactical pauldrons.
        add_cube(
            f"{GENERATED_PREFIX}Shoulder_{side}",
            (shoulder_x, 0.0, 0.792 * h),
            (0.125 * h, 0.11 * h, 0.07 * h),
            armor,
            collection,
            rig,
            upperarm,
            0.03,
        )

        add_cube(
            f"{GENERATED_PREFIX}UpperArm_{side}",
            (upperarm_x, 0.0, 0.690 * h),
            (0.082 * h, 0.090 * h, 0.215 * h),
            dark,
            collection,
            rig,
            upperarm,
            0.03,
        )

        add_cube(
            f"{GENERATED_PREFIX}ForearmArmor_{side}",
            (forearm_x, -0.004 * h, 0.585 * h),
            (0.095 * h, 0.105 * h, 0.18 * h),
            armor,
            collection,
            rig,
            lowerarm,
            0.035,
        )

        add_cube(
            f"{GENERATED_PREFIX}Hand_{side}",
            (hand_x, -0.003 * h, 0.535 * h),
            (0.060 * h, 0.070 * h, 0.075 * h),
            dark,
            collection,
            rig,
            hand,
            0.02,
        )

        # More athletic leg rhythm with clearer knee separation.
        add_cube(
            f"{GENERATED_PREFIX}Thigh_{side}",
            (leg_x, 0.0, 0.385 * h),
            (0.115 * h, 0.13 * h, 0.300 * h),
            dark,
            collection,
            rig,
            thigh,
            0.04,
        )

        add_cube(
            f"{GENERATED_PREFIX}ThighArmor_{side}",
            (leg_x, -0.040 * h, 0.405 * h),
            (0.130 * h, 0.060 * h, 0.195 * h),
            armor,
            collection,
            rig,
            thigh,
            0.03,
        )

        add_cube(
            f"{GENERATED_PREFIX}KneePlate_{side}",
            (leg_x, -0.045 * h, 0.265 * h),
            (0.105 * h, 0.050 * h, 0.060 * h),
            armor,
            collection,
            rig,
            calf,
            0.02,
        )

        add_cube(
            f"{GENERATED_PREFIX}Shin_{side}",
            (leg_x, 0.0, 0.165 * h),
            (0.105 * h, 0.120 * h, 0.235 * h),
            dark,
            collection,
            rig,
            calf,
            0.035,
        )

        add_cube(
            f"{GENERATED_PREFIX}ShinArmor_{side}",
            (leg_x, -0.045 * h, 0.165 * h),
            (0.120 * h, 0.060 * h, 0.185 * h),
            armor,
            collection,
            rig,
            calf,
            0.03,
        )

        add_cube(
            f"{GENERATED_PREFIX}Boot_{side}",
            (leg_x, -0.045 * h, 0.055 * h),
            (0.125 * h, 0.205 * h, 0.095 * h),
            metal,
            collection,
            rig,
            foot,
            0.03,
        )

    # Slimmer, longer integrated right-arm cannon.
    add_cylinder(
        f"{GENERATED_PREFIX}ArmCannonBody",
        (-0.332 * h, -0.085 * h, 0.588 * h),
        0.058 * h,
        0.30 * h,
        metal,
        collection,
        rig,
        "lowerarm_r",
        rotation=(1.57079632679, 0.0, 0.0),
    )

    add_cylinder(
        f"{GENERATED_PREFIX}ArmCannonEmitter",
        (-0.332 * h, -0.238 * h, 0.588 * h),
        0.036 * h,
        0.050 * h,
        blue,
        collection,
        rig,
        "lowerarm_r",
        rotation=(1.57079632679, 0.0, 0.0),
    )

    bpy.context.scene["nova_striker_nova_blockout_version"] = "2.0"
    bpy.context.scene["nova_striker_nova_blockout_generated"] = True

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
        f"[Nova Striker] Nova silhouette blockout V2 generated: "
        f"{len(generated)} objects. "
        "This is a proportion/silhouette review pass."
    )


if __name__ == "__main__":
    build_nova_v2()
