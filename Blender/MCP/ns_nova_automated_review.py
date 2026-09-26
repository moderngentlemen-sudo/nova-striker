"""
Deterministic Nova articulation review renderer.

Runs on a temporary copy of Nova_master.blend. It:
1. ensures TEST_Nova_Articulation_v3_2_1 exists,
2. visits every labeled review pose,
3. renders orthographic front and side images,
4. records per-pose world bounds,
5. embeds the articulation contact diagnostics in a JSON report,
6. writes a short human-readable summary.

This script does not require the MCP bridge to be listening; it is the
deterministic review backend that MCP clients can invoke when connected.
"""

import argparse
import importlib.util
import json
import math
import os
from pathlib import Path
import sys

import bpy
from mathutils import Vector


REVIEW_CAMERA = "NS_REVIEW_CAMERA"
REVIEW_COLLECTION = "NS_REVIEW"
BLOCKOUT_PREFIX = "BLOCKOUT_Nova_"
REPORT_TEXT = "NS_Nova_Articulation_Report"


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output",
        required=True,
    )
    return parser.parse_args(argv)


def repo_root():
    return Path(__file__).resolve().parents[2]


def load_articulation_module():
    path = (
        repo_root()
        / "Blender"
        / "Tools"
        / "ns_test_nova_articulation.py"
    )

    spec = importlib.util.spec_from_file_location(
        "ns_test_nova_articulation",
        str(path),
    )
    if spec is None or spec.loader is None:
        raise RuntimeError(
            f"Could not load articulation helper: {path}"
        )

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
        raise RuntimeError(
            "No BLOCKOUT_Nova_* mesh objects were found."
        )

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


def ensure_review_collection():
    collection = bpy.data.collections.get(
        REVIEW_COLLECTION
    )
    if collection is None:
        collection = bpy.data.collections.new(
            REVIEW_COLLECTION
        )
        bpy.context.scene.collection.children.link(
            collection
        )
    return collection


def ensure_camera():
    collection = ensure_review_collection()
    camera = bpy.data.objects.get(REVIEW_CAMERA)

    if camera is None:
        camera_data = bpy.data.cameras.new(
            REVIEW_CAMERA
        )
        camera = bpy.data.objects.new(
            REVIEW_CAMERA,
            camera_data,
        )
        collection.objects.link(camera)
    elif camera.type != "CAMERA":
        raise RuntimeError(
            f"{REVIEW_CAMERA} exists but is not a camera."
        )

    bpy.context.scene.camera = camera
    camera.data.type = "ORTHO"
    camera.data.lens = 50
    return camera


def point_camera(camera, target):
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat(
        "-Z",
        "Y",
    ).to_euler()


def frame_camera(camera, minimum, maximum, view):
    center = (minimum + maximum) * 0.5
    size = maximum - minimum

    aspect = (
        bpy.context.scene.render.resolution_x
        / bpy.context.scene.render.resolution_y
    )

    if view == "Front":
        horizontal = max(size.x, 0.01)
        camera.location = (
            center.x,
            minimum.y - 4.0,
            center.z,
        )
    elif view == "Side":
        horizontal = max(size.y, 0.01)
        camera.location = (
            maximum.x + 4.0,
            center.y,
            center.z,
        )
    else:
        raise ValueError(view)

    point_camera(
        camera,
        center,
    )

    vertical = max(size.z, 0.01)
    camera.data.ortho_scale = (
        max(
            vertical,
            horizontal / max(aspect, 0.01),
        )
        * 1.22
    )


def configure_render():
    scene = bpy.context.scene

    try:
        scene.render.engine = "BLENDER_WORKBENCH"
    except Exception:
        try:
            scene.render.engine = "BLENDER_EEVEE_NEXT"
        except Exception:
            pass

    scene.render.resolution_x = 768
    scene.render.resolution_y = 1024
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

    scene.world.color = (0.035, 0.035, 0.035)


