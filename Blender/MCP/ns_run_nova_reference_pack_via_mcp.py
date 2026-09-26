
"""Generate Nova's reference pack through the standard MCP stdio protocol."""

import argparse
import asyncio
import json
import os
from pathlib import Path
import sys

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--mcp-root", required=True)
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args()


def tool_text(result):
    return "\n".join(
        item.text
        for item in result.content
        if hasattr(item, "text")
    )


async def run(args):
    mcp_root = Path(args.mcp_root).resolve()
    repo_root = Path(args.repo_root).resolve()
    output_dir = Path(args.output).resolve()
    live_script = (
        repo_root
        / "Blender"
        / "MCP"
        / "ns_nova_mcp_reference_pack.py"
    )
    server_script = mcp_root / "server.py"

    if not server_script.is_file():
        raise RuntimeError(f"Missing Blender MCP server: {server_script}")
    if not live_script.is_file():
        raise RuntimeError(f"Missing Nova reference-pack script: {live_script}")

    output_dir.mkdir(parents=True, exist_ok=True)

    env = os.environ.copy()
    env["BLENDER_MCP_HOST"] = "127.0.0.1"
    env["BLENDER_MCP_PORT"] = "9877"

    params = StdioServerParameters(
        command=sys.executable,
        args=[str(server_script)],
        env=env,
    )

    async with stdio_client(params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            tools = await session.list_tools()
            tool_names = [tool.name for tool in tools.tools]

            required = {"ping", "execute_blender_code"}
            missing = sorted(required.difference(tool_names))
            if missing:
                raise RuntimeError(
                    "Blender MCP server is missing required tools: "
                    + ", ".join(missing)
                )

            ping_result = await session.call_tool("ping", {})
            if ping_result.isError:
                raise RuntimeError(tool_text(ping_result))
            ping = json.loads(tool_text(ping_result))

            code = f"""
from pathlib import Path
_script_path = {str(live_script)!r}
_namespace = {{
    "__name__": "__main__",
    "NS_REPO_ROOT": {str(repo_root)!r},
    "NS_OUTPUT_DIR": {str(output_dir)!r},
}}
exec(
    compile(
        Path(_script_path).read_text(encoding="utf-8"),
        _script_path,
        "exec",
    ),
    _namespace,
)
"""

            result = await session.call_tool(
                "execute_blender_code",
                {"code": code},
            )
            if result.isError:
                raise RuntimeError(tool_text(result))

            live_output = tool_text(result).strip()
            proof = {
                "schema_version": 1,
                "transport": "standard_mcp_stdio",
                "tool": "execute_blender_code",
                "blender": ping,
                "available_tool_count": len(tool_names),
                "live_reference_pack_output": live_output,
            }
            (output_dir / "mcp-run.json").write_text(
                json.dumps(proof, indent=2),
                encoding="utf-8",
            )

            print("MCP-DRIVEN NOVA REFERENCE PACK PASSED")
            print(json.dumps(proof, indent=2))


def main():
    args = parse_args()
    asyncio.run(run(args))


if __name__ == "__main__":
    main()
