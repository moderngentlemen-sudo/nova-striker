"""
Safely remove Blender factory Camera/Cube/Light from an existing Nova Striker
character master file created by an older launcher revision.

The script only operates when the scene is already marked with
nova_striker_character and only removes matching object names/types that are
direct members of Blender's factory "Collection".
"""

import argparse
import sys
import bpy


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--character",
        choices=["Nova", "Echo"],
        required=True,
    )
    return parser.parse_args(argv)


def main():
    args = parse_args()

    scene_character = bpy.context.scene.get(
        "nova_striker_character",
        "",
    )

    if scene_character != args.character:
        raise RuntimeError(
            f"Scene character marker is '{scene_character}', expected "
            f"'{args.character}'. Refusing repair."
        )

    expected = {
        "Camera": "CAMERA",
        "Cube": "MESH",
        "Light": "LIGHT",
    }

    default_collection = bpy.data.collections.get("Collection")
    removed = []

    if default_collection is not None:
        for object_name, object_type in expected.items():
            obj = bpy.data.objects.get(object_name)

            if (
                obj is not None
                and obj.type == object_type
                and obj in default_collection.objects[:]
            ):
                bpy.data.objects.remove(
                    obj,
                    do_unlink=True,
                )
                removed.append(object_name)

        if (
            default_collection.name in bpy.data.collections
            and len(default_collection.objects) == 0
            and len(default_collection.children) == 0
        ):
            bpy.data.collections.remove(default_collection)

    bpy.ops.wm.save_as_mainfile(
        filepath=bpy.data.filepath
    )

    if removed:
        print(
            "[Nova Striker] Repaired character source scene; removed: "
            + ", ".join(removed)
        )
    else:
        print(
            "[Nova Striker] Character source scene was already clean; "
            "no factory objects removed."
        )


if __name__ == "__main__":
    main()
