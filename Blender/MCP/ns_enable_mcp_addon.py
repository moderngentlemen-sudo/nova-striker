"""
Enable the locally installed Nova Striker Blender MCP bridge and save user
preferences. Intended to be invoked by setup_blender_mcp.ps1 in background mode.
"""

import os
import bpy


module_name = os.environ.get(
    "NS_BLENDER_MCP_ADDON_MODULE",
    "maket_blender_mcp",
)

try:
    bpy.ops.preferences.addon_enable(
        module=module_name,
    )
    bpy.ops.wm.save_userpref()
except Exception as exc:
    raise RuntimeError(
        f"Could not enable Blender addon '{module_name}': {exc}"
    ) from exc

enabled = module_name in bpy.context.preferences.addons
if not enabled:
    raise RuntimeError(
        f"Blender did not report '{module_name}' as enabled."
    )

print(
    f"[Nova Striker] Enabled Blender MCP addon: {module_name}"
)
