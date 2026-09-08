// extension/index.ts
import { randomBytes, timingSafeEqual } from "node:crypto";
import { chmodSync, mkdirSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";
var ENGINE_LABEL = "Unity";
var ENGINE_KEY = "unity";
var ALLOW_DESTRUCTIVE_ENV_VARS = [
  "OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE",
  "OPENCLAW_UNITY_ALLOW_DESTRUCTIVE"
];
var READ_ONLY_CUSTOM_TOOLS_ENV_VARS = [
  "OPENCLAW_EDITOR_READONLY_CUSTOM_TOOLS",
  "OPENCLAW_UNITY_READONLY_CUSTOM_TOOLS"
];
var READ_ONLY_VERB = /^(get|list|find|search|read|inspect|describe|query|exists|has|count|status|state|info|tree|hierarchy|screenshot|capture)/i;
var NON_MUTATING_TOOLS = /* @__PURE__ */ new Set([
  "debug.log",
  // writes one line to the Editor console
  "editor.focuswindow",
  // moves Editor UI focus
  "editor.listwindows",
  "scriptableobject.load"
  // loads an existing asset for inspection only
]);
var HIGH_RISK_NAME = /(execute|eval|delete|destroy|remove|install|uninstall|build|import|deploy|publish|reset|\brun\b)/i;
var ALWAYS_PROJECT_CHANGING = /* @__PURE__ */ new Set([
  "script.execute",
  "execute_code",
  "execute_custom_tool",
  "manage_tools",
  "manage_script",
  "code.execute"
]);
var BUILTIN_TOOLS = new Set(
  [
    "app.getState",
    "app.pause",
    "app.play",
    "app.stop",
    "asset.copy",
    "asset.delete",
    "asset.find",
    "asset.getPath",
    "asset.import",
    "asset.move",
    "asset.refresh",
    "batch.execute",
    "component.add",
    "component.get",
    "component.list",
    "component.remove",
    "component.set",
    "console.clear",
    "console.getErrors",
    "console.getLogs",
    "debug.hierarchy",
    "debug.log",
    "debug.screenshot",
    "editor.domainReload",
    "editor.focusWindow",
    "editor.getState",
    "editor.listWindows",
    "editor.pause",
    "editor.play",
    "editor.recompile",
    "editor.refresh",
    "editor.stop",
    "editor.unpause",
    "gameobject.create",
    "gameobject.delete",
    "gameobject.destroy",
    "gameobject.find",
    "gameobject.getAll",
    "gameobject.getData",
    "gameobject.setActive",
    "gameobject.setParent",
    "input.clickUI",
    "input.getMousePosition",
    "input.keyDown",
    "input.keyPress",
    "input.keyUp",
    "input.mouseClick",
    "input.mouseDrag",
    "input.mouseMove",
    "input.mouseScroll",
    "input.type",
    "material.assign",
    "material.create",
    "material.getInfo",
    "material.list",
    "material.modify",
    "package.add",
    "package.list",
    "package.remove",
    "package.search",
    "prefab.close",
    "prefab.create",
    "prefab.instantiate",
    "prefab.open",
    "prefab.save",
    "scene.getActive",
    "scene.getData",
    "scene.list",
    "scene.load",
    "scene.open",
    "scene.save",
    "scene.saveAll",
    "script.execute",
    "script.list",
    "script.read",
    "scriptableobject.create",
    "scriptableobject.getField",
    "scriptableobject.list",
    "scriptableobject.load",
    "scriptableobject.save",
    "scriptableobject.setField",
    "session.getInfo",
    "shader.getInfo",
    "shader.getKeywords",
    "shader.list",
    "test.getResults",
    "test.list",
    "test.run",
    "texture.create",
    "texture.getInfo",
    "texture.list",
    "texture.resize",
    "texture.setPixels",
    "transform.getPosition",
    "transform.getRotation",
    "transform.getScale",
    "transform.setPosition",
    "transform.setRotation",
    "transform.setScale"
  ].map((name) => name.toLowerCase())
);
var READ_ONLY_CUSTOM_TOOLS = [];
var BATCH_TOOLS = /* @__PURE__ */ new Set(["batch.execute"]);
var MAX_BATCH_DEPTH = 4;
function currentEnv() {
  return globalThis?.process?.env ?? {};
}
function isTruthyFlag(value) {
  if (value === true) return true;
  if (typeof value === "string") return /^(1|true|yes|on)$/i.test(value.trim());
  return false;
}
function batchCommands(parameters) {
  const commands = parameters?.commands ?? parameters?.calls;
  return Array.isArray(commands) ? commands : null;
}
function readOnlyCustomTools(env = currentEnv()) {
  const declared = [...READ_ONLY_CUSTOM_TOOLS];
  for (const name of READ_ONLY_CUSTOM_TOOLS_ENV_VARS) {
    const raw = env[name];
    if (typeof raw === "string") declared.push(...raw.split(/[,\s]+/));
  }
  const allowed = /* @__PURE__ */ new Set();
  for (const entry of declared) {
    const key = entry.trim().toLowerCase();
    if (!key) continue;
    if (BUILTIN_TOOLS.has(key)) continue;
    if (ALWAYS_PROJECT_CHANGING.has(key)) continue;
    if (HIGH_RISK_NAME.test(key)) continue;
    allowed.add(key);
  }
  return allowed;
}
function classifyTool(tool, parameters, depth = 0, env = currentEnv()) {
  const name = typeof tool === "string" ? tool.trim() : "";
  if (!name) return "project-changing";
  const key = name.toLowerCase();
  if (BATCH_TOOLS.has(key)) {
    if (depth >= MAX_BATCH_DEPTH) return "project-changing";
    const commands = batchCommands(parameters);
    if (!commands || commands.length === 0) return "project-changing";
    for (const command of commands) {
      if (!command || typeof command !== "object") return "project-changing";
      const sub = classifyTool(
        command.tool,
        command.params ?? command.parameters,
        depth + 1,
        env
      );
      if (sub === "project-changing") return "project-changing";
    }
    return "read-only";
  }
  if (NON_MUTATING_TOOLS.has(key)) return "read-only";
  if (ALWAYS_PROJECT_CHANGING.has(key)) return "project-changing";
  if (HIGH_RISK_NAME.test(key)) return "project-changing";
  if (!BUILTIN_TOOLS.has(key)) {
    return readOnlyCustomTools(env).has(key) ? "read-only" : "project-changing";
  }
  const verb = key.includes(".") ? key.slice(key.lastIndexOf(".") + 1) : key;
  return READ_ONLY_VERB.test(verb) ? "read-only" : "project-changing";
}
function isCustomTool(tool) {
  const name = typeof tool === "string" ? tool.trim().toLowerCase() : "";
  if (!name) return false;
  return !BUILTIN_TOOLS.has(name);
}
function projectChangingTools(tool, parameters, depth = 0, env = currentEnv()) {
  const name = typeof tool === "string" && tool.trim() ? tool.trim() : "(unnamed tool)";
  const key = name.toLowerCase();
  if (BATCH_TOOLS.has(key) && depth < MAX_BATCH_DEPTH) {
    const commands = batchCommands(parameters);
    if (!commands || commands.length === 0) return [name];
    const found = [];
    for (const command of commands) {
      if (!command || typeof command !== "object") {
        found.push(`${name} (malformed command)`);
        continue;
      }
      found.push(
        ...projectChangingTools(
          command.tool,
          command.params ?? command.parameters,
          depth + 1,
          env
        )
      );
    }
    return found;
  }
  return classifyTool(name, parameters, depth, env) === "project-changing" ? [name] : [];
}
function destructiveOperationsEnabled(env = currentEnv()) {
  return ALLOW_DESTRUCTIVE_ENV_VARS.some((name) => isTruthyFlag(env[name]));
}
function evaluateDestructiveGate(input) {
  const env = input.env ?? currentEnv();
  const risk = classifyTool(input.tool, input.parameters, 0, env);
  const custom = isCustomTool(input.tool);
  if (risk === "read-only") {
    return { allowed: true, risk, tools: [], custom, reason: "read-only" };
  }
  const tools = projectChangingTools(input.tool, input.parameters, 0, env);
  const listed = tools.length > 0 ? tools.join(", ") : "this call";
  const customNote = custom ? ` ${tools[0] ?? "That name"} is not a built-in tool, so it is treated as a project-registered custom tool: custom tools are project-changing whatever their name reads like.` : "";
  if (!destructiveOperationsEnabled(env)) {
    return {
      allowed: false,
      risk,
      tools,
      custom,
      reason: "operator-opt-in-missing",
      message: `Refused: ${listed} would change this ${ENGINE_LABEL} project, and this gateway was not started with project-changing operations enabled. Ask the user to confirm the change, restart the gateway with ${ALLOW_DESTRUCTIVE_ENV_VARS[0]}=1 (or ${ALLOW_DESTRUCTIVE_ENV_VARS[1]}=1), then repeat the call with confirm: true. Read-only tools are unaffected; pass dryRun: true to preview a call without sending it.` + customNote
    };
  }
  if (!isTruthyFlag(input.confirm)) {
    return {
      allowed: false,
      risk,
      tools,
      custom,
      reason: "confirmation-missing",
      message: `Refused: ${listed} would change this ${ENGINE_LABEL} project. Confirm the change with the user, then repeat this call with confirm: true. Pass dryRun: true to preview it first.` + customNote
    };
  }
  return { allowed: true, risk, tools, custom, reason: "confirmed" };
}
var BRIDGE_TOKEN_HEADER = "x-openclaw-bridge-token";
var SESSION_TOKEN_HEADER = "x-openclaw-session";
var BRIDGE_TOKEN_FILENAME = `${ENGINE_KEY}-bridge.token`;
var ALLOW_LEGACY_ENV_VARS = [
  "OPENCLAW_EDITOR_ALLOW_LEGACY_UNAUTHENTICATED",
  "OPENCLAW_UNITY_ALLOW_LEGACY_UNAUTHENTICATED"
];
function legacyUnauthenticatedEnabled(env = currentEnv()) {
  return ALLOW_LEGACY_ENV_VARS.some((name) => isTruthyFlag(env[name]));
}
function randomToken(bytes = 32) {
  return randomBytes(bytes).toString("hex");
}
function tokensMatch(candidate, expected) {
  if (typeof candidate !== "string" || candidate.length === 0) return false;
  const a = Buffer.from(candidate, "utf8");
  const b = Buffer.from(expected, "utf8");
  if (a.length !== b.length) {
    timingSafeEqual(b, b);
    return false;
  }
  return timingSafeEqual(a, b);
}
function isLoopbackAddress(address) {
  if (typeof address !== "string" || !address) return false;
  let value = address.trim().toLowerCase();
  const zone = value.indexOf("%");
  if (zone >= 0) value = value.slice(0, zone);
  if (value.startsWith("::ffff:")) value = value.slice(7);
  if (value === "::1" || value === "0:0:0:0:0:0:0:1") return true;
  return /^127\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(value);
}
function configDirectory(env) {
  const explicit = env.OPENCLAW_CONFIG_DIR || env.OPENCLAW_HOME;
  if (explicit) return explicit;
  return join(env.HOME || homedir(), ".openclaw");
}
var consoleLogger = {
  info: (message) => console.log(message),
  warn: (message) => console.warn(message),
  error: (message) => console.error(message)
};
var STALE_SESSION_MS = 12e4;
function createBridge(options = {}) {
  const env = options.env ?? currentEnv();
  const logger = options.logger ?? consoleLogger;
  const sessions = /* @__PURE__ */ new Map();
  const provided = typeof options.secret === "string" && options.secret.length > 0 ? options.secret : typeof env.OPENCLAW_BRIDGE_TOKEN === "string" && env.OPENCLAW_BRIDGE_TOKEN ? env.OPENCLAW_BRIDGE_TOKEN : null;
  const secret = provided ?? randomToken();
  const shouldPersist = options.persist ?? provided === null;
  let tokenFile = null;
  if (shouldPersist) {
    try {
      const dir = configDirectory(env);
      mkdirSync(dir, { recursive: true, mode: 448 });
      const file = join(dir, BRIDGE_TOKEN_FILENAME);
      writeFileSync(file, `${secret}
`, { mode: 384 });
      chmodSync(file, 384);
      tokenFile = file;
    } catch (err) {
      logger.error(
        `[${ENGINE_LABEL}] Could not write the bridge token file: ${err?.message}. The Editor add-on will not be able to connect until it can read the token.`
      );
    }
  }
  let legacyWarnedAt = 0;
  function warnLegacyOnce() {
    const now = Date.now();
    if (now - legacyWarnedAt < 6e4) return;
    legacyWarnedAt = now;
    logger.warn(
      `[${ENGINE_LABEL}] SECURITY: the bridge is running in legacy unauthenticated mode (${ALLOW_LEGACY_ENV_VARS[1]}). Any process on this machine can drive the Editor and forge tool results. Unset it and update the Editor add-on.`
    );
  }
  function generateSessionId() {
    return `${ENGINE_KEY}_${randomToken(12)}`;
  }
  function cleanupStaleSessions(now = Date.now()) {
    for (const [id, session] of sessions) {
      if (now - session.lastHeartbeat > STALE_SESSION_MS) {
        sessions.delete(id);
      }
    }
  }
  function queueCommand(session, tool, parameters) {
    const command = {
      toolCallId: `${ENGINE_KEY}_req_${randomToken(12)}`,
      tool,
      arguments: parameters || {},
      createdAt: Date.now(),
      nonce: randomToken(16)
    };
    session.pendingCommands.push(command);
    session.inflight.set(command.toolCallId, {
      nonce: command.nonce,
      tool,
      createdAt: command.createdAt
    });
    return command;
  }
  function forgetCommand(session, toolCallId) {
    session.inflight.delete(toolCallId);
    session.results.delete(toolCallId);
    const index = session.pendingCommands.findIndex(
      (command) => command.toolCallId === toolCallId
    );
    if (index >= 0) session.pendingCommands.splice(index, 1);
  }
  function resolveSession(sessionId) {
    if (typeof sessionId === "string" && sessionId) {
      const session = sessions.get(sessionId);
      if (!session) {
        return {
          error: `No ${ENGINE_LABEL} session with id ${sessionId}. Call ${ENGINE_KEY}_sessions to list the connected Editors.`
        };
      }
      return { session };
    }
    if (sessions.size === 0) {
      return {
        error: `No ${ENGINE_LABEL} session connected. Make sure ${ENGINE_LABEL} Editor is running with the OpenClaw add-on enabled and that it can read the bridge token.`
      };
    }
    if (sessions.size > 1) {
      const ids = Array.from(sessions.values()).map((s) => `${s.sessionId} (${s.projectName})`).join(", ");
      return {
        error: `${sessions.size} ${ENGINE_LABEL} Editors are connected, so this call needs an explicit sessionId \u2014 one session must not drive another's Editor. Connected: ${ids}.`
      };
    }
    return { session: sessions.values().next().value };
  }
  function listSessions() {
    return Array.from(sessions.values()).map((s) => ({
      sessionId: s.sessionId,
      project: s.projectName,
      version: s.unityVersion,
      platform: s.platform,
      tools: s.toolCount,
      connectedAt: new Date(s.registeredAt).toISOString(),
      lastSeen: new Date(s.lastHeartbeat).toISOString(),
      pendingCommands: s.pendingCommands.length
    }));
  }
  function header(req, name) {
    const value = req.headers?.[name];
    if (Array.isArray(value)) return value[0];
    return typeof value === "string" ? value : void 0;
  }
  function authenticateBridge(req, res) {
    if (tokensMatch(header(req, BRIDGE_TOKEN_HEADER), secret)) return true;
    if (legacyUnauthenticatedEnabled(env)) {
      warnLegacyOnce();
      return true;
    }
    sendJson(res, 401, {
      error: `Missing or invalid ${BRIDGE_TOKEN_HEADER} header. The OpenClaw ${ENGINE_LABEL} add-on reads the per-launch bridge token from ${tokenFile ?? configDirectory(env) + "/" + BRIDGE_TOKEN_FILENAME}.`
    });
    return false;
  }
  function authenticateSession(req, res, sessionId) {
    const session = typeof sessionId === "string" ? sessions.get(sessionId) : void 0;
    if (!session) {
      sendJson(res, 404, { error: "Session not found" });
      return null;
    }
    if (tokensMatch(header(req, SESSION_TOKEN_HEADER), session.sessionToken)) {
      return session;
    }
    if (legacyUnauthenticatedEnabled(env)) {
      warnLegacyOnce();
      return session;
    }
    sendJson(res, 401, {
      error: `Missing or invalid ${SESSION_TOKEN_HEADER} header for this session.`
    });
    return null;
  }
  async function handleRequest(req, res) {
    const url = new URL(req.url ?? "/", "http://127.0.0.1");
    const path = url.pathname;
    if (!path.startsWith(`/${ENGINE_KEY}/`)) {
      return false;
    }
    const remote = req.socket?.remoteAddress;
    if (!isLoopbackAddress(remote)) {
      sendJson(res, 403, {
        error: `The ${ENGINE_LABEL} bridge only accepts connections from 127.0.0.1.`
      });
      return true;
    }
    if (header(req, "origin") || header(req, "referer")) {
      sendJson(res, 403, {
        error: "Browser-originated requests are not accepted by this bridge."
      });
      return true;
    }
    if (req.method === "OPTIONS") {
      sendJson(res, 405, { error: "Method not allowed" });
      return true;
    }
    const endpoint = path.slice(`/${ENGINE_KEY}/`.length);
    try {
      switch (endpoint) {
        case "register": {
          if (req.method !== "POST") {
            sendJson(res, 405, { error: "Method not allowed" });
            return true;
          }
          if (!authenticateBridge(req, res)) return true;
          const body = await readJsonBody(req);
          const { project, version, platform, tools } = body;
          const sessionId = generateSessionId();
          const session = {
            sessionId,
            sessionToken: randomToken(),
            registeredAt: Date.now(),
            lastHeartbeat: Date.now(),
            projectName: typeof project === "string" ? project : "Unknown",
            unityVersion: typeof version === "string" ? version : "Unknown",
            platform: typeof platform === "string" ? platform : "UnityEditor",
            toolCount: typeof tools === "number" ? tools : 0,
            pendingCommands: [],
            inflight: /* @__PURE__ */ new Map(),
            results: /* @__PURE__ */ new Map()
          };
          sessions.set(sessionId, session);
          logger.info(
            `[${ENGINE_LABEL}] Registered: ${session.projectName} (${session.unityVersion}) - Session: ${sessionId}`
          );
          sendJson(res, 200, {
            sessionId,
            sessionToken: session.sessionToken,
            sessionTokenHeader: SESSION_TOKEN_HEADER,
            status: "connected"
          });
          return true;
        }
        case "heartbeat": {
          if (req.method !== "POST") {
            sendJson(res, 405, { error: "Method not allowed" });
            return true;
          }
          const body = await readJsonBody(req);
          const session = authenticateSession(req, res, body?.sessionId);
          if (!session) return true;
          session.lastHeartbeat = Date.now();
          sendJson(res, 200, { ok: true });
          return true;
        }
        case "poll": {
          const session = authenticateSession(
            req,
            res,
            url.searchParams.get("sessionId")
          );
          if (!session) return true;
          session.lastHeartbeat = Date.now();
          if (session.pendingCommands.length > 0) {
            const command = session.pendingCommands.shift();
            sendJson(res, 200, command);
          } else {
            res.statusCode = 204;
            res.end();
          }
          return true;
        }
        case "result": {
          if (req.method !== "POST") {
            sendJson(res, 405, { error: "Method not allowed" });
            return true;
          }
          const body = await readJsonBody(req);
          const session = authenticateSession(req, res, body?.sessionId);
          if (!session) return true;
          const { toolCallId, nonce, result } = body;
          const inflight = typeof toolCallId === "string" ? session.inflight.get(toolCallId) : void 0;
          if (!inflight) {
            logger.warn(
              `[${ENGINE_LABEL}] Dropped a result for an unknown tool call on session ${session.sessionId}`
            );
            sendJson(res, 409, {
              error: "Unknown or already-completed toolCallId for this session"
            });
            return true;
          }
          const nonceOk = tokensMatch(nonce, inflight.nonce);
          if (!nonceOk && !legacyUnauthenticatedEnabled(env)) {
            logger.warn(
              `[${ENGINE_LABEL}] Dropped a result with a mismatched nonce for ${toolCallId}`
            );
            sendJson(res, 409, {
              error: "Result nonce does not match the nonce this tool call was issued with"
            });
            return true;
          }
          if (!nonceOk) warnLegacyOnce();
          session.inflight.delete(toolCallId);
          session.results.set(toolCallId, result);
          logger.info(`[${ENGINE_LABEL}] Tool result received for: ${toolCallId}`);
          sendJson(res, 200, { ok: true });
          return true;
        }
        case "status": {
          if (!authenticateBridge(req, res)) return true;
          const activeSessions = listSessions();
          sendJson(res, 200, {
            enabled: true,
            auth: legacyUnauthenticatedEnabled(env) ? "legacy-unauthenticated" : "token",
            destructiveOperations: destructiveOperationsEnabled(env) ? "enabled" : "blocked",
            sessions: activeSessions,
            sessionCount: activeSessions.length
          });
          return true;
        }
        default:
          sendJson(res, 404, { error: "Unknown endpoint" });
          return true;
      }
    } catch (err) {
      logger.error(`[${ENGINE_LABEL}] HTTP error: ${err?.message}`);
      sendJson(res, 500, { error: err?.message });
      return true;
    }
  }
  return {
    get secret() {
      return secret;
    },
    get tokenFile() {
      return tokenFile;
    },
    sessions,
    handleRequest,
    queueCommand,
    forgetCommand,
    resolveSession,
    listSessions,
    cleanupStaleSessions
  };
}
async function readJsonBody(req, maxBytes = 1024 * 1024) {
  const chunks = [];
  let total = 0;
  return new Promise((resolve, reject) => {
    req.on("data", (chunk) => {
      total += chunk.length;
      if (total > maxBytes) {
        reject(new Error("Payload too large"));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => {
      const raw = Buffer.concat(chunks).toString("utf8");
      if (!raw.trim()) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(raw));
      } catch (err) {
        const repaired = raw.replace(/(:\s*-?\d+),(\d+\s*[,}\]])/g, "$1.$2");
        if (repaired !== raw) {
          try {
            resolve(JSON.parse(repaired));
            return;
          } catch {
          }
        }
        reject(err);
      }
    });
    req.on("error", reject);
  });
}
function sendJson(res, status, data) {
  res.statusCode = status;
  res.setHeader("Content-Type", "application/json");
  res.setHeader("Cache-Control", "no-store");
  res.end(JSON.stringify(data));
}
var plugin = {
  id: "unity",
  name: "Unity Plugin",
  description: "Connect Unity Editor to OpenClaw AI assistant",
  register(api) {
    const logger = api.logger;
    const bridge = createBridge({ logger });
    if (legacyUnauthenticatedEnabled()) {
      logger.warn(
        "[Unity] SECURITY: legacy unauthenticated bridge mode is enabled (OPENCLAW_UNITY_ALLOW_LEGACY_UNAUTHENTICATED). Any local process can drive the Editor and forge tool results. Unset it as soon as the Editor add-on is updated."
      );
    } else if (bridge.tokenFile) {
      logger.info(`[Unity] Bridge token written to ${bridge.tokenFile} (mode 0600)`);
    }
    setInterval(() => bridge.cleanupStaleSessions(), 3e4);
    const httpApi = api;
    if (typeof httpApi.registerHttpRoute === "function") {
      httpApi.registerHttpRoute({
        path: "/unity",
        auth: "plugin",
        match: "prefix",
        handler: bridge.handleRequest
      });
    } else {
      httpApi.registerHttpHandler(bridge.handleRequest);
    }
    api.registerTool({
      name: "unity_execute",
      description: "Execute a tool in the connected Unity Editor. Available tools: console.getLogs, scene.getData, gameobject.find, gameobject.create, gameobject.delete, gameobject.setActive, transform.setPosition, transform.setRotation, transform.setScale, component.get, component.add, debug.hierarchy, debug.screenshot, app.getState, app.play, app.stop, input.simulateKey, input.simulateMouse, and more. Read-only built-in tools (get*/list/find/read/debug.hierarchy/debug.screenshot) run directly. Project-changing tools \u2014 and every project-registered custom tool, whatever its name reads like \u2014 are refused unless the gateway was started with OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1 and the call passes confirm: true after the user approved the change; dryRun: true previews any call without sending it.",
      parameters: {
        type: "object",
        properties: {
          tool: { type: "string", description: "The Unity tool to execute (e.g., 'debug.hierarchy', 'gameobject.find')" },
          parameters: { type: "object", description: "Parameters for the tool (varies by tool)" },
          sessionId: { type: "string", description: "Which connected Editor to drive. Required when more than one is connected." },
          confirm: {
            type: "boolean",
            description: "Required for project-changing tools (create/delete/save/set*, package.add, script.execute, input.*, editor.play) and for every custom tool that has not been declared read-only. Set it only after the user has approved that specific change. Read-only tools ignore it."
          },
          dryRun: {
            type: "boolean",
            description: "Preview only: report how the call is classified and what would be sent to the Editor, without sending it."
          }
        },
        required: ["tool"]
      },
      execute: async (toolCallId, args) => {
        const jsonResult = (payload) => ({
          content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
          details: payload
        });
        const tool = args?.tool;
        const parameters = args?.parameters;
        const sessionId = args?.sessionId;
        if (!tool) {
          return jsonResult({
            success: false,
            error: "Missing 'tool' parameter. Specify which Unity tool to execute (e.g., 'debug.hierarchy', 'gameobject.find')."
          });
        }
        const gate = evaluateDestructiveGate({
          tool,
          parameters,
          confirm: args?.confirm
        });
        if (isTruthyFlag(args?.dryRun)) {
          return jsonResult({
            success: true,
            dryRun: true,
            executed: false,
            tool,
            parameters: parameters || {},
            risk: gate.risk,
            custom: gate.custom,
            projectChangingTools: gate.tools,
            requiresConfirmation: gate.risk === "project-changing",
            destructiveOperationsEnabled: destructiveOperationsEnabled(),
            wouldRun: gate.allowed,
            gate: { reason: gate.reason, message: gate.message }
          });
        }
        if (!gate.allowed) {
          logger.info(
            `[Unity] Refused ${gate.risk} call: ${gate.tools.join(", ") || tool} (${gate.reason})`
          );
          return jsonResult({
            success: false,
            error: gate.message,
            gate: {
              allowed: false,
              risk: gate.risk,
              reason: gate.reason,
              custom: gate.custom,
              tools: gate.tools
            }
          });
        }
        const resolved = bridge.resolveSession(sessionId);
        if (!resolved.session) {
          return jsonResult({ success: false, error: resolved.error });
        }
        const session = resolved.session;
        const command = bridge.queueCommand(session, tool, parameters);
        logger.info(`[Unity] Queued command: ${tool} (request: ${command.toolCallId})`);
        const timeout = 6e4;
        const startTime = Date.now();
        while (Date.now() - startTime < timeout) {
          if (session.results.has(command.toolCallId)) {
            const result = session.results.get(command.toolCallId);
            session.results.delete(command.toolCallId);
            return jsonResult({
              success: true,
              result
            });
          }
          await new Promise((resolve) => setTimeout(resolve, 100));
        }
        bridge.forgetCommand(session, command.toolCallId);
        return jsonResult({
          success: false,
          error: "Timeout waiting for Unity response. Make sure the OpenClaw plugin is enabled in Unity Editor."
        });
      }
    });
    api.registerTool({
      name: "unity_sessions",
      description: "List all connected Unity Editor sessions",
      parameters: {
        type: "object",
        properties: {
          _dummy: { type: "string", description: "Unused parameter" }
        },
        required: []
      },
      execute: async (_toolCallId, _args) => {
        const activeSessions = bridge.listSessions();
        const payload = {
          success: true,
          sessions: activeSessions,
          count: activeSessions.length
        };
        return {
          content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
          details: payload
        };
      }
    });
    api.registerCli(
      ({ program }) => {
        const unityCmd = program.command("unity").description("Unity Plugin commands");
        unityCmd.command("status").description("Show Unity connection status").action(() => {
          console.log("\n\u{1F3AE} Unity Plugin Status\n");
          console.log(
            legacyUnauthenticatedEnabled() ? "  Bridge auth: LEGACY UNAUTHENTICATED (any local process can drive the Editor)\n" : `  Bridge auth: token (${bridge.tokenFile ?? "token file unavailable"})
`
          );
          console.log(
            destructiveOperationsEnabled() ? "  Project-changing tools: ENABLED (confirm: true still required per call)\n" : "  Project-changing tools: BLOCKED (start the gateway with OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1 to enable)\n"
          );
          if (bridge.sessions.size === 0) {
            console.log("  No Unity sessions connected.\n");
            console.log("  To connect Unity:");
            console.log("  1. Install OpenClaw Unity Plugin package");
            console.log("  2. Configure Gateway URL: http://localhost:18789");
            console.log("  3. Enable plugin in Window > OpenClaw");
            console.log("  4. Check connection status\n");
            return;
          }
          for (const [id, session] of bridge.sessions) {
            const age = Math.round((Date.now() - session.registeredAt) / 1e3);
            const lastSeen = Math.round((Date.now() - session.lastHeartbeat) / 1e3);
            console.log(`  \u2705 ${session.projectName}`);
            console.log(`     Version: Unity ${session.unityVersion}`);
            console.log(`     Platform: ${session.platform}`);
            console.log(`     Session: ${id}`);
            console.log(`     Connected: ${age}s ago`);
            console.log(`     Last seen: ${lastSeen}s ago`);
            console.log(`     Pending: ${session.pendingCommands.length} commands
`);
          }
        });
      },
      { commands: ["unity"] }
    );
    logger.info("[Unity] Plugin loaded - authenticated HTTP endpoints at /unity/*");
  }
};
var extension_default = plugin;
export {
  ALWAYS_PROJECT_CHANGING,
  BRIDGE_TOKEN_FILENAME,
  BRIDGE_TOKEN_HEADER,
  BUILTIN_TOOLS,
  READ_ONLY_CUSTOM_TOOLS,
  SESSION_TOKEN_HEADER,
  classifyTool,
  createBridge,
  extension_default as default,
  destructiveOperationsEnabled,
  evaluateDestructiveGate,
  isCustomTool,
  isLoopbackAddress,
  legacyUnauthenticatedEnabled,
  projectChangingTools,
  readOnlyCustomTools,
  tokensMatch
};
