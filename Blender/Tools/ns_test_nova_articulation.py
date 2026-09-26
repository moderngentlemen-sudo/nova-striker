"""
Nova Striker — Nova articulation / armor-clearance test V3.2.1.

Creates a dedicated, removable production-review action on RIG_Nova with
representative gameplay poses.

V3.2.1 preserves the corrected root/contact system and adds world-space pose
direction validation:
- Aim Forward/Up/Down and Cannon Fire align the cannon to explicit world targets
- Wall Cling and Wall Jump Prep align average hand reach toward a canonical +X
  review wall
- direction diagnostics record target/measured vectors and angular error
- geometry-aware floor planting remains authoritative for grounded poses
- contact diagnostics check both absolute floor error and pair spread
- legacy articulation test actions are removed automatically

It does not modify edit bones, mesh topology, sockets, references, GEO_FINAL,
or gameplay data. The current FBX helper uses bake_anim=False, so this review
action is not exported as authored gameplay animation.
"""

import argparse
import math
import json
import sys
import bpy
from mathutils import Matrix, Vector


CHARACTER = "Nova"
RIG_NAME = "RIG_Nova"
ACTION_NAME = "TEST_Nova_Articulation_v3_2_1"
LEGACY_ACTION_NAMES = (
    "TEST_Nova_Articulation_v1",
    "TEST_Nova_Articulation_v2",
    "TEST_Nova_Articulation_v3",
    "TEST_Nova_Articulation_v3_1",
    "TEST_Nova_Articulation_v3_2",
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
    "Neutral": {
        "mode": "grounded_pair",
        "primary": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    },
    "Aim Forward": {
        "mode": "grounded_pair",
        "primary": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    },
    "Aim Up": {
        "mode": "grounded_pair",
        "primary": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    },
    "Aim Down": {
        "mode": "grounded_pair",
        "primary": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    },
    "Deep Crouch": {
        "mode": "grounded_pair",
        "primary": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    },
    "Powerslide": {
        "mode": "primary_contact",
        "primary": ("BLOCKOUT_Nova_Boot_R",),
        "secondary": ("BLOCKOUT_Nova_Boot_L",),
    },
    "Dash Lean": {
        "mode": "motion_pose",
        "primary": (),
    },
    "Wall Cling": {
        "mode": "wall_pose",
        "primary": (),
    },
    "Wall Jump Prep": {
        "mode": "wall_pose",
        "primary": (),
    },
    "Cannon Fire": {
        "mode": "grounded_pair",
        "primary": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    },
    "Downed": {
        "mode": "body_contact",
        "primary": (
            "BLOCKOUT_Nova_TorsoCore",
            "BLOCKOUT_Nova_ChestCenter",
            "BLOCKOUT_Nova_Shoulder_L",
            "BLOCKOUT_Nova_Shoulder_R",
        ),
    },
    "Revive Reach": {
        "mode": "grounded_pair",
        "primary": (
            "BLOCKOUT_Nova_Boot_L",
            "BLOCKOUT_Nova_Boot_R",
        ),
    },
    "Co-op Sync": {
        "mode": "grounded_pair",
        "primary": ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
    },
}

CONTACT_HEIGHT_TOLERANCE = 0.02
CONTACT_PAIR_SPREAD_TOLERANCE = 0.05

CANNON_BODY_NAME = "BLOCKOUT_Nova_ArmCannonBody"
CANNON_EMITTER_NAME = "BLOCKOUT_Nova_ArmCannonEmitter"
CHEST_CENTER_NAME = "BLOCKOUT_Nova_ChestCenter"
HAND_L_NAME = "BLOCKOUT_Nova_Hand_L"
HAND_R_NAME = "BLOCKOUT_Nova_Hand_R"
SHOULDER_L_NAME = "BLOCKOUT_Nova_Shoulder_L"
SHOULDER_R_NAME = "BLOCKOUT_Nova_Shoulder_R"

