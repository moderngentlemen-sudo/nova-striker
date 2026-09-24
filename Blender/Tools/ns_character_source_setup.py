"""
Nova Striker character source-scene setup.

Run inside Blender's Scripting workspace, or from command line:

blender --python Blender/Tools/ns_character_source_setup.py -- --character Nova
blender --python Blender/Tools/ns_character_source_setup.py -- --character Echo

The script is non-destructive: it creates a namespaced character collection and
will refuse to overwrite an existing canonical rig object.
"""

import argparse
import sys
import bpy
from mathutils import Vector


CHARACTER_CONFIG = {
    "Nova": {
        "height": 1.85,
        "rig_name": "RIG_Nova",
        "root_collection": "NS_Nova",
    },
    "Echo": {
        "height": 1.83,
        "rig_name": "RIG_Echo",
        "root_collection": "NS_Echo",
    },
}

SOCKETS = {
    "socket_root": ("root", (0.0, 0.0, 0.0)),
    "socket_camera_focus": ("head", (0.0, -0.10, 0.04)),
    "socket_head": ("head", (0.0, 0.0, 0.08)),
    "socket_chest": ("spine_03", (0.0, -0.10, 0.0)),
    "socket_back": ("spine_03", (0.0, 0.16, 0.0)),
    "socket_hand_l": ("hand_l", (0.0, -0.08, 0.0)),
    "socket_hand_r": ("hand_r", (0.0, -0.08, 0.0)),
    "socket_weapon_primary": ("hand_r", (0.0, -0.14, 0.0)),
    "socket_muzzle_primary": ("hand_r", (0.0, -0.34, 0.0)),
    "socket_muzzle_secondary": ("hand_l", (0.0, -0.30, 0.0)),
    "socket_ability_origin": ("spine_03", (0.0, -0.20, 0.0)),
    "socket_foot_l": ("foot_l", (0.0, -0.12, 0.0)),
    "socket_foot_r": ("foot_r", (0.0, -0.12, 0.0)),
}


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--character",
        choices=sorted(CHARACTER_CONFIG.keys()),
        default="Nova",
    )
    return parser.parse_args(argv)


def ensure_collection(name, parent=None):
    collection = bpy.data.collections.get(name)
    if collection is None:
        collection = bpy.data.collections.new(name)
        if parent is None:
            bpy.context.scene.collection.children.link(collection)
        else:
            parent.children.link(collection)
    return collection


def set_units():
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"


def create_source_collections(root_name):
    root = ensure_collection(root_name)

    child_names = [
        "REFERENCE",
        "GUIDES",
        "GEO_BLOCKOUT",
        "GEO_FINAL",
        "RIG",
        "SOCKETS",
        "EXPORT",
    ]

    children = {}
    for name in child_names:
        full_name = f"{root_name}_{name}"
        children[name] = ensure_collection(full_name, root)

    return root, children


def link_only(obj, collection):
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def create_armature(character, rig_collection, height):
    rig_name = CHARACTER_CONFIG[character]["rig_name"]

    if bpy.data.objects.get(rig_name):
        raise RuntimeError(
            f"{rig_name} already exists. Refusing to overwrite the canonical rig."
        )

    arm_data = bpy.data.armatures.new(rig_name)
    arm_obj = bpy.data.objects.new(rig_name, arm_data)
    rig_collection.objects.link(arm_obj)

    arm_obj.location = (0.0, 0.0, 0.0)
    arm_obj.rotation_euler = (0.0, 0.0, 0.0)
    arm_obj.scale = (1.0, 1.0, 1.0)
    arm_obj.show_in_front = True

    bpy.context.view_layer.objects.active = arm_obj
    arm_obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    h = height

    points = {
        "root": ((0, 0, 0.00*h), (0, 0, 0.08*h)),
        "pelvis": ((0, 0, 0.48*h), (0, 0, 0.57*h)),
        "spine_01": ((0, 0, 0.57*h), (0, 0, 0.66*h)),
        "spine_02": ((0, 0, 0.66*h), (0, 0, 0.75*h)),
        "spine_03": ((0, 0, 0.75*h), (0, 0, 0.83*h)),
        "neck": ((0, 0, 0.83*h), (0, 0, 0.89*h)),
        "head": ((0, 0, 0.89*h), (0, 0, 0.99*h)),
        "clavicle_l": ((0.02*h, 0, 0.80*h), (0.12*h, 0, 0.80*h)),
        "upperarm_l": ((0.12*h, 0, 0.80*h), (0.25*h, 0, 0.69*h)),
        "lowerarm_l": ((0.25*h, 0, 0.69*h), (0.34*h, 0, 0.58*h)),
        "hand_l": ((0.34*h, 0, 0.58*h), (0.39*h, 0, 0.55*h)),
        "clavicle_r": ((-0.02*h, 0, 0.80*h), (-0.12*h, 0, 0.80*h)),
        "upperarm_r": ((-0.12*h, 0, 0.80*h), (-0.25*h, 0, 0.69*h)),
        "lowerarm_r": ((-0.25*h, 0, 0.69*h), (-0.34*h, 0, 0.58*h)),
        "hand_r": ((-0.34*h, 0, 0.58*h), (-0.39*h, 0, 0.55*h)),
        "thigh_l": ((0.08*h, 0, 0.49*h), (0.08*h, 0, 0.29*h)),
        "calf_l": ((0.08*h, 0, 0.29*h), (0.08*h, 0, 0.08*h)),
        "foot_l": ((0.08*h, 0, 0.08*h), (0.08*h, -0.11*h, 0.04*h)),
        "toe_l": ((0.08*h, -0.11*h, 0.04*h), (0.08*h, -0.18*h, 0.04*h)),
        "thigh_r": ((-0.08*h, 0, 0.49*h), (-0.08*h, 0, 0.29*h)),
        "calf_r": ((-0.08*h, 0, 0.29*h), (-0.08*h, 0, 0.08*h)),
        "foot_r": ((-0.08*h, 0, 0.08*h), (-0.08*h, -0.11*h, 0.04*h)),
        "toe_r": ((-0.08*h, -0.11*h, 0.04*h), (-0.08*h, -0.18*h, 0.04*h)),
    }

    parent_map = {
        "pelvis": "root",
        "spine_01": "pelvis",
        "spine_02": "spine_01",
        "spine_03": "spine_02",
        "neck": "spine_03",
        "head": "neck",
        "clavicle_l": "spine_03",
        "upperarm_l": "clavicle_l",
        "lowerarm_l": "upperarm_l",
        "hand_l": "lowerarm_l",
        "clavicle_r": "spine_03",
        "upperarm_r": "clavicle_r",
        "lowerarm_r": "upperarm_r",
        "hand_r": "lowerarm_r",
        "thigh_l": "pelvis",
        "calf_l": "thigh_l",
        "foot_l": "calf_l",
        "toe_l": "foot_l",
        "thigh_r": "pelvis",
        "calf_r": "thigh_r",
        "foot_r": "calf_r",
        "toe_r": "foot_r",
    }

    bones = {}
    for name, (head, tail) in points.items():
        bone = arm_data.edit_bones.new(name)
        bone.head = Vector(head)
        bone.tail = Vector(tail)
        bones[name] = bone

    for child_name, parent_name in parent_map.items():
        bones[child_name].parent = bones[parent_name]
        bones[child_name].use_connect = False

    bpy.ops.object.mode_set(mode="OBJECT")
    arm_obj.select_set(False)
    return arm_obj


