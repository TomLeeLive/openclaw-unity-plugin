/**
 * OpenClaw Unity Plugin
 * Connects Unity Editor to OpenClaw AI assistant over an authenticated,
 * loopback-only HTTP bridge.
 */

import { randomBytes, timingSafeEqual } from "node:crypto";
import { chmodSync, mkdirSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";
import type { IncomingMessage, ServerResponse } from "node:http";
import type { OpenClawPluginApi } from "openclaw/plugin-sdk/core";

const ENGINE_LABEL = "Unity";
const ENGINE_KEY = "unity";

// ===== Safety gate: runtime confirmation for project-changing operations =====
//
// Read-only tools (get*/list/find/read/hierarchy/screenshot/...) behave exactly
// as before. Anything that can change the project — creating or deleting
// GameObjects and assets, saving scenes, installing packages, running C# via
// script.execute, simulating input, entering Play mode — is refused by default
// and needs two independent opt-ins:
//
//   1. the operator starts the gateway with OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1
//      (or OPENCLAW_UNITY_ALLOW_DESTRUCTIVE=1), and
//   2. the caller passes confirm: true on that individual call, after asking
//      the user about that specific change.
//
// A name that is not in this skill's built-in catalogue is a project-registered
// custom tool. Custom tools are project-changing by default whatever their verb
// reads like — the Editor add-on lets a project register a tool called
// `mygame.getScore` that formats the drive — and only the explicit allowlist
// below (or the operator's OPENCLAW_UNITY_READONLY_CUSTOM_TOOLS) can declare one
// read-only. execute_code, manage_tools, execute_custom_tool and script.execute
// can never be declared read-only. Pass dryRun: true to preview any call.

/** Environment variables that let the operator enable project-changing calls. */
const ALLOW_DESTRUCTIVE_ENV_VARS = [
  "OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE",
  "OPENCLAW_UNITY_ALLOW_DESTRUCTIVE",
];

/** Environment variables that extend the read-only custom-tool allowlist. */
const READ_ONLY_CUSTOM_TOOLS_ENV_VARS = [
  "OPENCLAW_EDITOR_READONLY_CUSTOM_TOOLS",
  "OPENCLAW_UNITY_READONLY_CUSTOM_TOOLS",
];

export type ToolRisk = "read-only" | "project-changing";

/** Verbs that only read Editor/project state. */
const READ_ONLY_VERB =
  /^(get|list|find|search|read|inspect|describe|query|exists|has|count|status|state|info|tree|hierarchy|screenshot|capture)/i;

/** Tools that write something, but never the project or its assets. */
const NON_MUTATING_TOOLS = new Set([
  "debug.log",              // writes one line to the Editor console
  "editor.focuswindow",     // moves Editor UI focus
  "editor.listwindows",
  "scriptableobject.load",  // loads an existing asset for inspection only
]);

/** Names that are project-changing even when the verb reads like a query. */
const HIGH_RISK_NAME =
  /(execute|eval|delete|destroy|remove|install|uninstall|build|import|deploy|publish|reset|\brun\b)/i;

/**
 * Tools that are project-changing no matter what any allowlist says: they run
 * caller-supplied code, or they change which tools exist.
 */
export const ALWAYS_PROJECT_CHANGING = new Set([
  "script.execute",
  "execute_code",
  "execute_custom_tool",
  "manage_tools",
  "manage_script",
  "code.execute",
]);

/**
 * Every tool this skill documents in references/tools.md. A name outside this
 * set is a project-registered custom tool, not a built-in.
 */
export const BUILTIN_TOOLS = new Set(
  [
    "app.getState", "app.pause", "app.play", "app.stop",
    "asset.copy", "asset.delete", "asset.find", "asset.getPath", "asset.import",
    "asset.move", "asset.refresh",
    "batch.execute",
    "component.add", "component.get", "component.list", "component.remove",
    "component.set",
    "console.clear", "console.getErrors", "console.getLogs",
    "debug.hierarchy", "debug.log", "debug.screenshot",
    "editor.domainReload", "editor.focusWindow", "editor.getState",
    "editor.listWindows", "editor.pause", "editor.play", "editor.recompile",
    "editor.refresh", "editor.stop", "editor.unpause",
    "gameobject.create", "gameobject.delete", "gameobject.destroy",
    "gameobject.find", "gameobject.getAll", "gameobject.getData",
    "gameobject.setActive", "gameobject.setParent",
    "input.clickUI", "input.getMousePosition", "input.keyDown", "input.keyPress",
    "input.keyUp", "input.mouseClick", "input.mouseDrag", "input.mouseMove",
    "input.mouseScroll", "input.type",
    "material.assign", "material.create", "material.getInfo", "material.list",
    "material.modify",
    "package.add", "package.list", "package.remove", "package.search",
    "prefab.close", "prefab.create", "prefab.instantiate", "prefab.open",
    "prefab.save",
    "scene.getActive", "scene.getData", "scene.list", "scene.load", "scene.open",
    "scene.save", "scene.saveAll",
    "script.execute", "script.list", "script.read",
    "scriptableobject.create", "scriptableobject.getField",
    "scriptableobject.list", "scriptableobject.load", "scriptableobject.save",
    "scriptableobject.setField",
    "session.getInfo",
    "shader.getInfo", "shader.getKeywords", "shader.list",
    "test.getResults", "test.list", "test.run",
    "texture.create", "texture.getInfo", "texture.list", "texture.resize",
    "texture.setPixels",
    "transform.getPosition", "transform.getRotation", "transform.getScale",
    "transform.setPosition", "transform.setRotation", "transform.setScale",
  ].map((name) => name.toLowerCase())
);

/**
 * Custom (project-registered) tools this skill declares read-only. Empty by
 * default: a custom tool is gated until someone who has read its implementation
 * puts its exact name here, or the operator lists it in
 * OPENCLAW_UNITY_READONLY_CUSTOM_TOOLS. Names that run code or change the tool
 * set are ignored even when listed.
 */
export const READ_ONLY_CUSTOM_TOOLS: readonly string[] = [];

/** Tools whose real risk is the risk of the commands they carry. */
const BATCH_TOOLS = new Set(["batch.execute"]);
const MAX_BATCH_DEPTH = 4;

function currentEnv(): Record<string, string | undefined> {
  return (globalThis as any)?.process?.env ?? {};
}

function isTruthyFlag(value: unknown): boolean {
  if (value === true) return true;
  if (typeof value === "string") return /^(1|true|yes|on)$/i.test(value.trim());
  return false;
}

function batchCommands(parameters: any): any[] | null {
  const commands = parameters?.commands ?? parameters?.calls;
  return Array.isArray(commands) ? commands : null;
}

/** Custom tool names currently declared read-only, lowercased. */
export function readOnlyCustomTools(
  env: Record<string, string | undefined> = currentEnv()
): Set<string> {
  const declared = [...READ_ONLY_CUSTOM_TOOLS];
  for (const name of READ_ONLY_CUSTOM_TOOLS_ENV_VARS) {
    const raw = env[name];
    if (typeof raw === "string") declared.push(...raw.split(/[,\s]+/));
  }

  const allowed = new Set<string>();
  for (const entry of declared) {
    const key = entry.trim().toLowerCase();
    if (!key) continue;
    // A built-in keeps its own classification, and nothing that runs code or
    // rewrites the tool set can be declared read-only.
    if (BUILTIN_TOOLS.has(key)) continue;
    if (ALWAYS_PROJECT_CHANGING.has(key)) continue;
    if (HIGH_RISK_NAME.test(key)) continue;
    allowed.add(key);
  }
  return allowed;
}

/**
 * Classify one tool call. Fails closed: anything unrecognised, unnamed or
 * malformed counts as project-changing, and so does every custom tool that has
 * not been explicitly declared read-only.
 */
export function classifyTool(
  tool: unknown,
  parameters?: Record<string, any>,
  depth = 0,
  env: Record<string, string | undefined> = currentEnv()
): ToolRisk {
  const name = typeof tool === "string" ? tool.trim() : "";
  if (!name) return "project-changing";
  const key = name.toLowerCase();

  // A batch is exactly as risky as the riskiest command inside it.
  if (BATCH_TOOLS.has(key)) {
    if (depth >= MAX_BATCH_DEPTH) return "project-changing";
    const commands = batchCommands(parameters);
    if (!commands || commands.length === 0) return "project-changing";
    for (const command of commands) {
      if (!command || typeof command !== "object") return "project-changing";
      const sub = classifyTool(
        (command as any).tool,
        (command as any).params ?? (command as any).parameters,
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

  // Not a built-in: a project-registered custom tool. Its name says nothing
  // about what it does, so it is gated unless it was declared read-only.
  if (!BUILTIN_TOOLS.has(key)) {
    return readOnlyCustomTools(env).has(key) ? "read-only" : "project-changing";
  }

  const verb = key.includes(".") ? key.slice(key.lastIndexOf(".") + 1) : key;
  return READ_ONLY_VERB.test(verb) ? "read-only" : "project-changing";
}

/** True when a tool name is not part of this skill's built-in catalogue. */
export function isCustomTool(tool: unknown): boolean {
  const name = typeof tool === "string" ? tool.trim().toLowerCase() : "";
  if (!name) return false;
  return !BUILTIN_TOOLS.has(name);
}

/** The project-changing tool names a call would run (flattens batches). */
export function projectChangingTools(
  tool: unknown,
  parameters?: Record<string, any>,
  depth = 0,
  env: Record<string, string | undefined> = currentEnv()
): string[] {
  const name =
    typeof tool === "string" && tool.trim() ? tool.trim() : "(unnamed tool)";
  const key = name.toLowerCase();

  if (BATCH_TOOLS.has(key) && depth < MAX_BATCH_DEPTH) {
    const commands = batchCommands(parameters);
    if (!commands || commands.length === 0) return [name];
    const found: string[] = [];
    for (const command of commands) {
      if (!command || typeof command !== "object") {
        found.push(`${name} (malformed command)`);
        continue;
      }
      found.push(
        ...projectChangingTools(
          (command as any).tool,
          (command as any).params ?? (command as any).parameters,
          depth + 1,
          env
        )
      );
    }
    return found;
  }

  return classifyTool(name, parameters, depth, env) === "project-changing"
    ? [name]
    : [];
}

/** True when the operator started the gateway with project changes enabled. */
export function destructiveOperationsEnabled(
  env: Record<string, string | undefined> = currentEnv()
): boolean {
  return ALLOW_DESTRUCTIVE_ENV_VARS.some((name) => isTruthyFlag(env[name]));
}

export interface GateDecision {
  allowed: boolean;
  risk: ToolRisk;
  /** Project-changing tools this call would run. */
  tools: string[];
  /** True when the call names a tool outside the built-in catalogue. */
  custom: boolean;
  reason:
    | "read-only"
    | "confirmed"
    | "operator-opt-in-missing"
    | "confirmation-missing";
  message?: string;
}

/**
 * The runtime confirmation control. Read-only calls pass through untouched;
 * project-changing calls require operator opt-in AND per-call confirm: true.
 */
export function evaluateDestructiveGate(input: {
  tool: unknown;
  parameters?: Record<string, any>;
  confirm?: unknown;
  env?: Record<string, string | undefined>;
}): GateDecision {
  const env = input.env ?? currentEnv();
  const risk = classifyTool(input.tool, input.parameters, 0, env);
  const custom = isCustomTool(input.tool);

  if (risk === "read-only") {
    return { allowed: true, risk, tools: [], custom, reason: "read-only" };
  }

  const tools = projectChangingTools(input.tool, input.parameters, 0, env);
  const listed = tools.length > 0 ? tools.join(", ") : "this call";
  const customNote = custom
    ? ` ${tools[0] ?? "That name"} is not a built-in tool, so it is treated as a ` +
      `project-registered custom tool: custom tools are project-changing whatever ` +
      `their name reads like.`
    : "";

  if (!destructiveOperationsEnabled(env)) {
    return {
      allowed: false,
      risk,
      tools,
      custom,
      reason: "operator-opt-in-missing",
      message:
        `Refused: ${listed} would change this ${ENGINE_LABEL} project, and this gateway was ` +
        `not started with project-changing operations enabled. Ask the user to confirm the ` +
        `change, restart the gateway with ${ALLOW_DESTRUCTIVE_ENV_VARS[0]}=1 (or ` +
        `${ALLOW_DESTRUCTIVE_ENV_VARS[1]}=1), then repeat the call with confirm: true. ` +
        `Read-only tools are unaffected; pass dryRun: true to preview a call without sending it.` +
        customNote,
    };
  }

  if (!isTruthyFlag(input.confirm)) {
    return {
      allowed: false,
      risk,
      tools,
      custom,
      reason: "confirmation-missing",
      message:
        `Refused: ${listed} would change this ${ENGINE_LABEL} project. Confirm the change with ` +
        `the user, then repeat this call with confirm: true. Pass dryRun: true to preview it first.` +
        customNote,
    };
  }

  return { allowed: true, risk, tools, custom, reason: "confirmed" };
}

// ===== Bridge authentication =====
//
// The /unity/* endpoints are the editor add-on's half of the bridge. Before
// 1.8.0 they were unauthenticated: any process on the machine — and, thanks to
// a wildcard CORS header, any web page open in the user's browser — could
// register a session, poll another session's queued commands, and POST a
// forged result that the model then read as the Editor's answer.
//
// Now:
//   * a per-launch bridge secret is generated when the gateway loads this
//     extension and written to ~/.openclaw/unity-bridge.token with mode 0600.
//     The Unity add-on reads that file (or OPENCLAW_BRIDGE_TOKEN) and sends it
//     as X-OpenClaw-Bridge-Token on /unity/register. No token, no session: 401.
//   * /unity/register answers with a per-session token. /unity/poll,
//     /unity/heartbeat and /unity/result require it in X-OpenClaw-Session and
//     it must belong to the session id in the request: 401 otherwise.
//   * every queued command carries a nonce. A result is accepted only for a
//     tool call that is actually in flight for that session and only with that
//     call's nonce; anything else is dropped with 409.
//   * requests are served on the loopback interface only, and any request that
//     carries an Origin or Referer header (i.e. comes from a browser) is
//     refused. No CORS headers are sent.
//
// Neither the bridge secret nor a session token is ever logged.

export const BRIDGE_TOKEN_HEADER = "x-openclaw-bridge-token";
export const SESSION_TOKEN_HEADER = "x-openclaw-session";
export const BRIDGE_TOKEN_FILENAME = `${ENGINE_KEY}-bridge.token`;

/** Environment variables that re-enable the pre-1.8.0 unauthenticated bridge. */
const ALLOW_LEGACY_ENV_VARS = [
  "OPENCLAW_EDITOR_ALLOW_LEGACY_UNAUTHENTICATED",
  "OPENCLAW_UNITY_ALLOW_LEGACY_UNAUTHENTICATED",
];

/** True when the operator opted back into the unauthenticated bridge. */
export function legacyUnauthenticatedEnabled(
  env: Record<string, string | undefined> = currentEnv()
): boolean {
  return ALLOW_LEGACY_ENV_VARS.some((name) => isTruthyFlag(env[name]));
}

function randomToken(bytes = 32): string {
  return randomBytes(bytes).toString("hex");
}

/** Constant-time string comparison that tolerates missing/short candidates. */
export function tokensMatch(candidate: unknown, expected: string): boolean {
  if (typeof candidate !== "string" || candidate.length === 0) return false;
  const a = Buffer.from(candidate, "utf8");
  const b = Buffer.from(expected, "utf8");
  if (a.length !== b.length) {
    // Compare something of equal length anyway so the failure path costs the
    // same as the success path.
    timingSafeEqual(b, b);
    return false;
  }
  return timingSafeEqual(a, b);
}

/** True for 127.0.0.0/8, ::1 and IPv4-mapped loopback. */
export function isLoopbackAddress(address: unknown): boolean {
  if (typeof address !== "string" || !address) return false;
  let value = address.trim().toLowerCase();
  const zone = value.indexOf("%");
  if (zone >= 0) value = value.slice(0, zone);
  if (value.startsWith("::ffff:")) value = value.slice(7);
  if (value === "::1" || value === "0:0:0:0:0:0:0:1") return true;
  return /^127\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(value);
}

function configDirectory(env: Record<string, string | undefined>): string {
  const explicit = env.OPENCLAW_CONFIG_DIR || env.OPENCLAW_HOME;
  if (explicit) return explicit;
  return join(env.HOME || homedir(), ".openclaw");
}

interface BridgeLogger {
  info(message: string): void;
  warn(message: string): void;
  error(message: string): void;
}

const consoleLogger: BridgeLogger = {
  info: (message) => console.log(message),
  warn: (message) => console.warn(message),
  error: (message) => console.error(message),
};

interface PendingCommand {
  toolCallId: string;
  tool: string;
  arguments: Record<string, any>;
  createdAt: number;
  nonce: string;
}

interface InflightCall {
  nonce: string;
  tool: string;
  createdAt: number;
}

export interface UnitySession {
  sessionId: string;
  /** Per-session bearer token. Never logged, never reported by /unity/status. */
  sessionToken: string;
  registeredAt: number;
  lastHeartbeat: number;
  projectName: string;
  unityVersion: string;
  platform: string;
  toolCount: number;
  pendingCommands: PendingCommand[];
  /** Tool calls handed to this session and still waiting for their result. */
  inflight: Map<string, InflightCall>;
  results: Map<string, any>;
}

export interface BridgeOptions {
  env?: Record<string, string | undefined>;
  /** Inject a secret (tests, or an operator-provided one). Skips the file write. */
  secret?: string;
  /** Write the generated secret to the config dir. Default: true. */
  persist?: boolean;
  logger?: BridgeLogger;
}

const STALE_SESSION_MS = 120000;

/**
 * One authenticated bridge: the /unity/* HTTP endpoints plus the sessions they
 * serve. The gateway extension creates one; tests create their own.
 */
export function createBridge(options: BridgeOptions = {}) {
  const env = options.env ?? currentEnv();
  const logger = options.logger ?? consoleLogger;
  const sessions = new Map<string, UnitySession>();

  const provided =
    typeof options.secret === "string" && options.secret.length > 0
      ? options.secret
      : typeof env.OPENCLAW_BRIDGE_TOKEN === "string" && env.OPENCLAW_BRIDGE_TOKEN
        ? env.OPENCLAW_BRIDGE_TOKEN
        : null;
  const secret = provided ?? randomToken();
  const shouldPersist = options.persist ?? (provided === null);

  let tokenFile: string | null = null;
  if (shouldPersist) {
    try {
      const dir = configDirectory(env);
      mkdirSync(dir, { recursive: true, mode: 0o700 });
      const file = join(dir, BRIDGE_TOKEN_FILENAME);
      writeFileSync(file, `${secret}\n`, { mode: 0o600 });
      chmodSync(file, 0o600);
      tokenFile = file;
    } catch (err: any) {
      logger.error(
        `[${ENGINE_LABEL}] Could not write the bridge token file: ${err?.message}. ` +
          `The Editor add-on will not be able to connect until it can read the token.`
      );
    }
  }

  let legacyWarnedAt = 0;
  function warnLegacyOnce() {
    const now = Date.now();
    if (now - legacyWarnedAt < 60000) return;
    legacyWarnedAt = now;
    logger.warn(
      `[${ENGINE_LABEL}] SECURITY: the bridge is running in legacy unauthenticated mode ` +
        `(${ALLOW_LEGACY_ENV_VARS[1]}). Any process on this machine can drive the Editor ` +
        `and forge tool results. Unset it and update the Editor add-on.`
    );
  }

  function generateSessionId(): string {
    return `${ENGINE_KEY}_${randomToken(12)}`;
  }

  function cleanupStaleSessions(now = Date.now()) {
    for (const [id, session] of sessions) {
      if (now - session.lastHeartbeat > STALE_SESSION_MS) {
        sessions.delete(id);
      }
    }
  }

  /** Queue one tool call for a session and record it as in flight. */
  function queueCommand(
    session: UnitySession,
    tool: string,
    parameters: Record<string, any> | undefined
  ): PendingCommand {
    const command: PendingCommand = {
      toolCallId: `${ENGINE_KEY}_req_${randomToken(12)}`,
      tool,
      arguments: parameters || {},
      createdAt: Date.now(),
      nonce: randomToken(16),
    };
    session.pendingCommands.push(command);
    session.inflight.set(command.toolCallId, {
      nonce: command.nonce,
      tool,
      createdAt: command.createdAt,
    });
    return command;
  }

  function forgetCommand(session: UnitySession, toolCallId: string) {
    session.inflight.delete(toolCallId);
    session.results.delete(toolCallId);
    const index = session.pendingCommands.findIndex(
      (command) => command.toolCallId === toolCallId
    );
    if (index >= 0) session.pendingCommands.splice(index, 1);
  }

  /**
   * Pick the session a tool call should drive. One connected Editor is used
   * implicitly; several require an explicit sessionId so a call meant for one
   * project can never land in another.
   */
  function resolveSession(sessionId?: unknown): {
    session?: UnitySession;
    error?: string;
  } {
    if (typeof sessionId === "string" && sessionId) {
      const session = sessions.get(sessionId);
      if (!session) {
        return {
          error:
            `No ${ENGINE_LABEL} session with id ${sessionId}. Call ${ENGINE_KEY}_sessions to ` +
            `list the connected Editors.`,
        };
      }
      return { session };
    }

    if (sessions.size === 0) {
      return {
        error:
          `No ${ENGINE_LABEL} session connected. Make sure ${ENGINE_LABEL} Editor is running with ` +
          `the OpenClaw add-on enabled and that it can read the bridge token.`,
      };
    }

    if (sessions.size > 1) {
      const ids = Array.from(sessions.values())
        .map((s) => `${s.sessionId} (${s.projectName})`)
        .join(", ");
      return {
        error:
          `${sessions.size} ${ENGINE_LABEL} Editors are connected, so this call needs an explicit ` +
          `sessionId — one session must not drive another's Editor. Connected: ${ids}.`,
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
      pendingCommands: s.pendingCommands.length,
    }));
  }

  function header(req: IncomingMessage, name: string): string | undefined {
    const value = (req.headers as any)?.[name];
    if (Array.isArray(value)) return value[0];
    return typeof value === "string" ? value : undefined;
  }

  /** The bridge secret, presented by the Editor add-on on /register. */
  function authenticateBridge(
    req: IncomingMessage,
    res: ServerResponse
  ): boolean {
    if (tokensMatch(header(req, BRIDGE_TOKEN_HEADER), secret)) return true;
    if (legacyUnauthenticatedEnabled(env)) {
      warnLegacyOnce();
      return true;
    }
    sendJson(res, 401, {
      error:
        `Missing or invalid ${BRIDGE_TOKEN_HEADER} header. The OpenClaw ${ENGINE_LABEL} add-on ` +
        `reads the per-launch bridge token from ${tokenFile ?? configDirectory(env) + "/" + BRIDGE_TOKEN_FILENAME}.`,
    });
    return false;
  }

  /** The per-session token issued at registration. */
  function authenticateSession(
    req: IncomingMessage,
    res: ServerResponse,
    sessionId: unknown
  ): UnitySession | null {
    const session =
      typeof sessionId === "string" ? sessions.get(sessionId) : undefined;
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
      error: `Missing or invalid ${SESSION_TOKEN_HEADER} header for this session.`,
    });
    return null;
  }

  async function handleRequest(
    req: IncomingMessage,
    res: ServerResponse
  ): Promise<boolean> {
    const url = new URL(req.url ?? "/", "http://127.0.0.1");
    const path = url.pathname;

    // Only handle /unity/* paths
    if (!path.startsWith(`/${ENGINE_KEY}/`)) {
      return false;
    }

    // The Editor add-on is a local process. Anything arriving from off-box is
    // refused even when the gateway itself listens on a public interface.
    const remote = (req.socket as any)?.remoteAddress;
    if (!isLoopbackAddress(remote)) {
      sendJson(res, 403, {
        error: `The ${ENGINE_LABEL} bridge only accepts connections from 127.0.0.1.`,
      });
      return true;
    }

    // Local MCP/editor clients never send Origin or Referer. A request that
    // does is a web page probing the bridge.
    if (header(req, "origin") || header(req, "referer")) {
      sendJson(res, 403, {
        error: "Browser-originated requests are not accepted by this bridge.",
      });
      return true;
    }

    if (req.method === "OPTIONS") {
      // No CORS: there is no legitimate browser client.
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
          const session: UnitySession = {
            sessionId,
            sessionToken: randomToken(),
            registeredAt: Date.now(),
            lastHeartbeat: Date.now(),
            projectName: typeof project === "string" ? project : "Unknown",
            unityVersion: typeof version === "string" ? version : "Unknown",
            platform: typeof platform === "string" ? platform : "UnityEditor",
            toolCount: typeof tools === "number" ? tools : 0,
            pendingCommands: [],
            inflight: new Map(),
            results: new Map(),
          };

          sessions.set(sessionId, session);
          logger.info(
            `[${ENGINE_LABEL}] Registered: ${session.projectName} (${session.unityVersion}) - Session: ${sessionId}`
          );

          sendJson(res, 200, {
            sessionId,
            sessionToken: session.sessionToken,
            sessionTokenHeader: SESSION_TOKEN_HEADER,
            status: "connected",
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

          // Return next pending command if any
          if (session.pendingCommands.length > 0) {
            const command = session.pendingCommands.shift()!;
            sendJson(res, 200, command);
          } else {
            // No content
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
          const inflight =
            typeof toolCallId === "string"
              ? session.inflight.get(toolCallId)
              : undefined;

          // A result for a call this session was never given is a forgery (or
          // a very late duplicate). Either way it must not reach the model.
          if (!inflight) {
            logger.warn(
              `[${ENGINE_LABEL}] Dropped a result for an unknown tool call on session ${session.sessionId}`
            );
            sendJson(res, 409, {
              error: "Unknown or already-completed toolCallId for this session",
            });
            return true;
          }

          const nonceOk = tokensMatch(nonce, inflight.nonce);
          if (!nonceOk && !legacyUnauthenticatedEnabled(env)) {
            logger.warn(
              `[${ENGINE_LABEL}] Dropped a result with a mismatched nonce for ${toolCallId}`
            );
            sendJson(res, 409, {
              error:
                "Result nonce does not match the nonce this tool call was issued with",
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
            destructiveOperations: destructiveOperationsEnabled(env)
              ? "enabled"
              : "blocked",
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
    cleanupStaleSessions,
  };
}

export type Bridge = ReturnType<typeof createBridge>;

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
      const raw = Buffer.concat(chunks).toString("utf8");
      if (!raw.trim()) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(raw));
      } catch (err) {
        // Older Unity plugins on comma-decimal locales (e.g. pl-PL) emit
        // "time":323,60 — repair bare-number commas and retry before failing.
        const repaired = raw.replace(/(:\s*-?\d+),(\d+\s*[,}\]])/g, "$1.$2");
        if (repaired !== raw) {
          try {
            resolve(JSON.parse(repaired));
            return;
          } catch {
            // fall through to reject with the original error
          }
        }
        reject(err);
      }
    });

    req.on("error", reject);
  });
}

