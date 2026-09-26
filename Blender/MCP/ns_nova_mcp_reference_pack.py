
"""
Generate Nova's production-reference source pack inside the connected Blender
session through MCP.

This is a review/export operation only. It does not save Nova_master.blend and
restores the user's scene, action, pose, camera, render, display, and visibility
state before returning.

Required globals supplied by the MCP client:
- NS_REPO_ROOT
- NS_OUTPUT_DIR
"""

import importlib.util
import json
from pathlib import Path

import bpy
from mathutils import Vector


CAMERA_NAME = "NS_MCP_REFERENCE_CAMERA"
COLLECTION_NAME = "NS_MCP_REFERENCE_PACK"
BLOCKOUT_PREFIX = "BLOCKOUT_Nova_"

# Values are camera-to-subject look directions because point_camera places
# the camera at center - direction * distance. Nova faces -Y, so a true front
# camera sits on -Y and looks toward +Y.
VIEWS = (
    ("01_Front", Vector((0.0, 1.0, 0.0))),
    ("02_FrontThreeQuarter", Vector((1.0, 1.0, 0.0))),
    ("03_Side", Vector((1.0, 0.0, 0.0))),
    ("04_BackThreeQuarter", Vector((1.0, -1.0, 0.0))),
    ("05_Back", Vector((0.0, -1.0, 0.0))),
    ("06_Top", Vector((0.0, 0.0, -1.0))),
)

LOCKED_INHERITANCE = (
    "1.85 m production scale",
    "military / Sentinel identity",
    "approved V3 shoulder-to-waist proportion",
    "narrower sealed-helmet proportion",
    "segmented chest / abdomen hierarchy",
    "integrated right-arm cannon silhouette",
    "athletic lower-body proportions",
    "reduced boot mass",
    "compact back power/comms module",
    "V3.2.1 articulation-clearance envelope",
)

OPEN_DESIGN_DECISIONS = (
    "final chest / abdomen panel seams",
    "final helmet brow / cheek / jaw / crown / rear-shell treatment",
    "final arm-cannon housing and modular attachment geometry",
    "soft-goods construction and exact placement",
    "boot / shin surface detailing",
    "back-module surface detailing",
    "functional fasteners / vents / service panels",
    "final open / unhelmeted presentation",
    "final visor face-visibility treatment",
    "modular team / class marking locations",
    "exact visible-height reconciliation against the 1.85 m production target",
)


