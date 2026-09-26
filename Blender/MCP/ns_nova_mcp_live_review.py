"""
Run the Nova articulation review inside the currently connected Blender session.

This file is executed through the Blender MCP `execute_blender_code` tool. It
renders the version-controlled articulation poses without saving the open .blend
file and restores temporary camera/render/frame/action state before returning.

Required globals supplied by the MCP client:
- NS_REPO_ROOT
- NS_OUTPUT_DIR
"""

import importlib.util
import json
from pathlib import Path

import bpy
from mathutils import Vector


LIVE_REVIEW_CAMERA = "NS_MCP_REVIEW_CAMERA"
LIVE_REVIEW_COLLECTION = "NS_MCP_REVIEW"
BLOCKOUT_PREFIX = "BLOCKOUT_Nova_"
REPORT_TEXT = "NS_Nova_Articulation_Report"
TEMP_ACTION_NAME = "TEST_Nova_MCP_Review_Temp"


def load_module(path, module_name):
    spec = importlib.util.spec_from_file_location(
        module_name,
        str(path),
    )
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load Python module: {path}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def review_objects():
    objects = [
        obj
        for obj in bpy.data.objects
        if (
            obj.type == "MESH"
            and obj.name.startswith(BLOCKOUT_PREFIX)
            and not obj.hide_render
        )
    ]
    if not objects:
        raise RuntimeError("No BLOCKOUT_Nova_* review meshes were found.")
    return objects


def world_bounds(objects):
    points = []
    for obj in objects:
        points.extend(
            obj.matrix_world @ Vector(corner)
            for corner in obj.bound_box
        )

    minimum = Vector(
        (
            min(point.x for point in points),
            min(point.y for point in points),
            min(point.z for point in points),
        )
    )
    maximum = Vector(
        (
            max(point.x for point in points),
            max(point.y for point in points),
            max(point.z for point in points),
        )
    )
    return minimum, maximum


def ensure_review_camera():
    collection = bpy.data.collections.get(LIVE_REVIEW_COLLECTION)
    if collection is None:
        collection = bpy.data.collections.new(LIVE_REVIEW_COLLECTION)
        bpy.context.scene.collection.children.link(collection)

    camera = bpy.data.objects.get(LIVE_REVIEW_CAMERA)
    if camera is None:
        camera_data = bpy.data.cameras.new(LIVE_REVIEW_CAMERA)
        camera = bpy.data.objects.new(LIVE_REVIEW_CAMERA, camera_data)
        collection.objects.link(camera)

    if camera.type != "CAMERA":
        raise RuntimeError(
            f"{LIVE_REVIEW_CAMERA} exists but is not a camera."
        )

    camera.data.type = "ORTHO"
    camera.data.lens = 50
    return camera


def remove_review_camera():
    camera = bpy.data.objects.get(LIVE_REVIEW_CAMERA)
    if camera is not None:
        camera_data = camera.data if camera.type == "CAMERA" else None
        bpy.data.objects.remove(camera, do_unlink=True)
        if camera_data is not None and camera_data.users == 0:
            bpy.data.cameras.remove(camera_data)

    collection = bpy.data.collections.get(LIVE_REVIEW_COLLECTION)
    if collection is not None and not collection.objects and not collection.children:
        bpy.data.collections.remove(collection)


def point_camera(camera, target):
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def frame_camera(camera, minimum, maximum, view):
    center = (minimum + maximum) * 0.5
    size = maximum - minimum
    scene = bpy.context.scene
    aspect = scene.render.resolution_x / max(scene.render.resolution_y, 1)

    if view == "Front":
        horizontal = max(size.x, 0.01)
        camera.location = (center.x, minimum.y - 4.0, center.z)
    elif view == "Side":
        horizontal = max(size.y, 0.01)
        camera.location = (maximum.x + 4.0, center.y, center.z)
    else:
        raise ValueError(view)

    point_camera(camera, center)
    vertical = max(size.z, 0.01)
    camera.data.ortho_scale = max(
        vertical,
        horizontal / max(aspect, 0.01),
    ) * 1.22


def sanitize_label(label):
    return (
        label.replace(" ", "")
        .replace("-", "")
        .replace("/", "")
    )


def snapshot_scene(scene, rig):
    display_state = {}
    if hasattr(scene, "display"):
        shading = scene.display.shading
        for name in (
            "light",
            "show_shadows",
            "show_cavity",
            "cavity_type",
        ):
            if hasattr(shading, name):
                display_state[name] = getattr(shading, name)

    world_color = None
    if scene.world is not None:
        world_color = tuple(scene.world.color)

    return {
        "frame_current": scene.frame_current,
        "frame_start": scene.frame_start,
        "frame_end": scene.frame_end,
        "camera": scene.camera,
        "engine": scene.render.engine,
        "resolution_x": scene.render.resolution_x,
        "resolution_y": scene.render.resolution_y,
        "resolution_percentage": scene.render.resolution_percentage,
        "filepath": scene.render.filepath,
        "file_format": scene.render.image_settings.file_format,
        "film_transparent": scene.render.film_transparent,
        "world_color": world_color,
        "display": display_state,
        "action": rig.animation_data.action if rig.animation_data else None,
    }


