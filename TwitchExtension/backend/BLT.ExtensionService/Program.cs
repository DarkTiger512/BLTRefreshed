using System.Net.WebSockets;
using System.Text.Json;
using BLT.ExtensionService.Infrastructure;
using BLT.ExtensionService.Models;
using BLT.ExtensionService.Security;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
var connectionString = Environment.GetEnvironmentVariable("BLT_DATABASE") ?? builder.Configuration["BLT:Database"] ?? throw new InvalidOperationException("BLT database is not configured");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<Database>();
builder.Services.AddSingleton<ChannelStateCache>();
builder.Services.AddSingleton<ChannelRouter>();
builder.Services.AddSingleton<RequestGuard>();
builder.Services.AddSingleton<TwitchExtensionTokenValidator>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins((builder.Configuration["BLT:AllowedOrigins"] ?? "http://127.0.0.1:5173").Split(';', StringSplitOptions.RemoveEmptyEntries))
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
await app.Services.GetRequiredService<Database>().InitializeAsync(CancellationToken.None);

static IResult Unauthorized(string error) => Results.Problem(error, statusCode: StatusCodes.Status401Unauthorized);
static bool Authorized(HttpContext context, TwitchExtensionTokenValidator validator, string channel, out TwitchPrincipal? principal, out IResult? failure)
{
    if (validator.TryValidate(context.Request.Headers.Authorization, channel, out principal, out var error)) { failure = null; return true; }
    failure = Unauthorized(error); return false;
}