def load_module(path, module_name):
    spec = importlib.util.spec_from_file_location(module_name, str(path))
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load Python module: {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def blockout_objects():
    objects = [
        obj
        for obj in bpy.data.objects
        if obj.type == "MESH" and obj.name.startswith(BLOCKOUT_PREFIX)
    ]
    if not objects:
        raise RuntimeError("No BLOCKOUT_Nova_* meshes were found.")
    return objects


def world_points(objects):
    points = []
    for obj in objects:
        points.extend(
            obj.matrix_world @ Vector(corner)
            for corner in obj.bound_box
        )
    return points


def world_bounds(objects):
    points = world_points(objects)
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


def ensure_camera():
    collection = bpy.data.collections.get(COLLECTION_NAME)
    if collection is None:
        collection = bpy.data.collections.new(COLLECTION_NAME)
        bpy.context.scene.collection.children.link(collection)

    camera = bpy.data.objects.get(CAMERA_NAME)
    if camera is None:
        data = bpy.data.cameras.new(CAMERA_NAME)
        camera = bpy.data.objects.new(CAMERA_NAME, data)
        collection.objects.link(camera)

    if camera.type != "CAMERA":
        raise RuntimeError(f"{CAMERA_NAME} exists but is not a camera.")

    camera.data.type = "ORTHO"
    camera.data.lens = 50
    return camera


def remove_camera():
    camera = bpy.data.objects.get(CAMERA_NAME)
    if camera is not None:
        data = camera.data if camera.type == "CAMERA" else None
        bpy.data.objects.remove(camera, do_unlink=True)
        if data is not None and data.users == 0:
            bpy.data.cameras.remove(data)

    collection = bpy.data.collections.get(COLLECTION_NAME)
    if collection is not None and not collection.objects and not collection.children:
        bpy.data.collections.remove(collection)


def snapshot_scene(scene, rig):
    display_state = {}
    if hasattr(scene, "display"):
        shading = scene.display.shading
        for name in (
            "light",
            "show_shadows",
            "show_cavity",
            "cavity_type",
            "color_type",
            "show_specular_highlight",
        ):
            if hasattr(shading, name):
                display_state[name] = getattr(shading, name)

    pose_state = {
        bone.name: {
            "rotation_mode": bone.rotation_mode,
            "matrix_basis": bone.matrix_basis.copy(),
        }
        for bone in rig.pose.bones
    }

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
        "world_color": tuple(scene.world.color) if scene.world is not None else None,
        "display": display_state,
        "action": rig.animation_data.action if rig.animation_data else None,
        "pose": pose_state,
        "hide_render": {
            obj.name: obj.hide_render
            for obj in scene.objects
        },
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

    for object_name, hidden in snapshot["hide_render"].items():
        obj = bpy.data.objects.get(object_name)
        if obj is not None:
            obj.hide_render = hidden

    if snapshot["action"] is not None:
        rig.animation_data_create()
        rig.animation_data.action = snapshot["action"]
    elif rig.animation_data is not None:
        rig.animation_data.action = None

    scene.frame_set(snapshot["frame_current"])
    bpy.context.view_layer.update()

    if snapshot["action"] is None:
        for bone_name, state in snapshot["pose"].items():
            bone = rig.pose.bones.get(bone_name)
            if bone is not None:
                bone.rotation_mode = state["rotation_mode"]
                bone.matrix_basis = state["matrix_basis"]
        bpy.context.view_layer.update()


def configure_render(scene):
    try:
        scene.render.engine = "BLENDER_WORKBENCH"
    except Exception:
        try:
            scene.render.engine = "BLENDER_EEVEE_NEXT"
        except Exception:
            pass

    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    if scene.world is not None:
        scene.world.color = (0.055, 0.06, 0.07)

    if hasattr(scene, "display"):
        shading = scene.display.shading
        try:
            shading.light = "STUDIO"
        except Exception:
            pass
        try:
            shading.color_type = "MATERIAL"
        except Exception:
            pass
        try:
            shading.show_shadows = True
            shading.show_cavity = True
            shading.cavity_type = "BOTH"
        except Exception:
            pass
        if hasattr(shading, "show_specular_highlight"):
            try:
                shading.show_specular_highlight = True
            except Exception:
                pass


def isolate_approved_blockout(scene, approved_objects):
    approved_names = {obj.name for obj in approved_objects}
    for obj in scene.objects:
        if obj.type == "MESH":
            obj.hide_render = obj.name not in approved_names


def neutralize_nova(articulation, rig):
    if rig.animation_data is not None:
        rig.animation_data.action = None

    articulation.reset_pose(rig)
    rig.update_tag()
    bpy.context.view_layer.update()

    articulation.plant_contacts_to_floor(
        rig,
        ("BLOCKOUT_Nova_Boot_L", "BLOCKOUT_Nova_Boot_R"),
        target_z=0.0,
    )


def point_camera(camera, center, view_direction, distance):
    direction = Vector(view_direction).normalized()
    camera.location = center - direction * distance
    look = center - camera.location
    camera.rotation_euler = look.to_track_quat("-Z", "Y").to_euler()


def fit_orthographic_camera(camera, objects, aspect):
    inverse = camera.matrix_world.inverted()
    projected = [
        inverse @ point
        for point in world_points(objects)
    ]

    min_x = min(point.x for point in projected)
    max_x = max(point.x for point in projected)
    min_y = min(point.y for point in projected)
    max_y = max(point.y for point in projected)

    width = max(max_x - min_x, 0.01)
    height = max(max_y - min_y, 0.01)

    camera.data.ortho_scale = max(
        height,
        width / max(aspect, 0.01),
    ) * 1.14


def render_view(output_dir, camera, objects, label, direction):
    scene = bpy.context.scene
    minimum, maximum = world_bounds(objects)
    center = (minimum + maximum) * 0.5
    diagonal = (maximum - minimum).length
    distance = max(4.0, diagonal * 3.0)

    point_camera(camera, center, direction, distance)

    aspect = scene.render.resolution_x / max(scene.render.resolution_y, 1)
    fit_orthographic_camera(camera, objects, aspect)

    path = output_dir / f"{label}.png"
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)

    return {
        "label": label,
        "file": path.name,
        "view_direction": list(Vector(direction).normalized()),
        "camera_location": list(camera.location),
        "ortho_scale": camera.data.ortho_scale,
    }


