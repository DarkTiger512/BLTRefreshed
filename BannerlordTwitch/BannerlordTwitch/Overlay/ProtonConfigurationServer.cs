using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using BannerlordTwitch.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;

namespace BannerlordTwitch
{
    internal sealed class ProtonHostSettings
    {
        public int OverlayPort { get; set; } = 8087;
        public int ConfigurationPort { get; set; } = 8088;

        public static ProtonHostSettings Load()
        {
            try
            {
                var path = FileSystem.GetConfigPath("Bannerlord-Twitch-Proton.yaml");
                return FileSystem.FileExists(path)
                    ? YamlHelpers.Deserialize<ProtonHostSettings>(FileSystem.GetFileContentString(path)) ?? new ProtonHostSettings()
                    : new ProtonHostSettings();
            }
            catch { return new ProtonHostSettings(); }
        }

        public static void Save(ProtonHostSettings value) =>
            FileSystem.SaveFileString(FileSystem.GetConfigPath("Bannerlord-Twitch-Proton.yaml"), YamlHelpers.Serialize(value));
    }

    /// <summary>Loopback-only configuration surface for Steam Proton installations.</summary>
    internal static class ProtonConfigurationServer
    {
        private const int DefaultPort = 8088;
        private static readonly string SessionToken = Guid.NewGuid().ToString("N");
        private static HttpListener listener;
        private static Thread worker;

        public static int Port { get; private set; }
        public static string Url => $"http://127.0.0.1:{Port}/?token={SessionToken}";

        public static void Start()
        {
            if (listener != null) return;
            Port = FindAvailablePort(ProtonHostSettings.Load().ConfigurationPort);
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Start();
            worker = new Thread(Run) { IsBackground = true, Name = "BLT Proton configuration" };
            worker.Start();
            Log.Info($"BLT browser configuration listening on {Url}");
            InformationManager.DisplayMessage(new InformationMessage($"BLT configuration: {Url}"));
        }

        public static void Stop()
        {
            try { listener?.Close(); } catch { }
            listener = null;
            worker = null;
        }

        private static void Run()
        {
            while (listener?.IsListening == true)
            {
                HttpListenerContext context = null;
                try { context = listener.GetContext(); Handle(context); }
                catch (HttpListenerException) when (listener?.IsListening != true) { }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Log.Exception("BLT configuration request failed", ex, noRethrow: true);
                    if (context != null)
                    {
                        try { WriteJson(context, 400, new { error = ex.Message }); } catch { }
                    }
                }
            }
        }

