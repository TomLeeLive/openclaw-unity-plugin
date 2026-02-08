/**
 * OpenClaw Unity Bridge Plugin
 * Connects Unity Editor to OpenClaw AI assistant via HTTP
 */

import type { IncomingMessage, ServerResponse } from "node:http";
import type { OpenClawPluginApi } from "openclaw/plugin-sdk";

interface UnitySession {
  sessionId: string;
  registeredAt: number;
  lastHeartbeat: number;
  projectName: string;
  unityVersion: string;
  platform: string;
  tools: Array<{ name: string; description: string }>;
  pendingCommands: Array<{
    requestId: string;
    tool: string;
    parameters: string;
    createdAt: number;
  }>;
  results: Map<string, any>;
}

// Store active Unity sessions
const sessions = new Map<string, UnitySession>();

// Generate unique ID
function generateId(): string {
  return `unity_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
}

// Clean up stale sessions (no heartbeat for 60 seconds)
function cleanupStaleSessions() {
  const now = Date.now();
  const staleThreshold = 120000; // 2 minutes
  
  for (const [id, session] of sessions) {
    if (now - session.lastHeartbeat > staleThreshold) {
      sessions.delete(id);
    }
  }
}

// Read JSON body from request
async function readJsonBody(req: IncomingMessage, maxBytes = 1024 * 1024): Promise<any> {
  const chunks: Buffer[] = [];
  let total = 0;
  
  return new Promise((resolve, reject) => {
    req.on("data", (chunk: Buffer) => {
      total += chunk.length;
      if (total > maxBytes) {
        reject(new Error("Payload too large"));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    
    req.on("end", () => {
      try {
        const raw = Buffer.concat(chunks).toString("utf8");
        if (!raw.trim()) {
          resolve({});
          return;
        }
        resolve(JSON.parse(raw));
      } catch (err) {
        reject(err);
      }
    });
    
    req.on("error", reject);
  });
}

// Send JSON response
function sendJson(res: ServerResponse, status: number, data: any) {
  res.statusCode = status;
  res.setHeader("Content-Type", "application/json");
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
  res.end(JSON.stringify(data));
}

// HTTP Handler for Unity endpoints
async function handleUnityHttpRequest(
  req: IncomingMessage,
  res: ServerResponse
): Promise<boolean> {
  const url = new URL(req.url ?? "/", "http://localhost");
  const path = url.pathname;
  
  // Only handle /unity/* paths
  if (!path.startsWith("/unity/")) {
    return false;
  }
  
  // Handle CORS preflight
  if (req.method === "OPTIONS") {
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
    res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
    res.statusCode = 204;
    res.end();
    return true;
  }
  
  const endpoint = path.replace("/unity/", "");
  
  try {
    switch (endpoint) {
      case "register": {
        if (req.method !== "POST") {
          sendJson(res, 405, { error: "Method not allowed" });
          return true;
        }
        
        const body = await readJsonBody(req);
        const { type, version, project, platform, tools } = body;
        
        const sessionId = generateId();
        const session: UnitySession = {
          sessionId,
          registeredAt: Date.now(),
          lastHeartbeat: Date.now(),
          projectName: project || "Unknown",
          unityVersion: version || "Unknown",
          platform: platform || "Unknown",
          tools: tools || [],
          pendingCommands: [],
          results: new Map(),
        };
        
        sessions.set(sessionId, session);
        console.log(`[Unity] Registered: ${project} (${version}) - Session: ${sessionId}`);
        
        sendJson(res, 200, { sessionId, status: "connected" });
        return true;
      }
      
      case "heartbeat": {
        if (req.method !== "POST") {
          sendJson(res, 405, { error: "Method not allowed" });
          return true;
        }
        
        const body = await readJsonBody(req);
        const { sessionId } = body;
        
        const session = sessions.get(sessionId);
        if (!session) {
          sendJson(res, 404, { error: "Session not found" });
          return true;
        }
        
        session.lastHeartbeat = Date.now();
        sendJson(res, 200, { ok: true });
        return true;
      }
      
      case "poll": {
        const sessionId = url.searchParams.get("sessionId");
        
        const session = sessions.get(sessionId || "");
        if (!session) {
          sendJson(res, 404, { error: "Session not found" });
          return true;
        }
        
        session.lastHeartbeat = Date.now();
        
        // Return next pending command if any
        if (session.pendingCommands.length > 0) {
          const command = session.pendingCommands.shift()!;
          sendJson(res, 200, command);
        } else {
          sendJson(res, 200, null);
        }
        return true;
      }
      
      case "result": {
        if (req.method !== "POST") {
          sendJson(res, 405, { error: "Method not allowed" });
          return true;
        }
        
        const body = await readJsonBody(req);
        const { sessionId, requestId, tool, success, result, error } = body;
        
        const session = sessions.get(sessionId);
        if (!session) {
          sendJson(res, 404, { error: "Session not found" });
          return true;
        }
        
        session.results.set(requestId, { success, result, error, tool });
        console.log(`[Unity] Tool result: ${tool} - ${success ? "success" : "failed"}`);
        
        sendJson(res, 200, { ok: true });
        return true;
      }
      
      case "message": {
        if (req.method !== "POST") {
          sendJson(res, 405, { error: "Method not allowed" });
          return true;
        }
        
        const body = await readJsonBody(req);
        const { sessionId, message } = body;
        
        const session = sessions.get(sessionId);
        if (!session) {
          sendJson(res, 404, { error: "Session not found" });
          return true;
        }
        
        console.log(`[Unity] Message from ${session.projectName}: ${message}`);
        sendJson(res, 200, { ok: true });
        return true;
      }
      
      case "status": {
        const activeSessions = Array.from(sessions.values()).map(s => ({
          sessionId: s.sessionId,
          project: s.projectName,
          version: s.unityVersion,
          platform: s.platform,
          connectedAt: new Date(s.registeredAt).toISOString(),
          lastSeen: new Date(s.lastHeartbeat).toISOString(),
          pendingCommands: s.pendingCommands.length,
        }));
        
        sendJson(res, 200, {
          enabled: true,
          sessions: activeSessions,
          sessionCount: activeSessions.length,
        });
        return true;
      }
      
      default:
        sendJson(res, 404, { error: "Unknown endpoint" });
        return true;
    }
  } catch (err: any) {
    console.error("[Unity] HTTP error:", err);
    sendJson(res, 500, { error: err.message });
    return true;
  }
}

const plugin = {
  id: "unity",
  name: "Unity Bridge",
  description: "Connect Unity Editor to OpenClaw AI assistant",
  
  register(api: OpenClawPluginApi) {
    const logger = api.logger;
    
    // Cleanup timer
    setInterval(cleanupStaleSessions, 30000);
    
    // Register HTTP handler
    api.registerHttpHandler(handleUnityHttpRequest);
    
    // ===== Agent Tools =====
    
    // Tool: Execute a Unity command
    api.registerTool({
      name: "unity_execute",
      description: "Execute a tool in the connected Unity Editor. Available tools: console.getLogs, scene.getData, gameobject.find, gameobject.create, transform.setPosition, component.get, debug.hierarchy, app.getState, and more.",
      parameters: {
        type: "object" as const,
        properties: {
          tool: { type: "string" as const, description: "The Unity tool to execute (e.g., 'debug.hierarchy', 'gameobject.find')" },
          parameters: { type: "object" as const, description: "Parameters for the tool (varies by tool)" },
          sessionId: { type: "string" as const, description: "Optional: specific Unity session ID" },
        },
        required: ["tool"] as const,
      },
      execute: async (_toolCallId: string, args: any) => {
        // Helper to format result for OpenClaw
        const jsonResult = (payload: any) => ({
          content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
          details: payload,
        });
        
        // Extract parameters
        const tool = args?.tool;
        const parameters = args?.parameters;
        const sessionId = args?.sessionId;
        
        if (!tool) {
          return jsonResult({
            success: false,
            error: "Missing 'tool' parameter. Specify which Unity tool to execute (e.g., 'debug.hierarchy', 'gameobject.find').",
          });
        }
        
        // Find session
        let session: UnitySession | undefined;
        
        if (sessionId) {
          session = sessions.get(sessionId);
        } else {
          // Use first active session
          const firstSession = sessions.values().next();
          session = firstSession.done ? undefined : firstSession.value;
        }
        
        if (!session) {
          return jsonResult({
            success: false,
            error: "No Unity session connected. Make sure Unity is running with OpenClaw Bridge in Play mode.",
          });
        }
        
        // Create command
        const requestId = generateId();
        session.pendingCommands.push({
          requestId,
          tool,
          parameters: JSON.stringify(parameters || {}),
          createdAt: Date.now(),
        });
        
        logger.info(`[Unity] Queued command: ${tool} (request: ${requestId})`);
        
        // Wait for result (with timeout)
        const timeout = 60000; // 60 seconds
        const startTime = Date.now();
        
        while (Date.now() - startTime < timeout) {
          if (session.results.has(requestId)) {
            const result = session.results.get(requestId);
            session.results.delete(requestId);
            
            if (result.success) {
              return jsonResult({
                success: true,
                result: result.result,
              });
            } else {
              return jsonResult({
                success: false,
                error: result.error,
              });
            }
          }
          
          await new Promise(resolve => setTimeout(resolve, 100));
        }
        
        return jsonResult({
          success: false,
          error: "Timeout waiting for Unity response. Make sure Unity is in Play mode and the bridge is active.",
        });
      },
    });
    
    // Tool: List Unity sessions
    api.registerTool({
      name: "unity_sessions",
      description: "List all connected Unity Editor sessions",
      parameters: {
        type: "object" as const,
        properties: {
          _dummy: { type: "string" as const, description: "Unused parameter" },
        },
        required: [] as const,
      },
      execute: async (_toolCallId: string, _args: any) => {
        const activeSessions = Array.from(sessions.values()).map(s => ({
          sessionId: s.sessionId,
          project: s.projectName,
          version: s.unityVersion,
          platform: s.platform,
          tools: s.tools.length,
        }));
        
        const payload = {
          success: true,
          sessions: activeSessions,
          count: activeSessions.length,
        };
        
        return {
          content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
          details: payload,
        };
      },
    });
    
    // ===== CLI Commands =====
    
    api.registerCli(
      ({ program }) => {
        const unityCmd = program
          .command("unity")
          .description("Unity Bridge commands");
        
        unityCmd
          .command("status")
          .description("Show Unity connection status")
          .action(() => {
            console.log("\n🎮 Unity Bridge Status\n");
            
            if (sessions.size === 0) {
              console.log("  No Unity sessions connected.\n");
              console.log("  To connect Unity:");
              console.log("  1. Install OpenClaw Unity Bridge package");
              console.log("  2. Configure Gateway URL: http://localhost:18789");
              console.log("  3. Add OpenClawBridge to your scene");
              console.log("  4. Enter Play Mode\n");
              return;
            }
            
            for (const [id, session] of sessions) {
              const age = Math.round((Date.now() - session.registeredAt) / 1000);
              const lastSeen = Math.round((Date.now() - session.lastHeartbeat) / 1000);
              
              console.log(`  ✅ ${session.projectName}`);
              console.log(`     Version: ${session.unityVersion}`);
              console.log(`     Platform: ${session.platform}`);
              console.log(`     Session: ${id}`);
              console.log(`     Connected: ${age}s ago`);
              console.log(`     Last seen: ${lastSeen}s ago`);
              console.log(`     Pending: ${session.pendingCommands.length} commands\n`);
            }
          });
      },
      { commands: ["unity"] }
    );
    
    logger.info("[Unity] Bridge plugin loaded - HTTP endpoints at /unity/*");
  },
};

export default plugin;
