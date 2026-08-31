using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BLT.ExtensionService.Models;

namespace BLT.ExtensionService.Security;

public sealed class TwitchExtensionTokenValidator(IHostEnvironment environment, ILogger<TwitchExtensionTokenValidator> logger)
{
    public bool TryValidate(string? authorization, string expectedChannel, out TwitchPrincipal? principal, out string error)
    {
        principal = null;
        error = "Invalid Twitch authorization.";
        if (environment.IsDevelopment() && Environment.GetEnvironmentVariable("BLT_ALLOW_DEVELOPMENT_AUTH") == "true" && authorization == "Bearer development-token")
        {
            principal = new TwitchPrincipal(expectedChannel, "development-user", "Udevelopment-user", "broadcaster", true, "Rowan");
            return true;
        }
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
        var token = authorization[7..].Trim();
        var parts = token.Split('.');
        if (parts.Length != 3) return false;
        var secretText = Environment.GetEnvironmentVariable("TWITCH_EXTENSION_SECRET");
        if (string.IsNullOrWhiteSpace(secretText))
        {
            logger.LogError("TWITCH_EXTENSION_SECRET is not configured");
            error = "Extension authentication is unavailable.";
            return false;
        }
        try
        {
            var secret = Convert.FromBase64String(secretText);
            using var hmac = new HMACSHA256(secret);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, Decode(parts[2]))) return false;
            using var payload = JsonDocument.Parse(Decode(parts[1]));
            var root = payload.RootElement;
            var exp = root.GetProperty("exp").GetInt64();
            if (DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow) { error = "Twitch authorization expired."; return false; }
            var channel = root.GetProperty("channel_id").GetString() ?? "";
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(channel), Encoding.UTF8.GetBytes(expectedChannel))) { error = "Channel mismatch."; return false; }
            var opaque = root.TryGetProperty("opaque_user_id", out var opaqueElement) ? opaqueElement.GetString() ?? "" : "";
            var userId = root.TryGetProperty("user_id", out var userElement) ? userElement.GetString() ?? "" : "";
            var role = root.TryGetProperty("role", out var roleElement) ? roleElement.GetString() ?? "viewer" : "viewer";
            principal = new TwitchPrincipal(channel, userId, opaque, role, !string.IsNullOrWhiteSpace(userId), userId.Length > 0 ? userId : opaque);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or KeyNotFoundException)
        {
            logger.LogWarning("Rejected malformed Twitch JWT: {Message}", ex.Message);
            return false;
        }
    }

    private static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