def restore_scene(scene, rig, snapshot):
    scene.render.engine = snapshot["engine"]
    scene.render.resolution_x = snapshot["resolution_x"]
    scene.render.resolution_y = snapshot["resolution_y"]
    scene.render.resolution_percentage = snapshot["resolution_percentage"]
    scene.render.filepath = snapshot["filepath"]
    scene.render.image_settings.file_format = snapshot["file_format"]
    scene.render.film_transparent = snapshot["film_transparent"]
    scene.camera = snapshot["camera"]
    scene.frame_start = snapshot["frame_start"]
    scene.frame_end = snapshot["frame_end"]

    if scene.world is not None and snapshot["world_color"] is not None:
        scene.world.color = snapshot["world_color"]

    if hasattr(scene, "display"):
        shading = scene.display.shading
        for name, value in snapshot["display"].items():
            try:
                setattr(shading, name, value)
            except Exception:
                pass

    if snapshot["action"] is not None:
        rig.animation_data_create()
        rig.animation_data.action = snapshot["action"]

    scene.frame_set(snapshot["frame_current"])
    bpy.context.view_layer.update()


def configure_render(scene):
    try:
        scene.render.engine = "BLENDER_WORKBENCH"
    except Exception:
        try:
            scene.render.engine = "BLENDER_EEVEE_NEXT"
        except Exception:
            pass

    scene.render.resolution_x = 640
    scene.render.resolution_y = 896
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    if hasattr(scene, "display"):
        try:
            scene.display.shading.light = "STUDIO"
            scene.display.shading.show_shadows = True
            scene.display.shading.show_cavity = True
            scene.display.shading.cavity_type = "BOTH"
        except Exception:
            pass

    if scene.world is not None:
        scene.world.color = (0.035, 0.035, 0.035)


def render_pose(output_dir, camera, frame, label):
    scene = bpy.context.scene
    scene.frame_set(frame)
    bpy.context.view_layer.update()

    objects = review_objects()
    minimum, maximum = world_bounds(objects)
    safe = sanitize_label(label)

    result = {
        "frame": frame,
        "label": label,
        "bounds": {
            "min": list(minimum),
            "max": list(maximum),
            "size": list(maximum - minimum),
        },
        "renders": {},
    }

    for view in ("Front", "Side"):
        frame_camera(camera, minimum, maximum, view)
        filepath = output_dir / f"{frame:03d}_{safe}_{view}.png"
        scene.render.filepath = str(filepath)
        bpy.ops.render.render(write_still=True)
        result["renders"][view.lower()] = filepath.name

    return result


def write_review_index(output_dir, report):
    diagnostics = {
        item.get("label"): item
        for item in report.get("contact_diagnostics", [])
    }

    cards = []
    for pose in report["poses"]:
        label = pose["label"]
        diagnostic = diagnostics.get(label, {})
        status = diagnostic.get("status", "UNKNOWN")
        reason = diagnostic.get("reason", "")
        front = pose["renders"].get("front", "")
        side = pose["renders"].get("side", "")
        cards.append(
            f"""
<section class="pose-card">
  <header><h2>{pose['frame']:03d} — {label}</h2>
  <p><strong>{status}</strong> {reason}</p></header>
  <div class="images">
    <figure><img src="{front}" alt="{label} front"><figcaption>Front</figcaption></figure>
    <figure><img src="{side}" alt="{label} side"><figcaption>Side</figcaption></figure>
  </div>
</section>
"""
        )

    html = f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>Nova MCP Articulation Review</title>
