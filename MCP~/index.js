#!/usr/bin/env node
/**
 * OpenClaw Unity MCP Server
 * Bridges MCP protocol to Unity Plugin via HTTP
 * 
 * Usage with Claude Code:
 *   claude mcp add unity -- node /path/to/MCP~/index.js
 * 
 * Or in claude_desktop_config.json:
 *   { "mcpServers": { "unity": { "command": "node", "args": ["/path/to/MCP~/index.js"] } } }
 */

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';

const UNITY_HOST = process.env.UNITY_HOST || '127.0.0.1';
const UNITY_PORT = process.env.UNITY_PORT || 27182;
const UNITY_URL = `http://${UNITY_HOST}:${UNITY_PORT}`;

// Tool definitions - mirrors Unity plugin tools
const TOOLS = [
  // Scene tools
  { name: 'scene.getData', description: 'Get current scene info and hierarchy', inputSchema: { type: 'object', properties: {} } },
  { name: 'scene.list', description: 'List all scenes in build settings', inputSchema: { type: 'object', properties: {} } },
  { name: 'scene.load', description: 'Load a scene by name or index', inputSchema: { type: 'object', properties: { name: { type: 'string' }, index: { type: 'number' }, mode: { type: 'string', enum: ['Single', 'Additive'] } } } },
  { name: 'scene.save', description: 'Save the current scene', inputSchema: { type: 'object', properties: {} } },
  { name: 'scene.saveAll', description: 'Save all open scenes', inputSchema: { type: 'object', properties: {} } },
  
  // GameObject tools
  { name: 'gameobject.find', description: 'Find GameObject by name or path', inputSchema: { type: 'object', properties: { name: { type: 'string' }, path: { type: 'string' } }, required: [] } },
  { name: 'gameobject.create', description: 'Create a new GameObject', inputSchema: { type: 'object', properties: { name: { type: 'string' }, type: { type: 'string' }, parent: { type: 'string' }, x: { type: 'number' }, y: { type: 'number' }, z: { type: 'number' } } } },
  { name: 'gameobject.delete', description: 'Delete a GameObject', inputSchema: { type: 'object', properties: { name: { type: 'string' } }, required: ['name'] } },
  { name: 'gameobject.setActive', description: 'Enable/disable a GameObject', inputSchema: { type: 'object', properties: { name: { type: 'string' }, active: { type: 'boolean' } }, required: ['name', 'active'] } },
  
  // Transform tools
  { name: 'transform.getPosition', description: 'Get GameObject position', inputSchema: { type: 'object', properties: { name: { type: 'string' } }, required: ['name'] } },
  { name: 'transform.setPosition', description: 'Set GameObject position', inputSchema: { type: 'object', properties: { name: { type: 'string' }, x: { type: 'number' }, y: { type: 'number' }, z: { type: 'number' } }, required: ['name'] } },
  { name: 'transform.getRotation', description: 'Get GameObject rotation', inputSchema: { type: 'object', properties: { name: { type: 'string' } }, required: ['name'] } },
  { name: 'transform.setRotation', description: 'Set GameObject rotation', inputSchema: { type: 'object', properties: { name: { type: 'string' }, x: { type: 'number' }, y: { type: 'number' }, z: { type: 'number' } }, required: ['name'] } },
  { name: 'transform.getScale', description: 'Get GameObject scale', inputSchema: { type: 'object', properties: { name: { type: 'string' } }, required: ['name'] } },
  { name: 'transform.setScale', description: 'Set GameObject scale', inputSchema: { type: 'object', properties: { name: { type: 'string' }, x: { type: 'number' }, y: { type: 'number' }, z: { type: 'number' } }, required: ['name'] } },
  
  // Component tools
  { name: 'component.get', description: 'Get component data from GameObject', inputSchema: { type: 'object', properties: { name: { type: 'string' }, component: { type: 'string' } }, required: ['name'] } },
  { name: 'component.add', description: 'Add component to GameObject', inputSchema: { type: 'object', properties: { name: { type: 'string' }, component: { type: 'string' } }, required: ['name', 'component'] } },
  { name: 'component.remove', description: 'Remove component from GameObject', inputSchema: { type: 'object', properties: { name: { type: 'string' }, component: { type: 'string' } }, required: ['name', 'component'] } },
  { name: 'component.setProperty', description: 'Set component property value', inputSchema: { type: 'object', properties: { name: { type: 'string' }, component: { type: 'string' }, property: { type: 'string' }, value: {} }, required: ['name', 'component', 'property', 'value'] } },
  
  // Debug tools
  { name: 'debug.hierarchy', description: 'Get scene hierarchy tree', inputSchema: { type: 'object', properties: { depth: { type: 'number' } } } },
  { name: 'debug.screenshot', description: 'Capture screenshot', inputSchema: { type: 'object', properties: { filename: { type: 'string' }, method: { type: 'string', enum: ['gameview', 'screencapture'] } } } },
  { name: 'debug.log', description: 'Write to Unity console', inputSchema: { type: 'object', properties: { message: { type: 'string' }, level: { type: 'string', enum: ['info', 'warning', 'error'] } }, required: ['message'] } },
  
  // Console tools
  { name: 'console.getLogs', description: 'Get Unity console logs', inputSchema: { type: 'object', properties: { count: { type: 'number' }, filter: { type: 'string' } } } },
  { name: 'console.clear', description: 'Clear Unity console', inputSchema: { type: 'object', properties: {} } },
  
  // Editor tools
  { name: 'editor.play', description: 'Enter Play mode', inputSchema: { type: 'object', properties: {} } },
  { name: 'editor.stop', description: 'Exit Play mode', inputSchema: { type: 'object', properties: {} } },
  { name: 'editor.pause', description: 'Pause Play mode', inputSchema: { type: 'object', properties: {} } },
  { name: 'editor.unpause', description: 'Unpause Play mode', inputSchema: { type: 'object', properties: {} } },
  { name: 'editor.getState', description: 'Get editor state (playing/paused)', inputSchema: { type: 'object', properties: {} } },
  { name: 'editor.recompile', description: 'Trigger script recompilation', inputSchema: { type: 'object', properties: {} } },
  
  // Input simulation
  { name: 'input.simulateKey', description: 'Simulate keyboard input', inputSchema: { type: 'object', properties: { key: { type: 'string' }, action: { type: 'string', enum: ['press', 'down', 'up'] }, duration: { type: 'number' } }, required: ['key'] } },
  { name: 'input.simulateMouse', description: 'Simulate mouse input', inputSchema: { type: 'object', properties: { button: { type: 'string' }, action: { type: 'string' }, x: { type: 'number' }, y: { type: 'number' } } } },
  
  // Selection
  { name: 'selection.get', description: 'Get currently selected objects', inputSchema: { type: 'object', properties: {} } },
  { name: 'selection.set', description: 'Set selected objects', inputSchema: { type: 'object', properties: { names: { type: 'array', items: { type: 'string' } } }, required: ['names'] } },
  
  // Asset tools
  { name: 'asset.list', description: 'List assets in path', inputSchema: { type: 'object', properties: { path: { type: 'string' }, filter: { type: 'string' } } } },
  { name: 'asset.refresh', description: 'Refresh asset database', inputSchema: { type: 'object', properties: {} } },
];

class UnityMCPServer {
  constructor() {
    this.server = new Server(
      { name: 'openclaw-unity', version: '1.0.0' },
      { capabilities: { tools: {} } }
    );
    
    this.setupHandlers();
  }
  
  setupHandlers() {
    // List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: TOOLS
    }));
    
    // Execute tool
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;
      
      try {
        const result = await this.executeUnityTool(name, args || {});
        return {
          content: [{ type: 'text', text: JSON.stringify(result, null, 2) }]
        };
      } catch (error) {
        return {
          content: [{ type: 'text', text: `Error: ${error.message}` }],
          isError: true
        };
      }
    });
  }
  
  async executeUnityTool(tool, args) {
    const url = `${UNITY_URL}/tool`;
    
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ tool, arguments: args })
    });
    
    if (!response.ok) {
      throw new Error(`Unity returned ${response.status}: ${await response.text()}`);
    }
    
    return await response.json();
  }
  
  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('[OpenClaw Unity MCP] Server started');
  }
}

// Start server
const server = new UnityMCPServer();
server.run().catch(console.error);
