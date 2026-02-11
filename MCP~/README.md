# OpenClaw Unity MCP Server

MCP (Model Context Protocol) server for direct Claude Code integration with Unity.

## Architecture

```
┌─────────────────┐     stdio      ┌──────────────────┐     HTTP      ┌──────────────────┐
│   Claude Code   │ ←───────────→  │  MCP Server      │ ←──────────→  │  Unity Editor    │
│   or Desktop    │    MCP         │  (this package)  │   :27182      │  (MCP Bridge)    │
└─────────────────┘                └──────────────────┘               └──────────────────┘
```

## Prerequisites

1. Unity project with OpenClaw Unity Plugin installed
2. MCP Bridge enabled in Unity (Window > OpenClaw > Start MCP Bridge)
3. Node.js 18+

## Installation

```bash
cd MCP~
npm install
```

## Usage with Claude Code

```bash
# Add as MCP server
claude mcp add unity -- node /path/to/openclaw_unity_mcp/MCP~/index.js

# Or start manually
node index.js
```

## Usage with Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["/path/to/openclaw_unity_mcp/MCP~/index.js"]
    }
  }
}
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `UNITY_HOST` | `127.0.0.1` | Unity MCP Bridge host |
| `UNITY_PORT` | `27182` | Unity MCP Bridge port |

## Available Tools

All standard Unity tools are available:
- `scene.getData`, `scene.list`, `scene.load`, `scene.save`
- `gameobject.find`, `gameobject.create`, `gameobject.delete`
- `transform.getPosition`, `transform.setPosition`, etc.
- `component.get`, `component.add`, `component.remove`
- `debug.hierarchy`, `debug.screenshot`, `debug.log`
- `editor.play`, `editor.stop`, `editor.pause`
- `input.simulateKey`, `input.simulateMouse`
- And more...

## Troubleshooting

### "Connection refused" error
- Ensure MCP Bridge is running in Unity
- Check port 27182 is not blocked
- Try: Window > OpenClaw > MCP Bridge Status

### Tools not executing
- Unity must be in focus for some operations
- Check Unity Console for errors

## License

MIT License
