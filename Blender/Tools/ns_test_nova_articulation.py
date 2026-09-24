"""
Nova Striker — Nova articulation / armor-clearance test V2.

Creates a dedicated, removable production-review action on RIG_Nova with
representative gameplay poses.

V2 improves the first test harness with:
- geometry-aware floor planting using the generated boot/knee/body blockout
- calibrated crouch/Powerslide/wall/revive pose limits
- more realistic pelvis/root placement and torso compensation
- a removable text report containing contact-height diagnostics
- automatic cleanup of the legacy V1 test action

It does not modify edit bones, mesh topology, sockets, references, GEO_FINAL,
or gameplay data. The current FBX helper uses bake_anim=False, so this review
action is not exported as authored gameplay animation.
"""

import argparse
import math
import sys
import bpy
from mathutils import Vector


CHARACTER = "Nova"
RIG_NAME = "RIG_Nova"
ACTION_NAME = "TEST_Nova_Articulation_v2"
LEGACY_ACTION_NAMES = (
    "TEST_Nova_Articulation_v1",
)
MARKER_PREFIX = "NS_TEST_NOVA_"
REPORT_NAME = "NS_Nova_Articulation_Report"

REQUIRED_BONES = [
    "root",
    "pelvis",
    "spine_01",
    "spine_02",
    "spine_03",
    "neck",
    "head",
    "clavicle_l",
    "upperarm_l",
    "lowerarm_l",
    "hand_l",
    "clavicle_r",
    "upperarm_r",
    "lowerarm_r",
    "hand_r",
    "thigh_l",
    "calf_l",
    "foot_l",
    "toe_l",
    "thigh_r",
    "calf_r",
    "foot_r",
    "toe_r",
]

POSES = [
    (1, "Neutral"),
    (20, "Aim Forward"),
    (40, "Aim Up"),
    (60, "Aim Down"),
    (80, "Deep Crouch"),
    (100, "Powerslide"),
    (120, "Dash Lean"),
    (140, "Wall Cling"),
    (160, "Wall Jump Prep"),
    (180, "Cannon Fire"),
    (200, "Downed"),
    (220, "Revive Reach"),
    (240, "Co-op Sync"),
]

POSE_CONTACTS = {
    "Neutral": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Aim Forward": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Aim Up": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Aim Down": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Deep Crouch": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Powerslide": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Dash Lean": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Wall Jump Prep": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Cannon Fire": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    "Downed": (
        "BLOCKOUT_Nova_TorsoCore",
        "BLOCKOUT_Nova_ChestCenter",
        "BLOCKOUT_Nova_Shoulder_L",
        "BLOCKOUT_Nova_Shoulder_R",
    ),
    "Revive Reach": (
        "BLOCKOUT_Nova_Boot_R",
        "BLOCKOUT_Nova_KneePlate_L",
    ),
    "Co-op Sync": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
}


def deg(value):
    return math.radians(value)


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--clear",
        action="store_true",
        help="Remove generated Nova articulation test data.",
    )
    return parser.parse_args(argv)


def require_nova_scene():
    scene_character = bpy.context.scene.get(
        "nova_striker_character",
        "",
    )
    if scene_character != CHARACTER:
        raise RuntimeError(
            f"Scene marker is '{scene_character}', expected '{CHARACTER}'."
        )


def require_rig():
    rig = bpy.data.objects.get(RIG_NAME)
    if rig is None or rig.type != "ARMATURE":
        raise RuntimeError(f"Missing canonical armature: {RIG_NAME}")

    missing = [
        bone_name
        for bone_name in REQUIRED_BONES
        if bone_name not in rig.pose.bones
    ]
    if missing:
        raise RuntimeError(
            "Missing required articulation bones: "
            + ", ".join(missing)
        )

    return rig


def remove_test_markers(scene):
    for marker in list(scene.timeline_markers):
        if marker.name.startswith(MARKER_PREFIX):
            scene.timeline_markers.remove(marker)


def remove_test_actions(rig):
    names = (ACTION_NAME,) + LEGACY_ACTION_NAMES

    if rig.animation_data and rig.animation_data.action:
        if rig.animation_data.action.name in names:
            rig.animation_data.action = None

    for action_name in names:
        action = bpy.data.actions.get(action_name)
        if action is not None:
            bpy.data.actions.remove(
                action,
                do_unlink=True,
            )