def write_index(output_dir, metadata):
    figures = []
    for view in metadata["views"]:
        figures.append(
            f"""
            <figure>
              <img src="{view['file']}" alt="{view['label']}">
              <figcaption>{view['label'].replace('_', ' ')}</figcaption>
            </figure>
            """
        )

    locked = "".join(
        f"<li>{item}</li>"
        for item in metadata["locked_inheritance"]
    )
    open_items = "".join(
        f"<li>{item}</li>"
        for item in metadata["open_design_decisions"]
    )

    html = f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Nova Production Reference Pack</title>
<style>
body{{margin:0;padding:32px;font-family:Arial,sans-serif;background:#17191d;color:#f4f6f8}}
main{{max-width:1500px;margin:0 auto}}
header{{margin-bottom:28px}}
.grid{{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:14px}}
figure{{margin:0;background:#22262c;padding:10px;border-radius:10px}}
img{{display:block;width:100%;height:auto;background:#111317;border-radius:6px}}
figcaption{{padding-top:8px;text-align:center}}
.notes{{display:grid;grid-template-columns:1fr 1fr;gap:24px;margin-top:32px}}
.panel{{background:#22262c;padding:22px;border-radius:12px}}
.badge{{display:inline-block;padding:5px 9px;border-radius:999px;background:#343a43}}
@media(max-width:1100px){{.grid{{grid-template-columns:repeat(2,1fr)}}.notes{{grid-template-columns:1fr}}}}
</style>
</head>
<body><main>
<header>
<h1>Nova Striker — Production Reference Pack</h1>
<p>Approved Blockout V3 · Articulation V3.2.1 · <span class="badge">SHEET REVIEW REQUIRED</span></p>
<p>This pack is a modeling-reference foundation. It does not approve the final reference sheet automatically.</p>
<p><strong>Scale review:</strong> visible blockout height {metadata['rendered_height_m']:.4f} m vs 1.85 m production target; reconcile this explicitly before sheet approval.</p>
</header>
<section class="grid">
{''.join(figures)}
</section>
<section class="notes">
<div class="panel">
<h2>Locked inheritance</h2>
<ul>{locked}</ul>
</div>
<div class="panel">
<h2>Still to resolve before sheet approval</h2>
<ul>{open_items}</ul>
</div>
</section>
</main></body></html>"""

    (output_dir / "reference-index.html").write_text(html, encoding="utf-8")


def write_checklist(output_dir):
    lines = [
        "# Nova Production Reference Sheet — Approval Checklist",
        "",
        "Status: REVIEW REQUIRED",
        "",
        "Do not mark CHR-NOVA.sheet = approved until every required item below",
        "has been reviewed and explicitly accepted.",
        "",
        "## Geometry / proportion",
        "",
        "- [ ] Matches approved Nova Blockout V3 proportions.",
        "- [ ] Exact visible-height relationship to the 1.85 m production target is explicitly approved.",
        "- [ ] Front, side, rear, and top construction are unambiguous.",
        "- [ ] No turnaround view contradicts another view.",
        "",
        "## Armor / materials",
        "",
        "- [ ] White/light-gray ceramic armor zones are defined.",
        "- [ ] Black/carbon technical undersuit zones are defined.",
        "- [ ] Restrained navy soft-goods zones are defined.",
        "- [ ] Titanium/dark structural-metal zones are defined.",
        "- [ ] Blue emissive/visor treatment is defined.",
        "",
        "## Helmet / head",
        "",
        "- [ ] Sealed helmet shape is resolved.",
        "- [ ] Visor construction is resolved.",
        "- [ ] Open/unhelmeted interface is resolved.",
        "- [ ] Comms / AR-HUD language is resolved.",
        "",
        "## Right-arm cannon",
        "",
        "- [ ] Cannon housing is resolved from front/side/top logic.",
        "- [ ] Precision/high-impact presentation is supported.",
        "- [ ] Sensor/scope, underbarrel, tactical-light, and EMP regions are identified.",
        "",
        "## Gameplay / production",
        "",
        "- [ ] V3.2.1 articulation clearance is preserved.",
        "- [ ] Required gameplay sockets remain unobstructed.",
        "- [ ] Team/Striker markings remain modular rather than baked into geometry.",
        "- [ ] Another modeler could build Nova without fundamental design guessing.",
        "",
        "## Approval",
        "",
        "- [ ] Production owner explicitly approves the completed reference sheet.",
        "- [ ] Production/asset-manifest.json is updated from sheet: in_progress to sheet: approved only after that approval.",
        "",
    ]

    (output_dir / "approval-checklist.md").write_text(
        "\n".join(lines),
        encoding="utf-8",
    )


def run_reference_pack():
    repo_root = Path(NS_REPO_ROOT).resolve()
    output_dir = Path(NS_OUTPUT_DIR).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    for child in output_dir.iterdir():
        if child.is_file():
            child.unlink()

    articulation = load_module(
        repo_root / "Blender" / "Tools" / "ns_test_nova_articulation.py",
        "ns_nova_reference_articulation",
    )

    articulation.require_nova_scene()
    rig = articulation.require_rig()
    scene = bpy.context.scene
    snapshot = snapshot_scene(scene, rig)

    try:
        objects = blockout_objects()
        isolate_approved_blockout(scene, objects)
        neutralize_nova(articulation, rig)
        configure_render(scene)

        camera = ensure_camera()
        scene.camera = camera

        minimum, maximum = world_bounds(objects)
        size = maximum - minimum

        views = [
            render_view(output_dir, camera, objects, label, direction)
            for label, direction in VIEWS
        ]

        materials = sorted(
            {
                material.name
                for obj in objects
                for material in getattr(obj.data, "materials", [])
                if material is not None
            }
        )

        metadata = {
            "schema_version": 1,
            "execution_transport": "mcp_stdio_execute_blender_code",
            "character": "Nova",
            "asset_id": "CHR-NOVA",
            "source_file": bpy.data.filepath,
            "source_saved": False,
            "blockout_version": scene.get(
                "nova_striker_nova_blockout_version",
                "",
            ),
            "articulation_baseline": "3.2.1",
            "sheet_manifest_status": "in_progress",
            "reference_pack_status": "review_required",
            "render_count": len(views),
            "views": views,
            "world_bounds": {
                "min": list(minimum),
                "max": list(maximum),
                "size": list(size),
            },
            "target_height_m": 1.85,
            "rendered_height_m": size.z,
            "height_delta_m": size.z - 1.85,
            "height_reconciliation_required": abs(size.z - 1.85) > 0.01,
            "materials_present": materials,
            "locked_inheritance": list(LOCKED_INHERITANCE),
            "open_design_decisions": list(OPEN_DESIGN_DECISIONS),
        }

        (output_dir / "reference-pack.json").write_text(
            json.dumps(metadata, indent=2),
            encoding="utf-8",
        )

        summary = [
            "Nova Striker — MCP-Driven Production Reference Pack",
            "",
            "Execution transport: MCP stdio -> execute_blender_code -> live Blender",
            f"Source: {metadata['source_file']}",
            "Source .blend saved: no",
            f"Blockout version: {metadata['blockout_version']}",
            f"Articulation baseline: {metadata['articulation_baseline']}",
            f"Views rendered: {metadata['render_count']}",
            f"Visible blockout height: {metadata['rendered_height_m']:.4f} m",
            f"Production target height: {metadata['target_height_m']:.4f} m",
            f"Height delta: {metadata['height_delta_m']:+.4f} m",
            f"Height reconciliation required: {metadata['height_reconciliation_required']}",
            f"Sheet manifest status: {metadata['sheet_manifest_status']}",
            f"Reference pack status: {metadata['reference_pack_status']}",
            "",
            "This pack does not automatically approve the Nova reference sheet.",
            "Review reference-index.html and approval-checklist.md before approval.",
        ]
        (output_dir / "summary.txt").write_text(
            "\n".join(summary) + "\n",
            encoding="utf-8",
        )

        write_index(output_dir, metadata)
        write_checklist(output_dir)

        print(
            json.dumps(
                {
                    "status": "complete",
                    "transport": "mcp_stdio_execute_blender_code",
                    "output": str(output_dir),
                    "render_count": len(views),
                    "views": [view["label"] for view in views],
                    "sheet_status": "in_progress",
                    "approval_status": "review_required",
                    "saved_source": False,
                }
            )
        )
    finally:
        remove_camera()
        restore_scene(scene, rig, snapshot)


run_reference_pack()