        private static void Handle(HttpListenerContext context)
        {
            if (!IPAddress.IsLoopback(context.Request.RemoteEndPoint?.Address ?? IPAddress.None))
            {
                Write(context, 403, "text/plain", "Loopback access only.");
                return;
            }

            string path = context.Request.Url.AbsolutePath.TrimEnd('/');
            if (path.Length == 0)
            {
                Write(context, 200, "text/html; charset=utf-8", Page());
                return;
            }
            if (path == "/health")
            {
                WriteJson(context, 200, new { ok = true, configurationPort = Port, overlayPort = BLTOverlay.BLTOverlay.Port });
                return;
            }
            if (!Authorized(context))
            {
                Write(context, 403, "text/plain", "Invalid BLT session token or origin.");
                return;
            }

            if (path == "/api/v1/status" && context.Request.HttpMethod == "GET")
            {
                var auth = AuthSettings.Load();
                WriteJson(context, 200, new
                {
                    activeProfile = Settings.ActiveProfile,
                    settingsValid = TryLoadSettings(out _),
                    twitchAuthorized = !string.IsNullOrWhiteSpace(auth?.AccessToken) && !string.IsNullOrWhiteSpace(auth?.ClientID),
                    twitchConnected = BLTModule.TwitchService != null,
                    overlayUrl = BLTOverlay.BLTOverlay.UrlRoot,
                    configurationPort = Port
                });
                return;
            }
            if (path == "/api/v1/settings" && context.Request.HttpMethod == "GET")
            {
                Write(context, 200, "text/yaml; charset=utf-8", ReadActiveSettings());
                return;
            }
            if (path == "/api/v1/host" && context.Request.HttpMethod == "GET")
            {
                WriteJson(context, 200, ProtonHostSettings.Load());
                return;
            }
            if (path == "/api/v1/host" && context.Request.HttpMethod == "POST")
            {
                var host = JsonConvert.DeserializeObject<ProtonHostSettings>(ReadBody(context)) ?? throw new InvalidDataException("Host settings are empty.");
                if (!ProtonCompatibilityPolicy.IsValidPort(host.OverlayPort) || !ProtonCompatibilityPolicy.IsValidPort(host.ConfigurationPort))
                    throw new InvalidDataException("Ports must be between 1024 and 65525.");
                if (!ProtonCompatibilityPolicy.PortsAreSeparated(host.OverlayPort, host.ConfigurationPort))
                    throw new InvalidDataException("Preferred overlay and configuration ports must be more than ten ports apart.");
                ProtonHostSettings.Save(host);
                WriteJson(context, 200, new { saved = true, restartRequired = true });
                return;
            }
            if (path == "/api/v1/settings" && context.Request.HttpMethod == "POST")
            {
                SaveSettings(ReadBody(context));
                WriteJson(context, 200, new { saved = true, restartRequired = false });
                return;
            }
            if (path == "/api/v1/profile" && context.Request.HttpMethod == "POST")
            {
                int profile = int.Parse(ReadBody(context));
                if (profile < 1 || profile > 99) throw new InvalidDataException("Profile must be between 1 and 99.");
                Settings.ChangeProfile(profile);
                WriteJson(context, 200, new { activeProfile = profile, restartRequired = true });
                return;
            }
            if (path == "/api/v1/auth" && context.Request.HttpMethod == "GET")
            {
                var auth = AuthSettings.Load() ?? new AuthSettings();
                WriteJson(context, 200, new
                {
                    auth.ClientID,
                    auth.BotMessagePrefix,
                    auth.ExtensionClientId,
                    hasAccessToken = !string.IsNullOrWhiteSpace(auth.AccessToken),
                    hasBotAccessToken = !string.IsNullOrWhiteSpace(auth.BotAccessToken),
                    hasExtensionSecret = !string.IsNullOrWhiteSpace(auth.ExtensionSecret)
                });
                return;
            }
            if (path == "/api/v1/auth" && context.Request.HttpMethod == "POST")
            {
                SaveAuth(JObject.Parse(ReadBody(context)));
                WriteJson(context, 200, new { saved = true });
                return;
            }
            if (path == "/api/v1/restart" && context.Request.HttpMethod == "POST")
            {
                MainThreadSync.Run(() => BLTModule.RestartTwitchService());
                WriteJson(context, 202, new { restartQueued = true });
                return;
            }

            Write(context, 404, "text/plain", "Not found.");
        }

        private static bool Authorized(HttpListenerContext context)
        {
            string supplied = context.Request.Headers["X-BLT-Token"] ?? context.Request.QueryString["token"];
            if (!string.Equals(supplied, SessionToken, StringComparison.Ordinal)) return false;
            string origin = context.Request.Headers["Origin"];
            return ProtonCompatibilityPolicy.IsAllowedOrigin(origin, Port);
        }

        private static bool TryLoadSettings(out Settings settings)
        {
            try { settings = Settings.Load(); return settings != null; }
            catch { settings = null; return false; }
        }

        private static string ReadActiveSettings()
        {
            var path = FileSystem.GetConfigPath($"Bannerlord-Twitch-v4-p{Settings.ActiveProfile}.yaml");
            if (FileSystem.FileExists(path)) return FileSystem.GetFileContentString(path);
            return YamlHelpers.Serialize(Settings.Load());
        }

        private static void SaveSettings(string yaml)
        {
            var parsed = YamlHelpers.Deserialize<Settings>(yaml) ?? throw new InvalidDataException("Settings YAML is empty.");
            var active = FileSystem.GetConfigPath($"Bannerlord-Twitch-v4-p{Settings.ActiveProfile}.yaml");
            if (FileSystem.FileExists(active))
            {
                string backup = $"Bannerlord-Twitch-v4-p{Settings.ActiveProfile}-{DateTime.UtcNow:yyyyMMddHHmmss}.backup.yaml";
                FileSystem.SaveFileString(FileSystem.GetConfigPath(backup), FileSystem.GetFileContentString(active));
            }
            Settings.Save(parsed);
        }

