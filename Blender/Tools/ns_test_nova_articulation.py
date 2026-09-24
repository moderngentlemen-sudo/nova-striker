"""
Nova Striker — Nova articulation / armor-clearance test.

Creates a dedicated, removable test action on RIG_Nova with representative
gameplay poses. It does not modify edit bones, mesh topology, sockets, reference
collections, GEO_FINAL, or gameplay data.

The generated action is for Blender production review only and is not exported
by the current FBX helper (bake_anim=False).
"""

import argparse
import math
import sys
import bpy


CHARACTER = "Nova"
RIG_NAME = "RIG_Nova"
ACTION_NAME = "TEST_Nova_Articulation_v1"
MARKER_PREFIX = "NS_TEST_NOVA_"

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


def deg(value):
    return math.radians(value)


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--clear",
        action="store_true",
        help="Remove the generated articulation action and test markers.",
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
        bone
        for bone in REQUIRED_BONES
        if bone not in rig.pose.bones
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


def neutral(rig):
    pass


def aim_forward(rig):
    set_rot(rig, "spine_02", x=-5)
    set_rot(rig, "spine_03", x=-7)
    set_rot(rig, "clavicle_r", x=-12, z=-7)
    set_rot(rig, "upperarm_r", x=-58, z=10)
    set_rot(rig, "lowerarm_r", x=-18)
    set_rot(rig, "hand_r", x=6)
    set_rot(rig, "clavicle_l", x=-5, z=5)
    set_rot(rig, "upperarm_l", x=-18, z=-10)


def aim_up(rig):
    set_rot(rig, "spine_02", x=5)
    set_rot(rig, "spine_03", x=10)
    set_rot(rig, "neck", x=-5)
    set_rot(rig, "head", x=-12)
    set_rot(rig, "clavicle_r", x=-18, z=-8)
    set_rot(rig, "upperarm_r", x=-72, y=-12, z=8)
    set_rot(rig, "lowerarm_r", x=-24)


def aim_down(rig):
    set_rot(rig, "spine_02", x=-12)
    set_rot(rig, "spine_03", x=-12)
    set_rot(rig, "neck", x=6)
    set_rot(rig, "head", x=10)
    set_rot(rig, "clavicle_r", x=-5, z=-5)
    set_rot(rig, "upperarm_r", x=-38, y=10, z=8)
    set_rot(rig, "lowerarm_r", x=28)


def deep_crouch(rig):
    set_loc(rig, "root", z=-0.23)
    set_rot(rig, "pelvis", x=12)
    set_rot(rig, "spine_01", x=-10)
    set_rot(rig, "spine_02", x=-12)
    set_rot(rig, "spine_03", x=-8)

    for side in ("l", "r"):
        set_rot(rig, f"thigh_{side}", x=68)
        set_rot(rig, f"calf_{side}", x=-112)
        set_rot(rig, f"foot_{side}", x=42)

    set_rot(rig, "upperarm_r", x=-28, z=8)
    set_rot(rig, "lowerarm_r", x=-20)
    set_rot(rig, "upperarm_l", x=-22, z=-8)
    set_rot(rig, "lowerarm_l", x=-35)


def powerslide(rig):
    set_loc(rig, "root", y=-0.16, z=-0.28)
    set_rot(rig, "pelvis", x=22, z=-5)
    set_rot(rig, "spine_01", x=-18)
    set_rot(rig, "spine_02", x=-22)
    set_rot(rig, "spine_03", x=-12)

    set_rot(rig, "thigh_l", x=78, z=6)
    set_rot(rig, "calf_l", x=-120)
    set_rot(rig, "foot_l", x=48)

    set_rot(rig, "thigh_r", x=38, z=-8)
    set_rot(rig, "calf_r", x=-62)
    set_rot(rig, "foot_r", x=24)

    set_rot(rig, "upperarm_r", x=-46, z=12)
    set_rot(rig, "lowerarm_r", x=-12)
    set_rot(rig, "upperarm_l", x=12, z=-18)
    set_rot(rig, "lowerarm_l", x=-40)


