using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BLT.ExtensionService.Security;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace BLT.ExtensionService.Tests;

[CollectionDefinition("Environment", DisableParallelization = true)]
public sealed class EnvironmentCollection;

[Collection("Environment")]
public sealed class TwitchTokenValidatorTests
{
    [Fact]
    public void ValidatesSignatureExpiryChannelIdentityAndRole()
    {
        var secret = RandomNumberGenerator.GetBytes(32);
        Environment.SetEnvironmentVariable("TWITCH_EXTENSION_SECRET", Convert.ToBase64String(secret));
        var validator = new TwitchExtensionTokenValidator(new TestEnvironment(), NullLogger<TwitchExtensionTokenValidator>.Instance);
        var token = CreateToken(secret, "123", "viewer", "viewer-7");
        Assert.True(validator.TryValidate($"Bearer {token}", "123", out var principal, out _));
        Assert.Equal("viewer-7", principal!.UserId);
        Assert.True(principal.IsLinked);
        Assert.False(validator.TryValidate($"Bearer {token}", "different", out _, out var error));
        Assert.Equal("Channel mismatch.", error);
    }

    private static string CreateToken(byte[] secret, string channel, string role, string user)
    {
        var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var body = Encode(JsonSerializer.SerializeToUtf8Bytes(new { channel_id = channel, role, user_id = user, opaque_user_id = $"U{user}", exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() }));
        using var hmac = new HMACSHA256(secret);
        return $"{header}.{body}.{Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{header}.{body}")))}";
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