def sanitize_label(label):
    return (
        label.replace(" ", "")
        .replace("-", "")
        .replace("/", "")
    )


def render_pose(
    output_dir,
    camera,
    frame,
    label,
):
    scene = bpy.context.scene
    scene.frame_set(frame)
    bpy.context.view_layer.update()

    objects = review_objects()
    minimum, maximum = world_bounds(objects)

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

    safe = sanitize_label(label)

    for view in ("Front", "Side"):
        frame_camera(
            camera,
            minimum,
            maximum,
            view,
        )
        filepath = (
            output_dir
            / f"{frame:03d}_{safe}_{view}.png"
        )

        scene.render.filepath = str(filepath)
        bpy.ops.render.render(
            write_still=True,
        )

        result["renders"][view.lower()] = str(
            filepath.name
        )

    return result


def main():
    args = parse_args()
    output_dir = Path(args.output).resolve()
    output_dir.mkdir(
        parents=True,
        exist_ok=True,
    )

    for child in output_dir.iterdir():
        if child.is_file():
            child.unlink()

    articulation = load_articulation_module()
    articulation.require_nova_scene()
    rig = articulation.require_rig()

    scene = bpy.context.scene
    action = (
        rig.animation_data.action
        if rig.animation_data
        else None
    )

    needs_rebuild = (
        action is None
        or action.name != articulation.ACTION_NAME
        or scene.get(
            "nova_striker_nova_articulation_test_version",
            "",
        )
        != "3.2.1"
    )

    if needs_rebuild:
        articulation.build_test(rig)

    configure_render()
    camera = ensure_camera()

    poses = []
    for frame, label in articulation.POSES:
        poses.append(
            render_pose(
                output_dir,
                camera,
                frame,
                label,
            )
        )

    contact_report = ""
    text_block = bpy.data.texts.get(
        REPORT_TEXT
    )
    if text_block is not None:
        contact_report = text_block.as_string()

    structured_contacts = []
    contact_json = scene.get(
        "nova_striker_nova_articulation_contact_json",
        "",
    )
    if contact_json:
        try:
            structured_contacts = json.loads(contact_json)
        except Exception:
            structured_contacts = []

    structured_directions = []
    direction_json = scene.get(
        "nova_striker_nova_articulation_direction_json",
        "",
    )
    if direction_json:
        try:
            structured_directions = json.loads(direction_json)
        except Exception:
            structured_directions = []

    report = {
        "schema_version": 3,
        "character": "Nova",
        "source_file": bpy.data.filepath,
        "blockout_version": scene.get(
            "nova_striker_nova_blockout_version",
            "",
        ),
        "articulation_test_version": scene.get(
            "nova_striker_nova_articulation_test_version",
            "",
        ),
        "articulation_action": scene.get(
            "nova_striker_nova_articulation_action",
            "",
        ),
        "interpolation": scene.get(
            "nova_striker_nova_articulation_interpolation",
            "",
        ),
        "pose_count": len(poses),
        "poses": poses,
        "contact_diagnostics": structured_contacts,
        "direction_diagnostics": structured_directions,
        "contact_report": contact_report,
    }

    report_path = (
        output_dir
        / "articulation-report.json"
    )
    report_path.write_text(
        json.dumps(
            report,
            indent=2,
        ),
        encoding="utf-8",
    )

    diagnostics_by_label = {
        item.get("label"): item
        for item in structured_contacts
    }
    directions_by_label = {
        item.get("label"): item
        for item in structured_directions
    }

    review_cards = []
    for pose in poses:
        label = pose["label"]
        diagnostic = diagnostics_by_label.get(
            label,
            {},
        )
        status = diagnostic.get(
            "status",
            "UNKNOWN",
        )
        reason = diagnostic.get(
            "reason",
            "",
        )
        direction = directions_by_label.get(
            label,
            {},
        )
        direction_status = direction.get(
            "status",
            "NOT_APPLICABLE",
        )
        direction_reason = direction.get(
            "reason",
            "no direction target",
        )
        direction_error = direction.get(
            "error_degrees",
        )
        direction_text = (
            f"{direction_status} {direction_reason}"
        )
        if direction_error is not None:
            direction_text += (
                f" · {direction_error:.2f}° error"
            )

        front = pose["renders"].get(
            "front",
            "",
        )
        side = pose["renders"].get(
            "side",
            "",
        )

        review_cards.append(
            f"""
            <section class="pose-card">
              <header>
                <h2>{pose['frame']:03d} — {label}</h2>
                <p><strong>Contact:</strong> {status} {reason}</p>
                <p><strong>Direction:</strong> {direction_text}</p>
              </header>
              <div class="images">
                <figure>
                  <img src="{front}" alt="{label} front view">
                  <figcaption>Front</figcaption>
                </figure>
                <figure>
                  <img src="{side}" alt="{label} side view">
                  <figcaption>Side</figcaption>
                </figure>
              </div>
            </section>
            """
        )

    review_html = f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Nova Articulation Review</title>