// Send JSON response. No CORS headers: this bridge has no browser clients.
function sendJson(res: ServerResponse, status: number, data: any) {
  res.statusCode = status;
  res.setHeader("Content-Type", "application/json");
  res.setHeader("Cache-Control", "no-store");
  res.end(JSON.stringify(data));
}

const plugin = {
  id: "unity",
  name: "Unity Plugin",
  description: "Connect Unity Editor to OpenClaw AI assistant",

  register(api: OpenClawPluginApi) {
    const logger = api.logger;
    const bridge = createBridge({ logger: logger as any });

    if (legacyUnauthenticatedEnabled()) {
      logger.warn(
        "[Unity] SECURITY: legacy unauthenticated bridge mode is enabled " +
          "(OPENCLAW_UNITY_ALLOW_LEGACY_UNAUTHENTICATED). Any local process can drive the " +
          "Editor and forge tool results. Unset it as soon as the Editor add-on is updated."
      );
    } else if (bridge.tokenFile) {
      logger.info(`[Unity] Bridge token written to ${bridge.tokenFile} (mode 0600)`);
    }

    // Cleanup timer
    setInterval(() => bridge.cleanupStaleSessions(), 30000);

    // Register HTTP handler.
    // OpenClaw ≥2026.3 removed registerHttpHandler in favor of registerHttpRoute —
    // support both so the plugin loads on old and new gateways (issue #1).
    const httpApi = api as any;
    if (typeof httpApi.registerHttpRoute === "function") {
      httpApi.registerHttpRoute({
        path: "/unity",
        auth: "plugin",
        match: "prefix",
        handler: bridge.handleRequest,
      });
    } else {
      httpApi.registerHttpHandler(bridge.handleRequest);
    }

    // ===== Agent Tools =====

    // Tool: Execute a Unity command
    api.registerTool({
      name: "unity_execute",
      description: "Execute a tool in the connected Unity Editor. Available tools: console.getLogs, scene.getData, gameobject.find, gameobject.create, gameobject.delete, gameobject.setActive, transform.setPosition, transform.setRotation, transform.setScale, component.get, component.add, debug.hierarchy, debug.screenshot, app.getState, app.play, app.stop, input.simulateKey, input.simulateMouse, and more. Read-only built-in tools (get*/list/find/read/debug.hierarchy/debug.screenshot) run directly. Project-changing tools — and every project-registered custom tool, whatever its name reads like — are refused unless the gateway was started with OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1 and the call passes confirm: true after the user approved the change; dryRun: true previews any call without sending it.",
      parameters: {
        type: "object" as const,
        properties: {
          tool: { type: "string" as const, description: "The Unity tool to execute (e.g., 'debug.hierarchy', 'gameobject.find')" },
          parameters: { type: "object" as const, description: "Parameters for the tool (varies by tool)" },
          sessionId: { type: "string" as const, description: "Which connected Editor to drive. Required when more than one is connected." },
          confirm: {
            type: "boolean" as const,
            description:
              "Required for project-changing tools (create/delete/save/set*, package.add, script.execute, input.*, editor.play) and for every custom tool that has not been declared read-only. Set it only after the user has approved that specific change. Read-only tools ignore it.",
          },
          dryRun: {
            type: "boolean" as const,
            description:
              "Preview only: report how the call is classified and what would be sent to the Editor, without sending it.",
          },
        },
        required: ["tool"] as const,
      },
      execute: async (toolCallId: string, args: any) => {
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

        // Safety gate (see "Safety gate" above): read-only calls pass straight
        // through, project-changing calls need operator opt-in + confirm: true.
        const gate = evaluateDestructiveGate({
          tool,
          parameters,
          confirm: args?.confirm,
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
            gate: { reason: gate.reason, message: gate.message },
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
              tools: gate.tools,
            },
          });
        }

        // Pick the Editor this call drives. Never implicitly when several are
        // connected: one session must not drive another's Editor.
        const resolved = bridge.resolveSession(sessionId);
        if (!resolved.session) {
          return jsonResult({ success: false, error: resolved.error });
        }
        const session = resolved.session;

        const command = bridge.queueCommand(session, tool, parameters);
        logger.info(`[Unity] Queued command: ${tool} (request: ${command.toolCallId})`);

        // Wait for result (with timeout)
        const timeout = 60000; // 60 seconds
        const startTime = Date.now();

        while (Date.now() - startTime < timeout) {
          if (session.results.has(command.toolCallId)) {
            const result = session.results.get(command.toolCallId);
            session.results.delete(command.toolCallId);

            return jsonResult({
              success: true,
              result,
            });
          }

          await new Promise(resolve => setTimeout(resolve, 100));
        }

        bridge.forgetCommand(session, command.toolCallId);
        return jsonResult({
          success: false,
          error: "Timeout waiting for Unity response. Make sure the OpenClaw plugin is enabled in Unity Editor.",
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
        const activeSessions = bridge.listSessions();

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
          .description("Unity Plugin commands");

        unityCmd
          .command("status")
          .description("Show Unity connection status")
          .action(() => {
            console.log("\n🎮 Unity Plugin Status\n");
            console.log(
              legacyUnauthenticatedEnabled()
                ? "  Bridge auth: LEGACY UNAUTHENTICATED (any local process can drive the Editor)\n"
                : `  Bridge auth: token (${bridge.tokenFile ?? "token file unavailable"})\n`
            );
            console.log(
              destructiveOperationsEnabled()
                ? "  Project-changing tools: ENABLED (confirm: true still required per call)\n"
                : "  Project-changing tools: BLOCKED (start the gateway with OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1 to enable)\n"
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
              const age = Math.round((Date.now() - session.registeredAt) / 1000);
              const lastSeen = Math.round((Date.now() - session.lastHeartbeat) / 1000);

              console.log(`  ✅ ${session.projectName}`);
              console.log(`     Version: Unity ${session.unityVersion}`);
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

    logger.info("[Unity] Plugin loaded - authenticated HTTP endpoints at /unity/*");
  },
};

export default plugin;