def dash_lean(rig):
    set_loc(rig, "root", y=-0.08, z=-0.04)
    set_rot(rig, "pelvis", x=-6)
    set_rot(rig, "spine_01", x=-16)
    set_rot(rig, "spine_02", x=-18)
    set_rot(rig, "spine_03", x=-14)
    set_rot(rig, "neck", x=8)
    set_rot(rig, "head", x=8)

    set_rot(rig, "thigh_l", x=18)
    set_rot(rig, "calf_l", x=-28)
    set_rot(rig, "thigh_r", x=-18)
    set_rot(rig, "calf_r", x=-15)

    set_rot(rig, "upperarm_r", x=12, z=8)
    set_rot(rig, "lowerarm_r", x=-24)
    set_rot(rig, "upperarm_l", x=-30, z=-8)
    set_rot(rig, "lowerarm_l", x=-18)


def wall_cling(rig):
    set_loc(rig, "root", y=0.08, z=0.03)
    set_rot(rig, "pelvis", x=8)
    set_rot(rig, "spine_02", x=-8)
    set_rot(rig, "spine_03", x=-10)

    set_rot(rig, "upperarm_l", x=-105, z=-18)
    set_rot(rig, "lowerarm_l", x=-48)
    set_rot(rig, "upperarm_r", x=-92, z=18)
    set_rot(rig, "lowerarm_r", x=-38)

    set_rot(rig, "thigh_l", x=36, z=8)
    set_rot(rig, "calf_l", x=-74)
    set_rot(rig, "foot_l", x=35)
    set_rot(rig, "thigh_r", x=58, z=-8)
    set_rot(rig, "calf_r", x=-92)
    set_rot(rig, "foot_r", x=45)


def wall_jump_prep(rig):
    set_loc(rig, "root", y=0.05, z=-0.10)
    set_rot(rig, "pelvis", x=18)
    set_rot(rig, "spine_01", x=-18)
    set_rot(rig, "spine_02", x=-14)
    set_rot(rig, "spine_03", x=-8)

    set_rot(rig, "upperarm_l", x=-72, z=-22)
    set_rot(rig, "lowerarm_l", x=-70)
    set_rot(rig, "upperarm_r", x=-58, z=16)
    set_rot(rig, "lowerarm_r", x=-34)

    for side in ("l", "r"):
        set_rot(rig, f"thigh_{side}", x=74)
        set_rot(rig, f"calf_{side}", x=-118)
        set_rot(rig, f"foot_{side}", x=44)


def cannon_fire(rig):
    set_rot(rig, "pelvis", z=-3)
    set_rot(rig, "spine_01", x=-4, z=3)
    set_rot(rig, "spine_02", x=-7, z=5)
    set_rot(rig, "spine_03", x=-8, z=6)
    set_rot(rig, "clavicle_r", x=-12, z=-10)
    set_rot(rig, "upperarm_r", x=-64, z=8)
    set_rot(rig, "lowerarm_r", x=-10)
    set_rot(rig, "hand_r", x=5)

    set_rot(rig, "upperarm_l", x=16, z=-18)
    set_rot(rig, "lowerarm_l", x=-22)
    set_rot(rig, "thigh_l", x=8)
    set_rot(rig, "thigh_r", x=-8)


def downed(rig):
    set_loc(rig, "root", y=0.05, z=0.22)
    set_rot(rig, "root", x=82, z=10)
    set_rot(rig, "pelvis", x=-8)
    set_rot(rig, "spine_02", x=8)
    set_rot(rig, "neck", x=-12)

    set_rot(rig, "upperarm_l", x=-18, z=-28)
    set_rot(rig, "lowerarm_l", x=-40)
    set_rot(rig, "upperarm_r", x=20, z=25)
    set_rot(rig, "lowerarm_r", x=-30)

    set_rot(rig, "thigh_l", x=32)
    set_rot(rig, "calf_l", x=-58)
    set_rot(rig, "thigh_r", x=-15)
    set_rot(rig, "calf_r", x=-22)