        private static void SaveAuth(JObject update)
        {
            var auth = AuthSettings.Load() ?? new AuthSettings();
            string Value(string name) => update[name]?.Value<string>();
            void Replace(string name, Action<string> setter, bool preserveBlank = false)
            {
                string value = Value(name);
                if (value != null && (!preserveBlank || !string.IsNullOrWhiteSpace(value))) setter(value);
            }
            Replace("clientID", v => auth.ClientID = v);
            Replace("botMessagePrefix", v => auth.BotMessagePrefix = v);
            Replace("extensionClientId", v => auth.ExtensionClientId = v);
            auth.AccessToken = ProtonCompatibilityPolicy.PreserveSecret(auth.AccessToken, Value("accessToken"));
            auth.BotAccessToken = ProtonCompatibilityPolicy.PreserveSecret(auth.BotAccessToken, Value("botAccessToken"));
            auth.ExtensionSecret = ProtonCompatibilityPolicy.PreserveSecret(auth.ExtensionSecret, Value("extensionSecret"));
            var old = AuthSettings.Load();
            if (old != null) FileSystem.SaveFileString(FileSystem.GetConfigPath($"Bannerlord-Twitch-Auth-{DateTime.UtcNow:yyyyMMddHHmmss}.backup.yaml"), YamlHelpers.Serialize(old));
            AuthSettings.Save(auth);
        }

        private static string ReadBody(HttpListenerContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static void WriteJson(HttpListenerContext context, int status, object value) =>
            Write(context, status, "application/json; charset=utf-8", JsonConvert.SerializeObject(value));

        private static void Write(HttpListenerContext context, int status, string contentType, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            context.Response.StatusCode = status;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        private static int FindAvailablePort(int first)
        {
            for (int port = first; port <= first + 10; port++)
            {
                try
                {
                    var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port);
                    probe.Start(); probe.Stop(); return port;
                }
                catch (System.Net.Sockets.SocketException) { }
            }
            throw new InvalidOperationException($"No available BLT configuration port in range {first}-{first + 10}.");
        }

        private static string Page() => @"<!doctype html><html><head><meta charset='utf-8'><title>BLT Proton Configuration</title><style>body{font:16px system-ui;background:#111827;color:#e5e7eb;max-width:1100px;margin:auto;padding:24px}textarea{width:100%;height:55vh;background:#030712;color:#d1fae5;border:1px solid #374151;padding:12px}button,input{padding:10px;margin:5px 0}section{margin:22px 0}.ok{color:#6ee7b7}.err{color:#fca5a5}</style></head><body><h1>BLT Proton Configuration</h1><p id='status'>Loading…</p><section><h2>Active profile YAML</h2><textarea id='yaml'></textarea><br><button onclick='saveSettings()'>Validate and save</button></section><section><h2>Twitch credentials</h2><input id='clientID' placeholder='Client ID'><br><input id='accessToken' type='password' placeholder='Access token (blank preserves existing)'><br><input id='botAccessToken' type='password' placeholder='Bot token (blank preserves existing)'><br><button onclick='saveAuth()'>Save credentials</button> <button onclick='restart()'>Restart Twitch service</button></section><pre id='result'></pre><script>const token=new URLSearchParams(location.search).get('token');const api=(p,o={})=>fetch(p,{...o,headers:{...(o.headers||{}),'X-BLT-Token':token}});async function load(){let s=await(await api('/api/v1/status')).json();status.textContent=`Profile ${s.activeProfile} · Twitch ${s.twitchConnected?'connected':s.twitchAuthorized?'authorized':'not authorized'} · Overlay ${s.overlayUrl}`;yaml.value=await(await api('/api/v1/settings')).text();let a=await(await api('/api/v1/auth')).json();clientID.value=a.ClientID||''}async function saveSettings(){show(await api('/api/v1/settings',{method:'POST',body:yaml.value}))}async function saveAuth(){show(await api('/api/v1/auth',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({clientID:clientID.value,accessToken:accessToken.value,botAccessToken:botAccessToken.value})}))}async function restart(){show(await api('/api/v1/restart',{method:'POST'}))}async function show(r){result.textContent=await r.text();result.className=r.ok?'ok':'err';if(r.ok)load()}load().catch(e=>{result.textContent=e;result.className='err'})</script></body></html>";
    }
}