app.MapGet("/health", (ChannelRouter router) => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.MapGet("/api/channels/{channel}/health", (string channel, HttpContext context, TwitchExtensionTokenValidator validator, ChannelRouter router) =>
{
    if (!Authorized(context, validator, channel, out _, out var failure)) return failure!;
    return Results.Ok(new { status = "ok", channelId = channel, gameConnected = router.IsGameConnected(channel), lastStateAt = router.LastStateAt(channel) });
});

app.MapPost("/api/channels/{channel}/pairing", async (string channel, HttpContext context, TwitchExtensionTokenValidator validator, Database database, IConfiguration config, CancellationToken token) =>
{
    if (!Authorized(context, validator, channel, out var principal, out var failure)) return failure!;
    if (principal!.Role != "broadcaster") return Results.Forbid();
    var code = $"BLT-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(2))}-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(2))}";
    var expiry = DateTimeOffset.UtcNow.AddMinutes(config.GetValue("BLT:PairingLifetimeMinutes", 10));
    await database.SavePairingCodeAsync(code, channel, expiry, token);
    return Results.Ok(new PairingCodeResponse(code, expiry));
});

app.MapPost("/api/pairing/exchange", async (PairingExchangeRequest request, Database database, CancellationToken token) =>
{
    var channel = await database.ConsumePairingCodeAsync(request.Code.Trim().ToUpperInvariant(), token);
    if (channel is null) return Results.Problem("The pairing code is invalid, expired, or already used.", statusCode: 400);
    var credential = InstallationCredentialService.Create();
    var id = await database.CreateInstallationAsync(channel, InstallationCredentialService.Hash(credential), token);
    return Results.Ok(new PairingExchangeResponse(channel, id.ToString(), credential, DateTimeOffset.UtcNow));
});

app.MapGet("/api/channels/{channel}/configuration", async (string channel, HttpContext context, TwitchExtensionTokenValidator validator, Database database, CancellationToken token) =>
{
    if (!Authorized(context, validator, channel, out _, out var failure)) return failure!;
    return Results.Ok(await database.GetConfigurationAsync(channel, token));
});

app.MapPut("/api/channels/{channel}/configuration", async (string channel, ChannelConfiguration configuration, HttpContext context, TwitchExtensionTokenValidator validator, Database database, CancellationToken token) =>
{
    if (!Authorized(context, validator, channel, out var principal, out var failure)) return failure!;
    if (principal!.Role != "broadcaster") return Results.Forbid();
    await database.SaveConfigurationAsync(channel, configuration with { UpdatedAt = DateTimeOffset.UtcNow }, token);
    return Results.NoContent();
});

app.MapPost("/api/channels/{channel}/actions", async (string channel, ActionSubmission submission, HttpContext context, TwitchExtensionTokenValidator validator, Database database, ChannelRouter router, RequestGuard guard, CancellationToken token) =>
{
    if (!Authorized(context, validator, channel, out var principal, out var failure)) return failure!;
    if (!principal!.IsLinked) return Results.Problem("Share Twitch identity before triggering actions.", statusCode: 403);
    if (!Guid.TryParse(submission.RequestId, out var requestId)) return Results.Problem("requestId must be a UUID.", statusCode: 400);
    if (!submission.ActionId.StartsWith("command.", StringComparison.Ordinal)) return Results.Problem("Unknown action namespace.", statusCode: 400);
    if (!await database.IsActionEnabledAsync(channel, submission.ActionId, token)) return Results.Problem("This action is disabled by the broadcaster.", statusCode: 403);
    if (!guard.Accept(requestId, $"{channel}:{principal.UserId}:{submission.ActionId}", submission.Timestamp, out var guardError))
        return Results.Problem(guardError, statusCode: 429);
    var envelope = new
    {
        v = ProtocolKinds.Version, id = requestId, kind = "action.request", channelId = channel, timestamp = submission.Timestamp,
        user = new IntegrationUser(principal.UserId, principal.DisplayName, principal.Roles),
        data = new { actionId = submission.ActionId, args = submission.Args }
    };
    router.RegisterPrivateRequest(requestId, channel, principal.UserId);
    if (!await router.SendGameAsync(channel, envelope, token)) { router.ForgetPrivateRequest(requestId); return Results.Problem("The streamer's game is offline.", statusCode: 409); }
    await database.AuditAsync(requestId, channel, principal.UserId, submission.ActionId, "accepted", null, token);
    return Results.Accepted(value: new { requestId, status = "accepted" });
});

app.MapPost("/api/channels/{channel}/inventory", async (string channel, InventorySubmission submission, HttpContext context, TwitchExtensionTokenValidator validator, ChannelRouter router, RequestGuard guard, CancellationToken token) =>
{
    if (!Authorized(context, validator, channel, out var principal, out var failure)) return failure!;
    if (!principal!.IsLinked) return Results.Problem("Share Twitch identity before viewing your inventory.", statusCode: 403);
    if (!Guid.TryParse(submission.RequestId, out var requestId)) return Results.Problem("requestId must be a UUID.", statusCode: 400);
    if (!guard.Accept(requestId, $"{channel}:{principal.UserId}:inventory", submission.Timestamp, out var guardError)) return Results.Problem(guardError, statusCode: 429);
    var envelope = new { v = ProtocolKinds.Version, id = requestId, kind = "inventory.request", channelId = channel, timestamp = submission.Timestamp, user = new IntegrationUser(principal.UserId, principal.DisplayName, principal.Roles), data = new { } };
    router.RegisterPrivateRequest(requestId, channel, principal.UserId);
    if (!await router.SendGameAsync(channel, envelope, token)) { router.ForgetPrivateRequest(requestId); return Results.Problem("The streamer's game is offline.", statusCode: 409); }
    return Results.Accepted(value: new { requestId, status = "accepted" });
});

app.MapPost("/api/channels/{channel}/retinue", async (string channel, RetinueSubmission submission, HttpContext context, TwitchExtensionTokenValidator validator, ChannelRouter router, RequestGuard guard, CancellationToken token) =>
{
    if (!Authorized(context, validator, channel, out var principal, out var failure)) return failure!;
    if (!principal!.IsLinked) return Results.Problem("Share Twitch identity before viewing your retinue.", statusCode: 403);
    if (!Guid.TryParse(submission.RequestId, out var requestId)) return Results.Problem("requestId must be a UUID.", statusCode: 400);
    if (!guard.Accept(requestId, $"{channel}:{principal.UserId}:retinue", submission.Timestamp, out var guardError)) return Results.Problem(guardError, statusCode: 429);
    var envelope = new { v = ProtocolKinds.Version, id = requestId, kind = "retinue.request", channelId = channel, timestamp = submission.Timestamp, user = new IntegrationUser(principal.UserId, principal.DisplayName, principal.Roles), data = new { } };
    router.RegisterPrivateRequest(requestId, channel, principal.UserId);
    if (!await router.SendGameAsync(channel, envelope, token)) { router.ForgetPrivateRequest(requestId); return Results.Problem("The streamer's game is offline.", statusCode: 409); }
    return Results.Accepted(value: new { requestId, status = "accepted" });
});

app.Map("/ws/game/{channel}", async (string channel, HttpContext context, Database database, ChannelRouter router, CancellationToken token) =>
{
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    var authorization = context.Request.Headers.Authorization.ToString();
    if (!authorization.StartsWith("Bearer ") || !await database.ValidateInstallationAsync(channel, InstallationCredentialService.Hash(authorization[7..].Trim()), token))
    { context.Response.StatusCode = 401; return; }
    using var socket = await context.WebSockets.AcceptWebSocketAsync("blt.integration.v1");
    await router.AttachGameAsync(channel, socket, token);
});

app.Map("/ws/viewer/{channel}", async (string channel, HttpContext context, TwitchExtensionTokenValidator validator, ChannelRouter router, CancellationToken token) =>
{
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    var tokenValue = context.Request.Query["token"].ToString();
    if (!validator.TryValidate($"Bearer {tokenValue}", channel, out var principal, out _)) { context.Response.StatusCode = 401; return; }
    using var socket = await context.WebSockets.AcceptWebSocketAsync("blt.viewer.v1");
    await router.AttachViewerAsync(channel, principal!.UserId, socket, token);
});

app.Run();

public partial class Program;