def revive_reach(rig):
    set_loc(rig, "root", y=-0.05, z=-0.16)
    set_rot(rig, "pelvis", x=14)
    set_rot(rig, "spine_01", x=-18)
    set_rot(rig, "spine_02", x=-22)
    set_rot(rig, "spine_03", x=-14)

    set_rot(rig, "thigh_l", x=76)
    set_rot(rig, "calf_l", x=-120)
    set_rot(rig, "foot_l", x=44)
    set_rot(rig, "thigh_r", x=18)
    set_rot(rig, "calf_r", x=-74)

    set_rot(rig, "upperarm_l", x=-74, z=-12)
    set_rot(rig, "lowerarm_l", x=-22)
    set_rot(rig, "hand_l", x=-10)

    set_rot(rig, "upperarm_r", x=-28, z=12)
    set_rot(rig, "lowerarm_r", x=-28)


def coop_sync(rig):
    set_rot(rig, "pelvis", x=-3)
    set_rot(rig, "spine_01", x=-5)
    set_rot(rig, "spine_02", x=-5)
    set_rot(rig, "spine_03", x=-3)

    set_rot(rig, "clavicle_l", z=-10)
    set_rot(rig, "upperarm_l", x=-42, z=-35)
    set_rot(rig, "lowerarm_l", x=-26)

    set_rot(rig, "clavicle_r", z=10)
    set_rot(rig, "upperarm_r", x=-50, z=28)
    set_rot(rig, "lowerarm_r", x=-18)

    set_rot(rig, "thigh_l", x=8, z=4)
    set_rot(rig, "thigh_r", x=-8, z=-4)


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


def clear_test(rig):
    scene = bpy.context.scene
    remove_test_markers(scene)

    if rig.animation_data and rig.animation_data.action:
        if rig.animation_data.action.name == ACTION_NAME:
            rig.animation_data.action = None

    existing = bpy.data.actions.get(ACTION_NAME)
    if existing is not None:
        bpy.data.actions.remove(existing)

    reset_pose(rig)
    scene.frame_start = int(
        scene.get("nova_striker_articulation_prev_frame_start", 1)
    )
    scene.frame_end = int(
        scene.get("nova_striker_articulation_prev_frame_end", 250)
    )
    scene.frame_set(scene.frame_start)

    scene["nova_striker_nova_articulation_test"] = False
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)

    print(
        "[Nova Striker] Removed Nova articulation test action/markers "
        "and restored neutral pose."
    )


def build_test(rig):
    scene = bpy.context.scene

    if not scene.get("nova_striker_nova_articulation_test", False):
        scene["nova_striker_articulation_prev_frame_start"] = scene.frame_start
        scene["nova_striker_articulation_prev_frame_end"] = scene.frame_end

    previous_action = ""
    if rig.animation_data and rig.animation_data.action:
        previous_action = rig.animation_data.action.name

    if previous_action and previous_action != ACTION_NAME:
        scene["nova_striker_articulation_previous_action"] = previous_action

    existing = bpy.data.actions.get(ACTION_NAME)
    if existing is not None:
        if rig.animation_data and rig.animation_data.action == existing:
            rig.animation_data.action = None
        bpy.data.actions.remove(existing)

    action = bpy.data.actions.new(ACTION_NAME)
    rig.animation_data_create()
    rig.animation_data.action = action

    remove_test_markers(scene)

    for frame, label in POSES:
        reset_pose(rig)
        POSE_BUILDERS[label](rig)
        key_all(rig, frame)

        marker_name = (
            f"{MARKER_PREFIX}{frame:03d}_"
            + label.upper().replace(" ", "_").replace("-", "_")
        )
        scene.timeline_markers.new(
            marker_name,
            frame=frame,
        )

    scene.frame_start = POSES[0][0]
    scene.frame_end = POSES[-1][0]
    scene["nova_striker_nova_articulation_test"] = True
    scene["nova_striker_nova_articulation_test_version"] = "1.0"
    scene["nova_striker_nova_articulation_action"] = ACTION_NAME
    scene["nova_striker_nova_articulation_pose_count"] = len(POSES)

    scene.frame_set(POSES[0][0])
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)

    print(
        f"[Nova Striker] Created {ACTION_NAME} with {len(POSES)} "
        f"review poses across frames {POSES[0][0]}-{POSES[-1][0]}."
    )
    print(
        "[Nova Striker] Use the timeline markers to inspect armor/socket "
        "clearance. This action is production-review data, not gameplay timing."
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