<style>
body{{margin:0;padding:32px;font-family:Arial,sans-serif;background:#17191d;color:#f4f6f8}}
main{{max-width:1400px;margin:0 auto}} .pose-card{{margin:0 0 32px;padding:20px;background:#22262c;border-radius:12px}}
.images{{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}} figure{{margin:0}}
img{{display:block;width:100%;height:auto;background:#111317;border-radius:8px}} figcaption{{padding-top:8px;opacity:.8}}
@media(max-width:900px){{.images{{grid-template-columns:1fr}}}}
</style></head><body><main>
<h1>Nova Striker — MCP-Driven Articulation Review</h1>
<p>Blockout {report['blockout_version']} · articulation {report['articulation_test_version']} · {report['pose_count']} poses · MCP live session</p>
{''.join(cards)}
</main></body></html>"""
    (output_dir / "review-index.html").write_text(html, encoding="utf-8")


def build_temporary_review_action(articulation, rig, scene):
    existing = bpy.data.actions.get(TEMP_ACTION_NAME)
    if existing is not None:
        if rig.animation_data and rig.animation_data.action == existing:
            rig.animation_data.action = None
        bpy.data.actions.remove(existing, do_unlink=True)

    action = bpy.data.actions.new(TEMP_ACTION_NAME)
    rig.animation_data_create()
    rig.animation_data.action = action

    edit_preferences = bpy.context.preferences.edit
    previous_interpolation = edit_preferences.keyframe_new_interpolation_type
    edit_preferences.keyframe_new_interpolation_type = "CONSTANT"

    diagnostics = []
    report_lines = []

    try:
        for frame, label in articulation.POSES:
            scene.frame_set(frame)
            articulation.reset_pose(rig)
            articulation.POSE_BUILDERS[label](rig)

            spec = articulation.POSE_CONTACTS.get(
                label,
                {"mode": "none", "primary": ()},
            )
            primary_names = spec.get("primary", ())
            secondary_names = spec.get("secondary", ())

            if primary_names:
                primary_heights = articulation.plant_contacts_to_floor(
                    rig,
                    primary_names,
                    target_z=0.0,
                )
            else:
                rig.update_tag()
                bpy.context.view_layer.update()
                primary_heights = {}

            secondary_heights = articulation.contact_heights(secondary_names)
            diagnostic = articulation.evaluate_contact(
                label,
                spec,
                primary_heights,
                secondary_heights,
            )
            diagnostics.append(diagnostic)
            report_lines.append(articulation.contact_report_line(diagnostic))
            articulation.key_all(rig, frame)
    finally:
        edit_preferences.keyframe_new_interpolation_type = previous_interpolation

    return action, diagnostics, "\n".join(report_lines)


def remove_temporary_review_action(rig, action):
    if rig.animation_data and rig.animation_data.action == action:
        rig.animation_data.action = None
    if action is not None and action.name in bpy.data.actions:
        bpy.data.actions.remove(action, do_unlink=True)


def run_review():
    repo_root = Path(NS_REPO_ROOT).resolve()
    output_dir = Path(NS_OUTPUT_DIR).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    for child in output_dir.iterdir():
        if child.is_file():
            child.unlink()

    articulation = load_module(
        repo_root / "Blender" / "Tools" / "ns_test_nova_articulation.py",
        "ns_test_nova_articulation_live_mcp",
    )

    articulation.require_nova_scene()
    rig = articulation.require_rig()
    scene = bpy.context.scene
    snapshot = snapshot_scene(scene, rig)
    temp_action = None
    try:
        temp_action, structured_contacts, contact_report = build_temporary_review_action(
            articulation,
            rig,
            scene,
        )

        configure_render(scene)
        camera = ensure_review_camera()
        scene.camera = camera

        poses = [
            render_pose(output_dir, camera, frame, label)
            for frame, label in articulation.POSES
        ]

        report = {
            "schema_version": 3,
            "execution_transport": "mcp_stdio_execute_blender_code",
            "character": "Nova",
            "source_file": bpy.data.filepath,
            "source_saved": False,
            "blockout_version": scene.get("nova_striker_nova_blockout_version", ""),
            "articulation_test_version": "3.1",
            "articulation_action": TEMP_ACTION_NAME,
            "interpolation": "CONSTANT",
            "pose_count": len(poses),
            "poses": poses,
            "contact_diagnostics": structured_contacts,
            "contact_report": contact_report,
        }

        (output_dir / "articulation-report.json").write_text(
            json.dumps(report, indent=2),
            encoding="utf-8",
        )

        summary = [
            "Nova Striker — MCP-Driven Nova Articulation Review",
            "",
            "Execution transport: MCP stdio → execute_blender_code → live Blender",
            f"Blockout version: {report['blockout_version']}",
            f"Articulation version: {report['articulation_test_version']}",
            f"Action: {report['articulation_action']}",
            f"Poses: {report['pose_count']}",
            f"Rendered images: {report['pose_count'] * 2}",
            "Source .blend saved: no",
            "",
            "Structured contact status:",
        ]
        summary.extend(
            f"{item.get('label')}: {item.get('status')} ({item.get('reason')})"
            for item in structured_contacts
        )
        (output_dir / "summary.txt").write_text(
            "\n".join(summary) + "\n",
            encoding="utf-8",
        )
        write_review_index(output_dir, report)

        print(
            json.dumps(
                {
                    "status": "complete",
                    "transport": "mcp_stdio_execute_blender_code",
                    "output": str(output_dir),
                    "pose_count": len(poses),
                    "render_count": len(poses) * 2,
                    "saved_source": False,
                }
            )
        )
    finally:
        remove_review_camera()
        remove_temporary_review_action(rig, temp_action)
        restore_scene(scene, rig, snapshot)


run_review()