<style>
body {{
  margin: 0;
  padding: 32px;
  font-family: Arial, sans-serif;
  background: #17191d;
  color: #f4f6f8;
}}
main {{
  max-width: 1400px;
  margin: 0 auto;
}}
.summary {{
  margin-bottom: 32px;
}}
.pose-card {{
  margin: 0 0 32px;
  padding: 20px;
  background: #22262c;
  border-radius: 12px;
}}
.pose-card h2 {{
  margin: 0 0 8px;
}}
.pose-card p {{
  margin: 0 0 16px;
}}
.images {{
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}}
figure {{
  margin: 0;
}}
img {{
  display: block;
  width: 100%;
  height: auto;
  background: #111317;
  border-radius: 8px;
}}
figcaption {{
  padding-top: 8px;
  opacity: 0.8;
}}
@media (max-width: 900px) {{
  .images {{
    grid-template-columns: 1fr;
  }}
}}
</style>
</head>
<body>
<main>
  <section class="summary">
    <h1>Nova Striker — Automated Articulation Review</h1>
    <p>Blockout {report['blockout_version']} · articulation {report['articulation_test_version']} · {len(poses)} poses · {len(poses) * 2} renders</p>
  </section>
  {''.join(review_cards)}
</main>
</body>
</html>
"""

    (
        output_dir
        / "review-index.html"
    ).write_text(
        review_html,
        encoding="utf-8",
    )

    summary_lines = [
        "Nova Striker — Automated Nova Articulation Review",
        "",
        f"Blockout version: {report['blockout_version']}",
        f"Articulation version: {report['articulation_test_version']}",
        f"Action: {report['articulation_action']}",
        f"Interpolation: {report['interpolation']}",
        f"Poses: {report['pose_count']}",
        f"Rendered images: {report['pose_count'] * 2}",
        "Visual index: review-index.html",
        "",
        "Contact diagnostics:",
        contact_report.strip() or "(none)",
        "",
        "Structured contact status:",
    ] + [
        (
            f"{item.get('label')}: {item.get('status')} "
            f"({item.get('reason')})"
        )
        for item in structured_contacts
    ] + [
        "",
        "Structured direction status:",
    ] + [
        (
            f"{item.get('label')}: {item.get('status')} "
            f"({item.get('reason')})"
            + (
                f" error={item.get('error_degrees'):.2f}deg"
                if item.get('error_degrees') is not None
                else ""
            )
        )
        for item in structured_directions
    ]

    (
        output_dir
        / "summary.txt"
    ).write_text(
        "\n".join(summary_lines) + "\n",
        encoding="utf-8",
    )

    print(
        "[Nova Striker] Automated articulation review complete."
    )
    print(
        f"[Nova Striker] Output: {output_dir}"
    )
    print(
        f"[Nova Striker] Rendered {len(poses) * 2} images."
    )


if __name__ == "__main__":
    main()
