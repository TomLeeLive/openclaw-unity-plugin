#!/usr/bin/env node
/**
 * OpenClaw Unity MCP Server
 * Bridges MCP protocol to Unity Plugin via HTTP
 * 
 * Features:
 * - Tools: Execute Unity commands
 * - Resources: Access Unity project data
 * 
 * Usage with Claude Code:
 *   claude mcp add unity -- node /path/to/MCP~/index.js
 */

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';

const UNITY_HOST = process.env.UNITY_HOST || '127.0.0.1';
const UNITY_PORT = process.env.UNITY_PORT || 27182;
const UNITY_URL = `http://${UNITY_HOST}:${UNITY_PORT}`;

// Dynamic tool list - fetched from Unity
let cachedTools = [];

// Fetch tools from Unity
async function fetchUnityTools() {
  try {
    const response = await fetch(`${UNITY_URL}/tools`);
    if (response.ok) {
      const data = await response.json();
      cachedTools = (data.tools || []).map(t => ({
        name: t.name,
        description: t.description || t.name,
        inputSchema: { type: 'object', properties: {} }
      }));
    }
  } catch (e) {
    // Use fallback if Unity not connected
  }
  return cachedTools;
}

// Fallback tools if Unity not connected
const FALLBACK_TOOLS = [
  { name: 'scene.getData', description: 'Get current scene hierarchy', inputSchema: { type: 'object', properties: { depth: { type: 'number' } } } },
  { name: 'scene.getActive', description: 'Get active scene info', inputSchema: { type: 'object', properties: {} } },
  { name: 'gameobject.find', description: 'Find GameObject by name', inputSchema: { type: 'object', properties: { name: { type: 'string' } } } },
  { name: 'gameobject.create', description: 'Create GameObject', inputSchema: { type: 'object', properties: { name: { type: 'string' }, primitive: { type: 'string' } } } },
  { name: 'transform.setPosition', description: 'Set position', inputSchema: { type: 'object', properties: { name: { type: 'string' }, x: { type: 'number' }, y: { type: 'number' }, z: { type: 'number' } } } },
  { name: 'debug.hierarchy', description: 'Get hierarchy tree', inputSchema: { type: 'object', properties: { depth: { type: 'number' } } } },
  { name: 'debug.screenshot', description: 'Capture screenshot', inputSchema: { type: 'object', properties: {} } },
  { name: 'editor.play', description: 'Enter Play mode', inputSchema: { type: 'object', properties: {} } },
  { name: 'editor.stop', description: 'Exit Play mode', inputSchema: { type: 'object', properties: {} } },
  { name: 'batch.execute', description: 'Execute multiple tools in one call', inputSchema: { type: 'object', properties: { commands: { type: 'array' }, stopOnError: { type: 'boolean' } } } },
  { name: 'session.getInfo', description: 'Get session info', inputSchema: { type: 'object', properties: {} } },
];

class UnityMCPServer {
  constructor() {
    this.server = new Server(
      { name: 'openclaw-unity', version: '1.6.0' },
      { 
        capabilities: { 
          tools: {},
          resources: {}  // Enable resources
        } 
      }
    );
    
    this.setupHandlers();
  }
  
  setupHandlers() {
    // List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      const tools = await fetchUnityTools();
      return { tools: tools.length > 0 ? tools : FALLBACK_TOOLS };
    });
    
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
    
    // List resources
    this.server.setRequestHandler(ListResourcesRequestSchema, async () => {
      return {
        resources: [
          {
            uri: 'unity://scene/hierarchy',
            name: 'Scene Hierarchy',
            description: 'Current scene hierarchy tree',
            mimeType: 'application/json'
          },
          {
            uri: 'unity://scene/active',
            name: 'Active Scene',
            description: 'Active scene information',
            mimeType: 'application/json'
          },
          {
            uri: 'unity://project/scripts',
            name: 'Project Scripts',
            description: 'List of C# scripts in project',
            mimeType: 'application/json'
          },
          {
            uri: 'unity://project/scenes',
            name: 'Project Scenes',
            description: 'List of scenes in build settings',
            mimeType: 'application/json'
          },
          {
            uri: 'unity://project/assets',
            name: 'Project Assets',
            description: 'Asset search (use ?query=name&type=Prefab)',
            mimeType: 'application/json'
          },
          {
            uri: 'unity://editor/state',
            name: 'Editor State',
            description: 'Editor state (play mode, paused, etc.)',
            mimeType: 'application/json'
          },
          {
            uri: 'unity://console/logs',
            name: 'Console Logs',
            description: 'Recent Unity console logs',
            mimeType: 'application/json'
          },
          {
            uri: 'unity://session/info',
            name: 'Session Info',
            description: 'Current Unity session info',
            mimeType: 'application/json'
          }
        ]
      };
    });
    
    // Read resource
    this.server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
      const { uri } = request.params;
      
      try {
        const data = await this.readResource(uri);
        return {
          contents: [{
            uri,
            mimeType: 'application/json',
            text: JSON.stringify(data, null, 2)
          }]
        };
      } catch (error) {
        return {
          contents: [{
            uri,
            mimeType: 'application/json',
            text: JSON.stringify({ error: error.message })
          }]
        };
      }
    });
  }
  
  async readResource(uri) {
    const url = new URL(uri);
    const path = url.pathname;
    const params = Object.fromEntries(url.searchParams);
    
    // Map resource URIs to Unity tools
    const resourceMap = {
      '/scene/hierarchy': { tool: 'debug.hierarchy', params: { depth: 3 } },
      '/scene/active': { tool: 'scene.getActive', params: {} },
      '/project/scripts': { tool: 'script.list', params: {} },
      '/project/scenes': { tool: 'scene.list', params: {} },
      '/project/assets': { tool: 'asset.find', params: { query: params.query || '', type: params.type || '' } },
      '/editor/state': { tool: 'app.getState', params: {} },
      '/console/logs': { tool: 'console.getLogs', params: { count: parseInt(params.count) || 50 } },
      '/session/info': { tool: 'session.getInfo', params: {} },
    };
    
    const mapping = resourceMap[path];
    if (!mapping) {
      throw new Error(`Unknown resource: ${uri}`);
    }
    
    return await this.executeUnityTool(mapping.tool, { ...mapping.params, ...params });
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
    console.error('[OpenClaw Unity MCP] Server started (v1.6.0 with Resources)');
  }
}

// Start server
const server = new UnityMCPServer();
server.run().catch(console.error);
