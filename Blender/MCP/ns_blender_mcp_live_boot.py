"""
Nova Striker deterministic live bootstrap for the third-party Blender MCP addon.

The upstream add-on's UI autostart path services queued commands through a
modal TIMER operator. On some Blender 5.2 startup paths the TCP listener can
become reachable before that modal pump is actually servicing commands.

This bootstrap:
- keeps the existing add-on/server implementation unchanged,
- starts the local TCP listener on 127.0.0.1,
- services the add-on command queue through bpy.app.timers on Blender's main
  thread,
- avoids depending on the UI modal event loop for MCP readiness.

It is intended to be launched with Blender after Nova_master.blend is opened.
"""

import importlib
import os
import bpy


MODULE_NAME = os.environ.get(
    "NS_BLENDER_MCP_ADDON_MODULE",
    "maket_blender_mcp",
)
PUMP_INTERVAL_SECONDS = 0.05
NAMESPACE_KEY = "NS_BLENDER_MCP_APP_TIMER_PUMP"


def _get_module():
    try:
        return importlib.import_module(MODULE_NAME)
    except Exception as exc:
        raise RuntimeError(
            f"Could not import enabled Blender MCP add-on '{MODULE_NAME}': {exc}"
        ) from exc


def _pump():
    module = _get_module()
    server = module.get_server()

    if not server.running:
        server.start()

    server.pump()

    # Keep servicing the queue for the lifetime of this Blender process.
    return PUMP_INTERVAL_SECONDS


def main():
    module = _get_module()
    server = module.get_server()

    if not server.running:
        server.start()

    namespace = bpy.app.driver_namespace
    existing = namespace.get(NAMESPACE_KEY)

    if existing is None or not bpy.app.timers.is_registered(existing):
        namespace[NAMESPACE_KEY] = _pump
        bpy.app.timers.register(
            _pump,
            first_interval=PUMP_INTERVAL_SECONDS,
            persistent=True,
        )

    bpy.context.scene["nova_striker_blender_mcp_bootstrap"] = "app_timer_v1"

    print(
        "[Nova Striker] Blender MCP deterministic app-timer pump installed "
        f"for {MODULE_NAME} on 127.0.0.1."
    )


if __name__ == "__main__":
    main()
