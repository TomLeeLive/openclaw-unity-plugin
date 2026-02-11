# OpenClaw Unity Plugin - Setup Guide

This guide explains how to set up the OpenClaw Unity Plugin for different use cases.

## 🤔 Which Mode Do I Need?

Before diving into setup, understand which mode fits your workflow:

| How You Use AI | Mode Needed | Why |
|----------------|-------------|-----|
| **Chat apps** (Telegram, Discord) | Mode A: Gateway | OpenClaw routes commands to Unity |
| **Claude Code** in terminal | Mode B: MCP | Direct connection needed |
| **Both** | Hybrid | Best of both worlds |

### If You Already Use OpenClaw...

If you're chatting with an AI assistant through OpenClaw (like Telegram or Discord), **you don't need MCP setup** - the assistant already has Unity tools via the Gateway!

```
You (Telegram) → OpenClaw Gateway → AI Assistant → unity_execute tool → Unity
                                    ↑
                            Already has access!
```

### When MCP is Useful

MCP is useful when:
1. **Using Claude Code directly** in terminal (not through OpenClaw)
2. **Using Claude Desktop** app
3. **Using Cursor** or other MCP-compatible editors
4. **Spawning Claude Code as sub-agent** from OpenClaw for coding + testing workflows

```
# Without MCP:
$ claude
> Control Unity  →  ❌ No tools available

# With MCP:
$ claude  
> Control Unity  →  ✅ unity.* tools available
```

## 🅰️ Mode A: OpenClaw Gateway (Remote Access)

**When to use:** When you want to develop games remotely via Telegram, Discord, or web.

### Setup Steps

```bash
# 1. Install OpenClaw
npm install -g openclaw

# 2. Start Gateway
openclaw gateway start

# 3. Install Unity Plugin
#    Option A: Package Manager > Add from git URL
#    https://github.com/TomLeeLive/openclaw-unity-plugin.git
#    
#    Option B: Clone and add from disk

# 4. Open Unity project
#    Plugin automatically connects to Gateway

# 5. Configure chat integration (optional)
openclaw config
```

### How it works

```
┌──────────────┐      ┌─────────────────┐      ┌──────────────┐
│  Telegram/   │ ───→ │    OpenClaw     │ ───→ │    Unity     │
│  Discord/Web │ ←─── │    Gateway      │ ←─── │    Editor    │
└──────────────┘      └─────────────────┘      └──────────────┘
     Phone              Your Computer           Your Computer
```

### Example Usage

From your phone at a café:
```
You: "Show me the Player object's position"
AI: Player is at position (10.5, 0, -3.2)

You: "Move it to origin"
AI: Done. Player moved to (0, 0, 0)

You: "Take a screenshot"
AI: [Screenshot attached]
```

---

## 🅱️ Mode B: MCP Direct (Local Development)

**When to use:** When you want to use Claude Code, Claude Desktop, or Cursor to directly control Unity.

### Setup Steps

```bash
# 1. Install Unity Plugin (same as above)

# 2. Install MCP server dependencies
cd /path/to/openclaw-unity-plugin/MCP~
npm install

# 3. Register MCP server with Claude Code
claude mcp add unity -- node /full/path/to/openclaw-unity-plugin/MCP~/index.js

# Or for Claude Desktop, edit claude_desktop_config.json:
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["/full/path/to/openclaw-unity-plugin/MCP~/index.js"]
    }
  }
}

# 4. Start MCP Bridge in Unity
#    Window > OpenClaw > Start MCP Bridge

# 5. Use Claude Code
claude
```

### How it works

```
┌──────────────┐      ┌─────────────────┐      ┌──────────────┐
│ Claude Code  │ ───→ │   MCP Server    │ ───→ │    Unity     │
│ or Desktop   │ ←─── │   (Node.js)     │ ←─── │    Editor    │
└──────────────┘      └─────────────────┘      └──────────────┘
    Terminal             localhost:27182        localhost:27182
```

### Example Usage

In your terminal:
```
$ claude
> What GameObjects are in the current scene?
AI: Found 15 GameObjects: Main Camera, Directional Light, Player, ...

> Create a Cube at position (5, 1, 0)
AI: Created Cube at (5, 1, 0)

> Enter Play mode and press W key
AI: Entered Play mode, simulating W key press
```

---

## 🔀 Hybrid Mode (Both)

You can run both modes simultaneously without conflicts.

```
At home:   Claude Code → MCP → Unity (local, fast)
Outside:   Telegram → OpenClaw Gateway → Unity (remote)
```

### Port Configuration

| Service | Default Port |
|---------|--------------|
| MCP Bridge | 27182 |
| OpenClaw Gateway | 18789 |

### Config File

Create `OpenClawConfig` asset in Unity (Assets > Create > OpenClaw > Config):

```
Gateway URL: http://localhost:18789
Enable MCP Bridge: ✓
MCP Bridge Port: 27182
```

---

## Troubleshooting

### MCP Bridge not connecting

1. Check Unity Console for `[OpenClaw MCP]` messages
2. Verify port 27182 is not in use: `lsof -i :27182`
3. Try restarting: Window > OpenClaw > Stop MCP Bridge, then Start

### Gateway not connecting

1. Check gateway status: `openclaw gateway status`
2. Verify port 18789 is accessible
3. Check Unity Console for `[OpenClaw]` messages

### Claude Code not finding tools

1. Verify MCP server is registered: `claude mcp list`
2. Check MCP server path is correct (use absolute path)
3. Restart Claude Code after adding MCP server

---

## Quick Reference

| Task | Command |
|------|---------|
| Start Gateway | `openclaw gateway start` |
| Stop Gateway | `openclaw gateway stop` |
| Check Gateway | `openclaw gateway status` |
| Add MCP to Claude | `claude mcp add unity -- node /path/to/MCP~/index.js` |
| Start MCP Bridge | Window > OpenClaw > Start MCP Bridge |
| Check MCP Status | Window > OpenClaw > MCP Bridge Status |
