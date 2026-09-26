"""
Read-only smoke test for the local Blender MCP bridge.

Uses the third-party MCP server module only for:
- ping
- get_scene_info
- get_object_info("RIG_Nova")

It deliberately does not create/delete/modify Blender data.
"""

import argparse
import json
import os
import sys


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--mcp-root",
        required=True,
    )
    return parser.parse_args()


def main():
    args = parse_args()
    mcp_root = os.path.abspath(args.mcp_root)

    if not os.path.isdir(mcp_root):
        raise SystemExit(
            f"MCP root does not exist: {mcp_root}"
        )

    os.environ["BLENDER_MCP_HOST"] = "127.0.0.1"
    os.environ["BLENDER_MCP_PORT"] = "9877"

    sys.path.insert(0, mcp_root)

    try:
        import server as blender_mcp_server
    except Exception as exc:
        raise SystemExit(
            f"Could not import Blender MCP server from {mcp_root}: {exc}"
        ) from exc

    print("== Blender MCP ping ==")
    ping = blender_mcp_server.ping()
    print(
        json.dumps(
            ping,
            indent=2,
            ensure_ascii=False,
            default=str,
        )
    )

    print("== Scene info ==")
    scene = blender_mcp_server.get_scene_info()
    print(
        json.dumps(
            scene,
            indent=2,
            ensure_ascii=False,
            default=str,
        )
    )

    print("== RIG_Nova ==")
    rig = blender_mcp_server.get_object_info("RIG_Nova")
    print(
        json.dumps(
            rig,
            indent=2,
            ensure_ascii=False,
            default=str,
        )
    )

    print(
        "READ-ONLY MCP SMOKE TEST PASSED"
    )


if __name__ == "__main__":
    main()
