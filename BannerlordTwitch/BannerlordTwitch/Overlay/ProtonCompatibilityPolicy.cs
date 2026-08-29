using System;

namespace BannerlordTwitch
{
    public static class ProtonCompatibilityPolicy
    {
        public static bool IsValidPort(int port) => port >= 1024 && port <= 65525;
        public static bool PortsAreSeparated(int overlayPort, int configurationPort) => Math.Abs(overlayPort - configurationPort) > 10;
        public static string PreserveSecret(string existing, string submitted) =>
            string.IsNullOrWhiteSpace(submitted) ? existing : submitted;
        public static bool IsAllowedOrigin(string origin, int port) =>
            string.IsNullOrEmpty(origin) || string.Equals(origin, $"http://127.0.0.1:{port}", StringComparison.OrdinalIgnoreCase);
    }
}
