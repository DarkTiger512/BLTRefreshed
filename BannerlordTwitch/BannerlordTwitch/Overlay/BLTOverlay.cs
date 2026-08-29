using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using BannerlordTwitch.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Logging;
using Microsoft.Owin.StaticFiles;
using Owin;
using TaleWorlds.Library;

namespace BLTOverlay
{
    public static class BLTOverlay
    {
        private static string WebRoot => Path.Combine(
            Path.GetDirectoryName(typeof(BLTOverlay).Assembly.Location) ?? ".",
            "..", "..", "web");

        private const int DefaultPort = 8087;
        public static int Port { get; private set; }
        private static IDisposable host;

        public static string UrlRoot => $"http://127.0.0.1:{Port}";
        private static string UrlBinding => $"http://127.0.0.1:{Port}/";

        private const string JSExtension =
#if DEBUG
                "js"
#else
                "min.js"
#endif
            ;

        public static void Start()
        {
            if (host != null) return;
            Port = FindAvailablePort(BannerlordTwitch.ProtonHostSettings.Load().OverlayPort);
            string indexTemplate = File.ReadAllText(Path.Combine(WebRoot, "index-template.html"));

            overlayProviders.Sort((l, r) => l.order.CompareTo(r.order));

            indexTemplate = indexTemplate.Replace("$custom_styles$",
                string.Join("\n", overlayProviders
                    .Where(o => !string.IsNullOrWhiteSpace(o.css))
                    .Select(o => $"<style type=\"text/css\">\n{o.css}\n</style>")));
            indexTemplate = indexTemplate.Replace("$custom_body$",
                string.Join("\n", overlayProviders
                    .Where(o => !string.IsNullOrWhiteSpace(o.body))
                    .Select(o => o.body)));
            indexTemplate = indexTemplate.Replace("$custom_scripts$",
                string.Join("\n", overlayProviders
                    .Where(o => !string.IsNullOrWhiteSpace(o.script))
                    .Select(o => $"<script type=\"text/javascript\">\n{o.script}\n</script>")));

            indexTemplate = indexTemplate.Replace("$url_root$", UrlRoot);
            indexTemplate = indexTemplate.Replace("$min_js$", JSExtension);
            indexTemplate = indexTemplate.Replace("$version$", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

            File.WriteAllText(Path.Combine(WebRoot, "index.html"), indexTemplate);

            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromDays(1);
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromDays(1);

            host = WebApp.Start(UrlBinding, app =>
            {
                app.SetLoggerFactory(new LoggerFactory());
                app.UseCors(CorsOptions.AllowAll);
                app.MapSignalR();
                var physicalFileSystem = new PhysicalFileSystemEx(WebRoot);
                var options = new FileServerOptions
                {
                    EnableDefaultFiles = true,
                    FileSystem = physicalFileSystem,
                    StaticFileOptions = { FileSystem = physicalFileSystem, ServeUnknownFileTypes = true },
                    DefaultFilesOptions = { DefaultFileNames = new[] { "index.html" } }
                };
                app.UseStaticFiles();
                app.UseFileServer(options);
            });

            Log.Info($"BLT overlay listening on {UrlRoot}");
            InformationManager.DisplayMessage(new InformationMessage($"BLT overlay: {UrlRoot}"));
        }

        public static void Stop()
        {
            host?.Dispose();
            host = null;
        }

        private static int FindAvailablePort(int first)
        {
            for (var port = first; port <= first + 10; port++)
            {
                try
                {
                    var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch (System.Net.Sockets.SocketException) { }
            }
            throw new InvalidOperationException($"No available BLT overlay port in range {first}-{first + 10}.");
        }

        private class OverlayProvider
        {
            public string id;
            public int order;
            public string css;
            public string body;
            public string script;
        }

        private static readonly List<OverlayProvider> overlayProviders = new();

        public static void Register(string id, int order, string css, string body, string script)
        {
            overlayProviders.Add(new()
            {
                id = id,
                order = order,
                css = css,
                body = body,
                script = script,
            });
        }
    }
}
