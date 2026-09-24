"""
Nova Striker character FBX validation/export helper.

Example from Blender:
blender Nova_master.blend --python Blender/Tools/ns_export_character_fbx.py -- \
  --character Nova --out /absolute/path/Nova.fbx

This validates naming/transform/socket basics. It does not validate topology,
weights, deformation quality, UVs, or art approval.
"""

import argparse
import os
import sys
import bpy


REQUIRED_SOCKETS = [
    "socket_root",
    "socket_camera_focus",
    "socket_head",
    "socket_chest",
    "socket_back",
    "socket_hand_l",
    "socket_hand_r",
    "socket_weapon_primary",
    "socket_muzzle_primary",
    "socket_muzzle_secondary",
    "socket_ability_origin",
    "socket_foot_l",
    "socket_foot_r",
]


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--character", choices=["Nova", "Echo"], required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument(
        "--include-blockout",
        action="store_true",
        help="Export GEO_BLOCKOUT if no GEO_FINAL mesh exists.",
    )
    return parser.parse_args(argv)


def near(value, target=0.0, tolerance=1e-4):
    return abs(value - target) <= tolerance


def validate_transform(obj):
    errors = []

    if any(not near(v, 0.0) for v in obj.location):
        errors.append(f"{obj.name}: location must be 0,0,0 before export.")

    if any(not near(v, 0.0) for v in obj.rotation_euler):
        errors.append(f"{obj.name}: rotation must be applied before export.")

    if any(not near(v, 1.0) for v in obj.scale):
        errors.append(f"{obj.name}: scale must be 1,1,1 before export.")

    return errors


def objects_in_collection_prefix(prefix):
    result = []
    for collection in bpy.data.collections:
        if collection.name.endswith(prefix):
            result.extend(collection.all_objects)
    return result


def main():
    args = parse_args()
    character = args.character
    rig_name = f"RIG_{character}"

    errors = []
    rig = bpy.data.objects.get(rig_name)

    if rig is None or rig.type != "ARMATURE":
        errors.append(f"Missing canonical armature: {rig_name}")
    else:
        errors.extend(validate_transform(rig))

    for socket_name in REQUIRED_SOCKETS:
        socket = bpy.data.objects.get(socket_name)
        if socket is None:
            errors.append(f"Missing required socket: {socket_name}")

    final_meshes = [
        obj for obj in objects_in_collection_prefix("_GEO_FINAL")
        if obj.type == "MESH"
    ]

    blockout_meshes = [
        obj for obj in objects_in_collection_prefix("_GEO_BLOCKOUT")
        if obj.type == "MESH"
    ]

    meshes = final_meshes
    if not meshes and args.include_blockout:
        meshes = blockout_meshes

    if not meshes:
        errors.append(
            "No export mesh found in a *_GEO_FINAL collection. "
            "Use --include-blockout only for pipeline tests."
        )

    for mesh in meshes:
        errors.extend(validate_transform(mesh))

    if errors:
        print("[Nova Striker] Character export validation FAILED:")
        for error in errors:
            print(" - " + error)
        raise SystemExit(2)

    bpy.ops.object.select_all(action="DESELECT")

    export_objects = [rig] + meshes
    for socket_name in REQUIRED_SOCKETS:
        socket = bpy.data.objects.get(socket_name)
        if socket:
            export_objects.append(socket)

    for obj in export_objects:
        obj.select_set(True)

    bpy.context.view_layer.objects.active = rig

    output_path = os.path.abspath(args.out)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        use_armature_deform_only=False,
        mesh_smooth_type="FACE",
    )

    print(f"[Nova Striker] Exported {character} FBX: {output_path}")


if __name__ == "__main__":
    main()
