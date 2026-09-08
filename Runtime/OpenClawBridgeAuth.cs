/*
 * OpenClaw Unity Plugin - Bridge authentication
 * Per-launch shared secrets for the two local bridges.
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Shared secrets for the two local bridges.
    ///
    /// Gateway bridge (this add-on is the client): the OpenClaw gateway writes a
    /// per-launch token to &lt;config&gt;/unity-bridge.token when it loads the Unity
    /// extension. We read it and send it as X-OpenClaw-Bridge-Token on
    /// /unity/register; the gateway answers with a per-session token that every
    /// later request must carry.
    ///
    /// MCP bridge (this add-on is the server): we generate a per-launch token
    /// when the bridge starts and write it to &lt;config&gt;/unity-mcp-bridge.token
    /// with mode 0600. MCP~/index.js reads that file and sends it as
    /// X-OpenClaw-Token.
    ///
    /// &lt;config&gt; is $OPENCLAW_CONFIG_DIR, else $OPENCLAW_HOME, else ~/.openclaw.
    /// A token is never written to the Unity console or to any log.
    /// </summary>
    public static class OpenClawBridgeAuth
    {
        public const string BridgeTokenHeader = "X-OpenClaw-Bridge-Token";
        public const string SessionTokenHeader = "X-OpenClaw-Session";
        public const string McpTokenHeader = "X-OpenClaw-Token";

        public const string GatewayTokenFileName = "unity-bridge.token";
        public const string McpTokenFileName = "unity-mcp-bridge.token";

        /// <summary>Environment variable that overrides both token files.</summary>
        public const string TokenEnvVar = "OPENCLAW_BRIDGE_TOKEN";

        /// <summary>
        /// Environment variable that re-enables the pre-1.7.0 unauthenticated
        /// MCP bridge. Off by default; the bridge shouts when it is on.
        /// </summary>
        public const string AllowLegacyEnvVar = "OPENCLAW_UNITY_ALLOW_LEGACY_UNAUTHENTICATED";

        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int SysChmod(string path, uint mode);

        /// <summary>The OpenClaw config directory, without creating it.</summary>
        public static string ConfigDirectory()
        {
            var explicitDir = Environment.GetEnvironmentVariable("OPENCLAW_CONFIG_DIR");
            if (string.IsNullOrEmpty(explicitDir))
            {
                explicitDir = Environment.GetEnvironmentVariable("OPENCLAW_HOME");
            }
            if (!string.IsNullOrEmpty(explicitDir))
            {
                return explicitDir;
            }

            var home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
            {
                home = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            if (string.IsNullOrEmpty(home))
            {
                home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            return Path.Combine(home ?? ".", ".openclaw");
        }

        public static string GatewayTokenPath()
        {
            return Path.Combine(ConfigDirectory(), GatewayTokenFileName);
        }

        public static string McpTokenPath()
        {
            return Path.Combine(ConfigDirectory(), McpTokenFileName);
        }

        /// <summary>True when the operator opted back into the old open bridge.</summary>
        public static bool LegacyUnauthenticatedEnabled()
        {
            var value = Environment.GetEnvironmentVariable(AllowLegacyEnvVar);
            if (string.IsNullOrEmpty(value)) return false;
            value = value.Trim().ToLowerInvariant();
            return value == "1" || value == "true" || value == "yes" || value == "on";
        }

        /// <summary>
        /// The token this add-on presents to the gateway: the environment
        /// override, else the config asset's apiToken, else the file the gateway
        /// wrote. Returns null when there is none — the gateway will answer 401
        /// and the panel explains why.
        /// </summary>
        public static string ReadGatewayToken(OpenClawConfig config)
        {
            var fromEnv = Environment.GetEnvironmentVariable(TokenEnvVar);
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv.Trim();

            if (config != null && !string.IsNullOrEmpty(config.apiToken))
            {
                return config.apiToken.Trim();
            }

            return ReadTokenFile(GatewayTokenPath());
        }

        /// <summary>Read a token file, or null when it is missing/unreadable.</summary>
        public static string ReadTokenFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var text = File.ReadAllText(path).Trim();
                return string.IsNullOrEmpty(text) ? null : text;
            }
            catch (Exception e)
            {
                // The path is safe to log. The token is not.
                Debug.LogWarning($"[OpenClaw] Could not read {path}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// The token the MCP bridge requires: the environment override, else a
        /// fresh per-launch token written to the config dir with mode 0600.
        /// </summary>
        public static string EnsureMcpToken()
        {
            var fromEnv = Environment.GetEnvironmentVariable(TokenEnvVar);
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv.Trim();

            var token = NewToken();
            WriteTokenFile(McpTokenPath(), token);
            return token;
        }

        /// <summary>32 random bytes, hex encoded.</summary>
        public static string NewToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var text = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) text.Append(b.ToString("x2"));
            return text.ToString();
        }

        /// <summary>
        /// Write a token to a file only its owner can read. The path is logged;
        /// the token never is.
        /// </summary>
        public static void WriteTokenFile(string path, string token)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, token + "\n");
                RestrictToOwner(path);
                Debug.Log($"[OpenClaw] Bridge token written to {path} (owner-only)");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[OpenClaw] Could not write the bridge token to {path}: {e.Message}. " +
                    "MCP clients will not be able to authenticate.");
            }
        }

        /// <summary>chmod 600 on Unix. On Windows the user profile ACL applies.</summary>
        private static void RestrictToOwner(string path)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // No chmod on Windows; the file sits under the user profile.
            return;
#else
            try
            {
                // 0600
                if (SysChmod(path, 0x180) != 0)
                {
                    Debug.LogWarning(
                        $"[OpenClaw] chmod 600 failed for {path}; check its permissions by hand.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[OpenClaw] Could not restrict {path} to the owner ({e.Message}); " +
                    "check its permissions by hand.");
            }
#endif
        }

        /// <summary>
        /// Length-constant comparison, so a wrong token cannot be found one
        /// character at a time.
        /// </summary>
        public static bool TokensMatch(string candidate, string expected)
        {
            if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(expected)) return false;
            if (candidate.Length != expected.Length) return false;

            var difference = 0;
            for (var i = 0; i < candidate.Length; i++)
            {
                difference |= candidate[i] ^ expected[i];
            }
            return difference == 0;
        }
    }
}