def create_socket_empty(name, armature, bone_name, offset, sockets_collection):
    empty = bpy.data.objects.get(name)
    if empty is None:
        empty = bpy.data.objects.new(name, None)
        sockets_collection.objects.link(empty)

    empty.empty_display_type = "SPHERE"
    empty.empty_display_size = 0.035
    empty.parent = armature
    empty.parent_type = "BONE"
    empty.parent_bone = bone_name
    empty.location = offset
    empty.rotation_euler = (0.0, 0.0, 0.0)
    empty.scale = (1.0, 1.0, 1.0)
    return empty


def create_height_guide(character, height, guide_collection):
    name = f"GUIDE_{character}_Height_{height:.2f}m"
    guide = bpy.data.objects.new(name, None)
    guide_collection.objects.link(guide)
    guide.empty_display_type = "PLAIN_AXES"
    guide.empty_display_size = 0.12
    guide.location = (0.55, 0.0, height)

    ground_name = f"GUIDE_{character}_Ground"
    ground = bpy.data.objects.new(ground_name, None)
    guide_collection.objects.link(ground)
    ground.empty_display_type = "CIRCLE"
    ground.empty_display_size = 0.45
    ground.location = (0.0, 0.0, 0.0)


def create_blockout_markers(character, height, collection):
    # Deliberately primitive proportion markers. They are guides, not final topology.
    parts = [
        ("Head", (0.0, 0.0, 0.91*height), (0.18*height, 0.15*height, 0.18*height)),
        ("Torso", (0.0, 0.0, 0.70*height), (0.34*height, 0.18*height, 0.38*height)),
        ("Pelvis", (0.0, 0.0, 0.50*height), (0.28*height, 0.17*height, 0.18*height)),
    ]

    for part, location, dimensions in parts:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
        obj = bpy.context.object
        obj.name = f"BLOCKOUT_{character}_{part}"
        obj.dimensions = dimensions
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        link_only(obj, collection)


def main():
    args = parse_args()
    character = args.character
    config = CHARACTER_CONFIG[character]
    height = config["height"]

    set_units()
    root, collections = create_source_collections(config["root_collection"])
    armature = create_armature(character, collections["RIG"], height)

    for socket_name, (bone_name, offset) in SOCKETS.items():
        create_socket_empty(
            socket_name,
            armature,
            bone_name,
            offset,
            collections["SOCKETS"],
        )

    create_height_guide(character, height, collections["GUIDES"])
    create_blockout_markers(character, height, collections["GEO_BLOCKOUT"])

    bpy.context.scene["nova_striker_character"] = character
    bpy.context.scene["nova_striker_height_m"] = height
    bpy.context.scene["nova_striker_pipeline_version"] = "1.0.0"

    print(
        f"[Nova Striker] Created {character} source scaffold at {height:.2f} m. "
        f"Save manually as Blender/Characters/{character}/{character}_master.blend."
    )


if __name__ == "__main__":
    main()