# Blender character convention: front is -Y; side-view movement/walls are X/Z.
# The review harness uses a +X wall as its canonical wall-contact side.
POSE_DIRECTION_TARGETS = {
    "Aim Forward": {
        "kind": "cannon",
        "target": (0.0, -1.0, 0.0),
        "tolerance_degrees": 6.0,
    },
    "Aim Up": {
        "kind": "cannon",
        "target": (0.0, -0.7660444431, 0.6427876097),
        "tolerance_degrees": 6.0,
    },
    "Aim Down": {
        "kind": "cannon",
        "target": (0.0, -0.7660444431, -0.6427876097),
        "tolerance_degrees": 6.0,
    },
    "Cannon Fire": {
        "kind": "cannon",
        "target": (0.0, -1.0, 0.0),
        "tolerance_degrees": 6.0,
    },
    "Wall Cling": {
        "kind": "wall_reach",
        "target": (1.0, 0.0, 0.0),
        "tolerance_degrees": 18.0,
    },
    "Wall Jump Prep": {
        "kind": "wall_reach",
        "target": (1.0, 0.0, 0.0),
        "tolerance_degrees": 22.0,
    },
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


def _root_world_delta_to_pose_local(rig, world_delta):
    root = rig.pose.bones["root"]

    armature_delta = (
        rig.matrix_world.to_3x3().inverted()
        @ Vector(world_delta)
    )

    rest_axes = root.bone.matrix_local.to_3x3()
    return rest_axes.inverted() @ armature_delta


def set_root_world_offset(rig, x=0.0, y=0.0, z=0.0):
    root = rig.pose.bones["root"]
    root.location = _root_world_delta_to_pose_local(
        rig,
        (x, y, z),
    )


def add_root_world_offset(rig, x=0.0, y=0.0, z=0.0):
    root = rig.pose.bones["root"]
    root.location += _root_world_delta_to_pose_local(
        rig,
        (x, y, z),
    )


def key_all(rig, frame):
    for bone_name in REQUIRED_BONES:
        bone = rig.pose.bones[bone_name]
        bone.keyframe_insert(
            data_path="location",
            frame=frame,
            group=bone_name,
        )
        if bone.rotation_mode == "QUATERNION":
            bone.keyframe_insert(
                data_path="rotation_quaternion",
                frame=frame,
                group=bone_name,
            )
        elif bone.rotation_mode == "AXIS_ANGLE":
            bone.keyframe_insert(
                data_path="rotation_axis_angle",
                frame=frame,
                group=bone_name,
            )
        else:
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

    rig.update_tag()
    bpy.context.view_layer.update()
    heights = contact_heights(object_names)

    if not heights:
        return {}

    lowest = min(heights.values())
    correction = target_z - lowest

    add_root_world_offset(
        rig,
        z=correction,
    )

    rig.update_tag()
    bpy.context.view_layer.update()

    # A second correction absorbs any dependency-graph lag or rest-space
    # conversion drift. This remains a pose-channel translation, not an
    # armature-object transform.
    heights = contact_heights(object_names)
    if heights:
        residual = target_z - min(heights.values())
        if abs(residual) > 1e-5:
            add_root_world_offset(
                rig,
                z=residual,
            )
            rig.update_tag()
            bpy.context.view_layer.update()

    return contact_heights(object_names)


def evaluate_contact(label, spec, primary_heights, secondary_heights):
    mode = spec.get("mode", "none")

    if not primary_heights:
        return {
            "label": label,
            "mode": mode,
            "status": "NOT_APPLICABLE",
            "reason": "no floor normalization",
            "primary": {},
            "secondary": secondary_heights,
            "minimum_height": None,
            "maximum_height": None,
            "spread": None,
        }

    values = list(primary_heights.values())
    minimum = min(values)
    maximum = max(values)
    spread = maximum - minimum

    reasons = []

    if minimum < -CONTACT_HEIGHT_TOLERANCE:
        reasons.append("penetrating")
    elif minimum > CONTACT_HEIGHT_TOLERANCE:
        reasons.append("floating")

    if mode in ("grounded_pair", "kneel_pair"):
        if spread > CONTACT_PAIR_SPREAD_TOLERANCE:
            reasons.append("asymmetric_contact")

        for height in values:
            if abs(height) > CONTACT_HEIGHT_TOLERANCE:
                if height > 0:
                    reasons.append("required_contact_floating")
                else:
                    reasons.append("required_contact_penetrating")
                break

    status = "OK" if not reasons else "REVIEW"

    return {
        "label": label,
        "mode": mode,
        "status": status,
        "reason": ",".join(dict.fromkeys(reasons)) if reasons else "within_tolerance",
        "primary": primary_heights,
        "secondary": secondary_heights,
        "minimum_height": minimum,
        "maximum_height": maximum,
        "spread": spread,
    }


def contact_report_line(diagnostic):
    label = diagnostic["label"]
    status = diagnostic["status"]
    mode = diagnostic["mode"]

    if status == "NOT_APPLICABLE":
        return (
            f"{label}: NOT_APPLICABLE; mode={mode}; "
            f"{diagnostic['reason']}"
        )

    primary = diagnostic["primary"]
    secondary = diagnostic["secondary"]

    primary_items = ", ".join(
        f"{name}={height:.4f}m"
        for name, height in sorted(primary.items())
    )

    secondary_items = ""
    if secondary:
        secondary_items = "; secondary=" + ", ".join(
            f"{name}={height:.4f}m"
            for name, height in sorted(secondary.items())
        )

    return (
        f"{label}: {status}; mode={mode}; "
        f"reason={diagnostic['reason']}; "
        f"min={diagnostic['minimum_height']:.4f}m; "
        f"spread={diagnostic['spread']:.4f}m; "
        f"{primary_items}{secondary_items}"
    )


def world_object_center(object_name):
    obj = bpy.data.objects.get(object_name)
    if obj is None:
        raise RuntimeError(f"Missing direction-review object: {object_name}")

    points = [
        obj.matrix_world @ Vector(corner)
        for corner in obj.bound_box
    ]
    center = Vector((0.0, 0.0, 0.0))
    for point in points:
        center += point
    return center / max(len(points), 1)


def normalized(vector):
    value = Vector(vector)
    if value.length < 1e-8:
        raise RuntimeError("Cannot normalize a near-zero direction vector.")
    value.normalize()
    return value


def angular_error_degrees(measured, target):
    measured = normalized(measured)
    target = normalized(target)
    dot = max(-1.0, min(1.0, measured.dot(target)))
    return math.degrees(math.acos(dot))


def cannon_world_direction():
    return normalized(
        world_object_center(CANNON_EMITTER_NAME)
        - world_object_center(CANNON_BODY_NAME)
    )


def wall_reach_world_direction():
    hand_average = (
        world_object_center(HAND_L_NAME)
        + world_object_center(HAND_R_NAME)
    ) * 0.5

    return normalized(
        hand_average
        - world_object_center(CHEST_CENTER_NAME)
    )


def rotate_pose_bone_world_delta(rig, bone_name, delta_world):
    bone = rig.pose.bones[bone_name]

    rig_world_rotation = rig.matrix_world.to_quaternion().normalized()
    delta_armature = (
        rig_world_rotation.inverted()
        @ delta_world
        @ rig_world_rotation
    )

    head = bone.head.copy()
    transform = (
        Matrix.Translation(head)
        @ delta_armature.to_matrix().to_4x4()
        @ Matrix.Translation(-head)
    )

    bone.rotation_mode = "QUATERNION"
    bone.matrix = transform @ bone.matrix

    rig.update_tag()
    bpy.context.view_layer.update()


def align_cannon_to_world(rig, target_world, iterations=3):
    target = normalized(target_world)

    for _ in range(iterations):
        current = cannon_world_direction()
        error = angular_error_degrees(current, target)
        if error <= 0.25:
            break

        delta_world = current.rotation_difference(target)
        rotate_pose_bone_world_delta(
            rig,
            "lowerarm_r",
            delta_world,
        )

    return cannon_world_direction()


def align_hand_reach_to_world(
    rig,
    bone_name,
    shoulder_name,
    hand_name,
    target_world,
    iterations=3,
):
    target = normalized(target_world)

    for _ in range(iterations):
        current = normalized(
            world_object_center(hand_name)
            - world_object_center(shoulder_name)
        )
        error = angular_error_degrees(current, target)
        if error <= 0.5:
            break

        delta_world = current.rotation_difference(target)
        rotate_pose_bone_world_delta(
            rig,
            bone_name,
            delta_world,
        )


def apply_direction_constraints(rig, label):
    spec = POSE_DIRECTION_TARGETS.get(label)
    if spec is None:
        return

    target = normalized(spec["target"])
    kind = spec["kind"]

    if kind == "cannon":
        align_cannon_to_world(
            rig,
            target,
        )
        return

    if kind == "wall_reach":
        # Wall Jump Prep is a whole-body reorientation before release, not
        # merely an arm reach. Rotate the root toward the canonical wall first
        # so the torso, shoulders, weapon, and legs all present a coherent
        # wall-facing silhouette; then refine each arm independently.
        if label == "Wall Jump Prep":
            current = wall_reach_world_direction()
            delta_world = current.rotation_difference(target)
            rotate_pose_bone_world_delta(
                rig,
                "root",
                delta_world,
            )

        align_hand_reach_to_world(
            rig,
            "upperarm_l",
            SHOULDER_L_NAME,
            HAND_L_NAME,
            target,
            iterations=5,
        )
        align_hand_reach_to_world(
            rig,
            "upperarm_r",
            SHOULDER_R_NAME,
            HAND_R_NAME,
            target,
            iterations=5,
        )
        return

    raise RuntimeError(f"Unknown direction-review kind: {kind}")


def evaluate_direction(label):
    spec = POSE_DIRECTION_TARGETS.get(label)
    if spec is None:
        return {
            "label": label,
            "kind": "none",
            "status": "NOT_APPLICABLE",
            "reason": "no direction target",
            "target": None,
            "measured": None,
            "error_degrees": None,
            "tolerance_degrees": None,
        }

    target = normalized(spec["target"])
    kind = spec["kind"]

    if kind == "cannon":
        measured = cannon_world_direction()
    elif kind == "wall_reach":
        measured = wall_reach_world_direction()
    else:
        raise RuntimeError(f"Unknown direction-review kind: {kind}")

    error = angular_error_degrees(
        measured,
        target,
    )
    tolerance = float(spec["tolerance_degrees"])
    status = "OK" if error <= tolerance else "REVIEW"

    return {
        "label": label,
        "kind": kind,
        "status": status,
        "reason": (
            "within_tolerance"
            if status == "OK"
            else "direction_error"
        ),
        "target": list(target),
        "measured": list(measured),
        "error_degrees": error,
        "tolerance_degrees": tolerance,
    }


def direction_report_line(diagnostic):
    if diagnostic["status"] == "NOT_APPLICABLE":
        return (
            f"{diagnostic['label']}: NOT_APPLICABLE; "
            f"{diagnostic['reason']}"
        )

    return (
        f"{diagnostic['label']}: {diagnostic['status']}; "
        f"kind={diagnostic['kind']}; "
        f"error={diagnostic['error_degrees']:.2f}deg; "
        f"tolerance={diagnostic['tolerance_degrees']:.2f}deg; "
        f"reason={diagnostic['reason']}"
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
    set_root_world_offset(rig, y=-0.10)
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
    set_root_world_offset(rig, y=-0.18)
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
    set_root_world_offset(rig, y=-0.10)
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
    set_root_world_offset(rig, y=0.08, z=0.30)
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
    set_root_world_offset(rig, y=0.04)
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
    # Stable two-foot braced firing stance. Upper-body asymmetry carries the
    # recoil/readability while the lower body stays reliably planted.
    set_root_world_offset(rig, y=-0.03)
    set_rot(rig, "pelvis", x=2)
    set_rot(rig, "spine_01", x=-5, z=2)
    set_rot(rig, "spine_02", x=-7, z=4)
    set_rot(rig, "spine_03", x=-8, z=6)

    set_rot(rig, "clavicle_r", x=-10, z=-10)
    set_rot(rig, "upperarm_r", x=-56, z=8)
    set_rot(rig, "lowerarm_r", x=-9)
    set_rot(rig, "hand_r", x=4)

    set_rot(rig, "upperarm_l", x=12, z=-16)
    set_rot(rig, "lowerarm_l", x=-18)

    # Keep both legs symmetric so the floor-planting pass can establish a true
    # two-foot stance instead of inheriting a small FK height mismatch.
    for side in ("l", "r"):
        set_rot(rig, f"thigh_{side}", x=4)
        set_rot(rig, f"calf_{side}", x=-8)
        set_rot(rig, f"foot_{side}", x=3)


def downed(rig):
    # Side-fall review pose. Body contact, not foot contact, defines the floor.
    set_root_world_offset(rig, y=0.04, z=0.10)
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
    # The starter rig does not yet have IK controls suitable for a reliable
    # one-knee contact solve. Use a low, planted combat-revive stance instead:
    # both feet stay grounded while the torso/left arm carry the reach.
    set_root_world_offset(rig, y=-0.12)
    set_rot(rig, "pelvis", x=10)
    set_rot(rig, "spine_01", x=-16)
    set_rot(rig, "spine_02", x=-20)
    set_rot(rig, "spine_03", x=-13)
    set_rot(rig, "neck", x=5)

    for side in ("l", "r"):
        set_rot(rig, f"thigh_{side}", x=46)
        set_rot(rig, f"calf_{side}", x=-72)
        set_rot(rig, f"foot_{side}", x=24)

    set_rot(rig, "upperarm_l", x=-62, z=-12)
    set_rot(rig, "lowerarm_l", x=-20)
    set_rot(rig, "hand_l", x=-10)

    set_rot(rig, "upperarm_r", x=-26, z=10)
    set_rot(rig, "lowerarm_r", x=-20)


def coop_sync(rig):
    set_root_world_offset(rig, y=-0.02)
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


def write_report(contact_lines, direction_lines):
    report = bpy.data.texts.get(REPORT_NAME)
    if report is None:
        report = bpy.data.texts.new(REPORT_NAME)
    else:
        report.clear()

    report.write(
        "Nova Striker — Nova Articulation Review V3.2.1\n"
        "Generated from the V3 blockout/starter rig.\n"
        "Root translation is converted from world space into the root bone's "
        "pose-local channels before keying.\n"
        "Contact diagnostics check absolute floor error as well as pair spread.\n\n"
    )

    report.write("Contact diagnostics:\n")
    for line in contact_lines:
        report.write(line + "\n")

    report.write("\nDirection diagnostics:\n")
    for line in direction_lines:
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
    scene["nova_striker_nova_articulation_interpolation"] = ""
    scene["nova_striker_nova_articulation_contact_json"] = ""
    scene["nova_striker_nova_articulation_direction_json"] = ""

    bpy.ops.wm.save_as_mainfile(
        filepath=bpy.data.filepath
    )

    print(
        "[Nova Striker] Removed generated Nova articulation test data "
        "and restored the previous action or neutral pose."
    )


def build_test(rig):
    scene = bpy.context.scene

    # Review poses are discrete clearance snapshots, not animation samples.
    # Force new keys to CONSTANT so arbitrary frames between timeline markers
    # cannot produce misleading blended/interpolated poses.
    edit_preferences = bpy.context.preferences.edit
    previous_interpolation = edit_preferences.keyframe_new_interpolation_type
    edit_preferences.keyframe_new_interpolation_type = "CONSTANT"

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
    direction_report_lines = []
    contact_diagnostics = []
    direction_diagnostics = []

    for frame, label in POSES:
        scene.frame_set(frame)
        reset_pose(rig)
        POSE_BUILDERS[label](rig)
        apply_direction_constraints(
            rig,
            label,
        )

        spec = POSE_CONTACTS.get(
            label,
            {
                "mode": "none",
                "primary": (),
            },
        )
        primary_names = spec.get("primary", ())
        secondary_names = spec.get("secondary", ())

        if primary_names:
            primary_heights = plant_contacts_to_floor(
                rig,
                primary_names,
                target_z=0.0,
            )
        else:
            rig.update_tag()
            bpy.context.view_layer.update()
            primary_heights = {}

        secondary_heights = contact_heights(
            secondary_names
        )

        diagnostic = evaluate_contact(
            label,
            spec,
            primary_heights,
            secondary_heights,
        )
        contact_diagnostics.append(diagnostic)

        direction_diagnostic = evaluate_direction(
            label,
        )
        direction_diagnostics.append(
            direction_diagnostic
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
                diagnostic,
            )
        )
        direction_report_lines.append(
            direction_report_line(
                direction_diagnostic,
            )
        )

    scene.frame_start = POSES[0][0]
    scene.frame_end = POSES[-1][0]
    scene["nova_striker_nova_articulation_test"] = True
    scene["nova_striker_nova_articulation_test_version"] = "3.2.1"
    scene["nova_striker_nova_articulation_action"] = ACTION_NAME
    scene["nova_striker_nova_articulation_pose_count"] = len(POSES)
    scene["nova_striker_nova_articulation_floor_planting"] = "world_space_root_channel_v3"
    scene["nova_striker_nova_articulation_contact_json"] = json.dumps(
        contact_diagnostics,
        sort_keys=True,
    )
    scene["nova_striker_nova_articulation_direction_json"] = json.dumps(
        direction_diagnostics,
        sort_keys=True,
    )

    write_report(
        report_lines,
        direction_report_lines,
    )

    scene["nova_striker_nova_articulation_interpolation"] = "CONSTANT"
    scene.frame_set(POSES[0][0])

    # Do not permanently change the user's Blender keyframe preference.
    edit_preferences.keyframe_new_interpolation_type = previous_interpolation

    bpy.ops.wm.save_as_mainfile(
        filepath=bpy.data.filepath
    )

    print(
        f"[Nova Striker] Created {ACTION_NAME} with {len(POSES)} "
        f"review poses across frames {POSES[0][0]}-{POSES[-1][0]}."
    )
    print(
        "[Nova Striker] V3.2.1 uses world-space root translation, geometry-aware "
        "floor planting, corrected Cannon Fire/Revive Reach stances, absolute "
        "contact checks, and CONSTANT pose holds. Review the "
        "NS_Nova_Articulation_Report text block for contact-height diagnostics."
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