def remove_report():
    report = bpy.data.texts.get(REPORT_NAME)
    if report is not None:
        bpy.data.texts.remove(
            report,
            do_unlink=True,
        )


def reset_pose(rig):
    for pose_bone in rig.pose.bones:
        pose_bone.rotation_mode = "XYZ"
        pose_bone.location = (0.0, 0.0, 0.0)
        pose_bone.rotation_euler = (0.0, 0.0, 0.0)
        pose_bone.scale = (1.0, 1.0, 1.0)


def set_rot(rig, bone_name, x=0.0, y=0.0, z=0.0):
    bone = rig.pose.bones[bone_name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = (
        deg(x),
        deg(y),
        deg(z),
    )


def set_loc(rig, bone_name, x=0.0, y=0.0, z=0.0):
    rig.pose.bones[bone_name].location = (x, y, z)


def key_all(rig, frame):
    for bone_name in REQUIRED_BONES:
        bone = rig.pose.bones[bone_name]
        bone.keyframe_insert(
            data_path="location",
            frame=frame,
            group=bone_name,
        )
        bone.keyframe_insert(
            data_path="rotation_euler",
            frame=frame,
            group=bone_name,
        )
        bone.keyframe_insert(
            data_path="scale",
            frame=frame,
            group=bone_name,
        )


def world_min_z(obj):
    return min(
        (obj.matrix_world @ Vector(corner)).z
        for corner in obj.bound_box
    )


def contact_heights(object_names):
    heights = {}
    for object_name in object_names:
        obj = bpy.data.objects.get(object_name)
        if obj is not None and obj.type == "MESH":
            heights[object_name] = world_min_z(obj)
    return heights


def plant_contacts_to_floor(rig, object_names, target_z=0.0):
    if not object_names:
        return {}

    bpy.context.view_layer.update()
    heights = contact_heights(object_names)

    if not heights:
        return {}

    lowest = min(heights.values())
    root = rig.pose.bones["root"]
    root.location.z += target_z - lowest

    bpy.context.view_layer.update()
    return contact_heights(object_names)


def contact_report_line(label, heights):
    if not heights:
        return f"{label}: no geometry floor-contact normalization"

    values = list(heights.values())
    spread = max(values) - min(values)
    items = ", ".join(
        f"{name}={height:.4f}m"
        for name, height in sorted(heights.items())
    )

    status = "OK"
    if spread > 0.08:
        status = "REVIEW"

    return (
        f"{label}: {status}; contact spread={spread:.4f}m; "
        f"{items}"
    )


def neutral(rig):
    # Bind-pose baseline.
    pass


def aim_forward(rig):
    set_rot(rig, "spine_01", x=-3)
    set_rot(rig, "spine_02", x=-5)
    set_rot(rig, "spine_03", x=-5)
    set_rot(rig, "clavicle_r", x=-8, z=-6)
    set_rot(rig, "upperarm_r", x=-48, z=9)
    set_rot(rig, "lowerarm_r", x=-14)
    set_rot(rig, "hand_r", x=4)
    set_rot(rig, "clavicle_l", x=-4, z=5)
    set_rot(rig, "upperarm_l", x=-14, z=-9)


def aim_up(rig):
    set_rot(rig, "spine_01", x=3)
    set_rot(rig, "spine_02", x=6)
    set_rot(rig, "spine_03", x=8)
    set_rot(rig, "neck", x=-4)
    set_rot(rig, "head", x=-9)
    set_rot(rig, "clavicle_r", x=-12, z=-7)
    set_rot(rig, "upperarm_r", x=-64, y=-8, z=8)
    set_rot(rig, "lowerarm_r", x=-20)


def aim_down(rig):
    set_rot(rig, "spine_01", x=-6)
    set_rot(rig, "spine_02", x=-9)
    set_rot(rig, "spine_03", x=-9)
    set_rot(rig, "neck", x=5)
    set_rot(rig, "head", x=8)
    set_rot(rig, "clavicle_r", x=-4, z=-4)
    set_rot(rig, "upperarm_r", x=-32, y=7, z=7)
    set_rot(rig, "lowerarm_r", x=22)


def deep_crouch(rig):
    # Athletic gameplay crouch rather than an anatomical-limit squat.
    set_loc(rig, "root", y=-0.10)
    set_rot(rig, "pelvis", x=7)
    set_rot(rig, "spine_01", x=-12)
    set_rot(rig, "spine_02", x=-15)
    set_rot(rig, "spine_03", x=-9)
    set_rot(rig, "neck", x=5)

    for side in ("l", "r"):
        set_rot(rig, f"thigh_{side}", x=52)
        set_rot(rig, f"calf_{side}", x=-84)
        set_rot(rig, f"foot_{side}", x=28)

    set_rot(rig, "upperarm_r", x=-30, z=8)
    set_rot(rig, "lowerarm_r", x=-18)
    set_rot(rig, "upperarm_l", x=-20, z=-8)
    set_rot(rig, "lowerarm_l", x=-28)


def powerslide(rig):
    # Low forward center of mass, one leg carrying more compression.
    set_loc(rig, "root", y=-0.18)
    set_rot(rig, "pelvis", x=14, z=-4)
    set_rot(rig, "spine_01", x=-16)
    set_rot(rig, "spine_02", x=-19)
    set_rot(rig, "spine_03", x=-11)
    set_rot(rig, "neck", x=6)

    set_rot(rig, "thigh_l", x=56, z=5)
    set_rot(rig, "calf_l", x=-92)
    set_rot(rig, "foot_l", x=30)

    set_rot(rig, "thigh_r", x=24, z=-6)
    set_rot(rig, "calf_r", x=-48)
    set_rot(rig, "foot_r", x=16)

    set_rot(rig, "upperarm_r", x=-42, z=11)
    set_rot(rig, "lowerarm_r", x=-12)
    set_rot(rig, "upperarm_l", x=10, z=-15)
    set_rot(rig, "lowerarm_l", x=-32)


def dash_lean(rig):
    set_loc(rig, "root", y=-0.10)
    set_rot(rig, "pelvis", x=-4)
    set_rot(rig, "spine_01", x=-13)
    set_rot(rig, "spine_02", x=-16)
    set_rot(rig, "spine_03", x=-12)
    set_rot(rig, "neck", x=7)
    set_rot(rig, "head", x=6)

    set_rot(rig, "thigh_l", x=16)
    set_rot(rig, "calf_l", x=-24)
    set_rot(rig, "thigh_r", x=-14)
    set_rot(rig, "calf_r", x=-12)

    set_rot(rig, "upperarm_r", x=10, z=7)
    set_rot(rig, "lowerarm_r", x=-20)
    set_rot(rig, "upperarm_l", x=-26, z=-7)
    set_rot(rig, "lowerarm_l", x=-16)


def wall_cling(rig):
    # Suspended pose: no floor normalization is applied.
    set_loc(rig, "root", y=0.08, z=0.30)
    set_rot(rig, "pelvis", x=6)
    set_rot(rig, "spine_01", x=-5)
    set_rot(rig, "spine_02", x=-7)
    set_rot(rig, "spine_03", x=-8)

    set_rot(rig, "upperarm_l", x=-82, z=-16)
    set_rot(rig, "lowerarm_l", x=-42)
    set_rot(rig, "upperarm_r", x=-74, z=16)
    set_rot(rig, "lowerarm_r", x=-34)

    set_rot(rig, "thigh_l", x=32, z=6)
    set_rot(rig, "calf_l", x=-62)
    set_rot(rig, "foot_l", x=28)
    set_rot(rig, "thigh_r", x=48, z=-6)
    set_rot(rig, "calf_r", x=-78)
    set_rot(rig, "foot_r", x=34)


def wall_jump_prep(rig):
    set_loc(rig, "root", y=0.04)
    set_rot(rig, "pelvis", x=12)
    set_rot(rig, "spine_01", x=-13)
    set_rot(rig, "spine_02", x=-12)
    set_rot(rig, "spine_03", x=-7)

    set_rot(rig, "upperarm_l", x=-62, z=-18)
    set_rot(rig, "lowerarm_l", x=-55)
    set_rot(rig, "upperarm_r", x=-50, z=14)
    set_rot(rig, "lowerarm_r", x=-28)

    for side in ("l", "r"):
        set_rot(rig, f"thigh_{side}", x=58)
        set_rot(rig, f"calf_{side}", x=-92)
        set_rot(rig, f"foot_{side}", x=31)


def cannon_fire(rig):
    set_loc(rig, "root", y=-0.03)
    set_rot(rig, "pelvis", z=-2)
    set_rot(rig, "spine_01", x=-4, z=2)
    set_rot(rig, "spine_02", x=-6, z=4)
    set_rot(rig, "spine_03", x=-7, z=5)

    set_rot(rig, "clavicle_r", x=-9, z=-9)
    set_rot(rig, "upperarm_r", x=-54, z=8)
    set_rot(rig, "lowerarm_r", x=-8)
    set_rot(rig, "hand_r", x=4)

    set_rot(rig, "upperarm_l", x=13, z=-15)
    set_rot(rig, "lowerarm_l", x=-18)
    set_rot(rig, "thigh_l", x=6)
    set_rot(rig, "thigh_r", x=-6)


def downed(rig):
    # Side-fall review pose. Body contact, not foot contact, defines the floor.
    set_loc(rig, "root", y=0.04, z=0.10)
    set_rot(rig, "root", x=76, z=8)
    set_rot(rig, "pelvis", x=-6)
    set_rot(rig, "spine_02", x=6)
    set_rot(rig, "neck", x=-9)

    set_rot(rig, "upperarm_l", x=-16, z=-24)
    set_rot(rig, "lowerarm_l", x=-32)
    set_rot(rig, "upperarm_r", x=18, z=22)
    set_rot(rig, "lowerarm_r", x=-24)

    set_rot(rig, "thigh_l", x=26)
    set_rot(rig, "calf_l", x=-46)
    set_rot(rig, "thigh_r", x=-12)
    set_rot(rig, "calf_r", x=-18)


def revive_reach(rig):
    # One-knee kneel with forward reach rather than a symmetric deep squat.
    set_loc(rig, "root", y=-0.08)
    set_rot(rig, "pelvis", x=9)
    set_rot(rig, "spine_01", x=-13)
    set_rot(rig, "spine_02", x=-17)
    set_rot(rig, "spine_03", x=-11)

    set_rot(rig, "thigh_l", x=64)
    set_rot(rig, "calf_l", x=-102)
    set_rot(rig, "foot_l", x=34)

    set_rot(rig, "thigh_r", x=28)
    set_rot(rig, "calf_r", x=-56)
    set_rot(rig, "foot_r", x=18)

    set_rot(rig, "upperarm_l", x=-58, z=-10)
    set_rot(rig, "lowerarm_l", x=-18)
    set_rot(rig, "hand_l", x=-8)

    set_rot(rig, "upperarm_r", x=-24, z=10)
    set_rot(rig, "lowerarm_r", x=-22)


def coop_sync(rig):
    set_loc(rig, "root", y=-0.02)
    set_rot(rig, "pelvis", x=-2)
    set_rot(rig, "spine_01", x=-4)
    set_rot(rig, "spine_02", x=-4)
    set_rot(rig, "spine_03", x=-2)

    set_rot(rig, "clavicle_l", z=-8)
    set_rot(rig, "upperarm_l", x=-36, z=-30)
    set_rot(rig, "lowerarm_l", x=-22)

    set_rot(rig, "clavicle_r", z=8)
    set_rot(rig, "upperarm_r", x=-42, z=24)
    set_rot(rig, "lowerarm_r", x=-15)

    set_rot(rig, "thigh_l", x=6, z=3)
    set_rot(rig, "thigh_r", x=-6, z=-3)


POSE_BUILDERS = {
    "Neutral": neutral,
    "Aim Forward": aim_forward,
    "Aim Up": aim_up,
    "Aim Down": aim_down,
    "Deep Crouch": deep_crouch,
    "Powerslide": powerslide,
    "Dash Lean": dash_lean,
    "Wall Cling": wall_cling,
    "Wall Jump Prep": wall_jump_prep,
    "Cannon Fire": cannon_fire,
    "Downed": downed,
    "Revive Reach": revive_reach,
    "Co-op Sync": coop_sync,
}


def write_report(lines):
    report = bpy.data.texts.get(REPORT_NAME)
    if report is None:
        report = bpy.data.texts.new(REPORT_NAME)
    else:
        report.clear()

    report.write(
        "Nova Striker — Nova Articulation Review V2\n"
        "Generated from the V3 blockout/starter rig.\n"
        "Contact diagnostics are review aids, not pass/fail gameplay data.\n\n"
    )

    for line in lines:
        report.write(line + "\n")


def restore_previous_action(rig, scene):
    previous_name = scene.get(
        "nova_striker_articulation_previous_action",
        "",
    )
    if previous_name:
        previous_action = bpy.data.actions.get(previous_name)
        if previous_action is not None:
            rig.animation_data_create()
            rig.animation_data.action = previous_action
            return True
    return False


def clear_test(rig):
    scene = bpy.context.scene
    remove_test_markers(scene)
    remove_test_actions(rig)
    remove_report()

    restored = restore_previous_action(rig, scene)
    if not restored:
        reset_pose(rig)

    scene.frame_start = int(
        scene.get("nova_striker_articulation_prev_frame_start", 1)
    )
    scene.frame_end = int(
        scene.get("nova_striker_articulation_prev_frame_end", 250)
    )
    scene.frame_set(scene.frame_start)

    scene["nova_striker_nova_articulation_test"] = False
    scene["nova_striker_nova_articulation_test_version"] = ""
    scene["nova_striker_nova_articulation_action"] = ""
    scene["nova_striker_nova_articulation_pose_count"] = 0

    bpy.ops.wm.save_as_mainfile(
        filepath=bpy.data.filepath
    )

    print(
        "[Nova Striker] Removed Nova articulation V1/V2 test data "
        "and restored the previous action or neutral pose."
    )


def build_test(rig):
    scene = bpy.context.scene

    if not scene.get("nova_striker_nova_articulation_test", False):
        scene["nova_striker_articulation_prev_frame_start"] = scene.frame_start
        scene["nova_striker_articulation_prev_frame_end"] = scene.frame_end

    if rig.animation_data and rig.animation_data.action:
        current_name = rig.animation_data.action.name
        test_names = (ACTION_NAME,) + LEGACY_ACTION_NAMES
        if current_name not in test_names:
            scene["nova_striker_articulation_previous_action"] = current_name

    remove_test_actions(rig)
    remove_test_markers(scene)

    action = bpy.data.actions.new(ACTION_NAME)
    rig.animation_data_create()
    rig.animation_data.action = action

    report_lines = []

    for frame, label in POSES:
        scene.frame_set(frame)
        reset_pose(rig)
        POSE_BUILDERS[label](rig)

        contacts = POSE_CONTACTS.get(label, ())
        final_heights = plant_contacts_to_floor(
            rig,
            contacts,
            target_z=0.0,
        )

        key_all(rig, frame)

        marker_name = (
            f"{MARKER_PREFIX}{frame:03d}_"
            + label.upper().replace(" ", "_").replace("-", "_")
        )
        scene.timeline_markers.new(
            marker_name,
            frame=frame,
        )

        report_lines.append(
            contact_report_line(
                label,
                final_heights,
            )
        )

    scene.frame_start = POSES[0][0]
    scene.frame_end = POSES[-1][0]
    scene["nova_striker_nova_articulation_test"] = True
    scene["nova_striker_nova_articulation_test_version"] = "2.0"
    scene["nova_striker_nova_articulation_action"] = ACTION_NAME
    scene["nova_striker_nova_articulation_pose_count"] = len(POSES)
    scene["nova_striker_nova_articulation_floor_planting"] = "geometry_aware"

    write_report(report_lines)

    scene.frame_set(POSES[0][0])
    bpy.ops.wm.save_as_mainfile(
        filepath=bpy.data.filepath
    )

    print(
        f"[Nova Striker] Created {ACTION_NAME} with {len(POSES)} "
        f"review poses across frames {POSES[0][0]}-{POSES[-1][0]}."
    )
    print(
        "[Nova Striker] V2 uses geometry-aware floor planting and calibrated "
        "pose limits. Review the NS_Nova_Articulation_Report text block for "
        "contact-height diagnostics."
    )


def main():
    args = parse_args()
    require_nova_scene()
    rig = require_rig()

    if args.clear:
        clear_test(rig)
    else:
        build_test(rig)


if __name__ == "__main__":
    main()
