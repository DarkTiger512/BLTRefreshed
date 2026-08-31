using System.Text.Json;
using BLT.ExtensionService.Models;
using Npgsql;

namespace BLT.ExtensionService.Infrastructure;

public sealed class Database(NpgsqlDataSource dataSource)
{
    public async Task InitializeAsync(CancellationToken token)
    {
        const string sql = """
        CREATE TABLE IF NOT EXISTS pairing_codes (
          code text PRIMARY KEY, channel_id text NOT NULL, expires_at timestamptz NOT NULL, consumed_at timestamptz NULL
        );
        CREATE TABLE IF NOT EXISTS installations (
          installation_id uuid PRIMARY KEY, channel_id text NOT NULL, credential_hash text NOT NULL UNIQUE,
          created_at timestamptz NOT NULL, revoked_at timestamptz NULL, last_seen_at timestamptz NULL
        );
        CREATE INDEX IF NOT EXISTS installations_channel_idx ON installations(channel_id);
        CREATE TABLE IF NOT EXISTS channel_configurations (
          channel_id text PRIMARY KEY, document jsonb NOT NULL, updated_at timestamptz NOT NULL
        );
        CREATE TABLE IF NOT EXISTS action_audit (
          audit_id bigserial PRIMARY KEY, request_id uuid NOT NULL, channel_id text NOT NULL, user_id text NOT NULL,
          action_id text NOT NULL, status text NOT NULL, detail text NULL, created_at timestamptz NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS action_audit_request_idx ON action_audit(request_id);
        """;
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task SavePairingCodeAsync(string code, string channel, DateTimeOffset expiresAt, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("INSERT INTO pairing_codes(code,channel_id,expires_at) VALUES($1,$2,$3) ON CONFLICT(code) DO UPDATE SET channel_id=$2,expires_at=$3,consumed_at=NULL");
        command.Parameters.AddWithValue(code); command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(expiresAt);
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task<string?> ConsumePairingCodeAsync(string code, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("UPDATE pairing_codes SET consumed_at=now() WHERE code=$1 AND consumed_at IS NULL AND expires_at>now() RETURNING channel_id");
        command.Parameters.AddWithValue(code);
        return (string?)await command.ExecuteScalarAsync(token);
    }

    public async Task<Guid> CreateInstallationAsync(string channel, string credentialHash, CancellationToken token)
    {
        var id = Guid.NewGuid();
        await using var command = dataSource.CreateCommand("INSERT INTO installations(installation_id,channel_id,credential_hash,created_at) VALUES($1,$2,$3,now())");
        command.Parameters.AddWithValue(id); command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(credentialHash);
        await command.ExecuteNonQueryAsync(token);
        return id;
    }

    public async Task<bool> ValidateInstallationAsync(string channel, string credentialHash, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("UPDATE installations SET last_seen_at=now() WHERE channel_id=$1 AND credential_hash=$2 AND revoked_at IS NULL RETURNING installation_id");
        command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(credentialHash);
        return await command.ExecuteScalarAsync(token) is Guid;
    }

    public async Task SaveConfigurationAsync(string channel, ChannelConfiguration configuration, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(configuration);
        await using var command = dataSource.CreateCommand("INSERT INTO channel_configurations(channel_id,document,updated_at) VALUES($1,$2::jsonb,now()) ON CONFLICT(channel_id) DO UPDATE SET document=$2::jsonb,updated_at=now()");
        command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(json);
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task<ChannelConfiguration> GetConfigurationAsync(string channel, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("SELECT document::text FROM channel_configurations WHERE channel_id=$1");
        command.Parameters.AddWithValue(channel);
        var json = (string?)await command.ExecuteScalarAsync(token);
        return json is null ? new ChannelConfiguration([], DateTimeOffset.UtcNow) : JsonSerializer.Deserialize<ChannelConfiguration>(json)!;
    }

    public async Task AuditAsync(Guid requestId, string channel, string user, string action, string status, string? detail, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("INSERT INTO action_audit(request_id,channel_id,user_id,action_id,status,detail,created_at) VALUES($1,$2,$3,$4,$5,$6,now()) ON CONFLICT(request_id) DO NOTHING");
        command.Parameters.AddWithValue(requestId); command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(user);
        command.Parameters.AddWithValue(action); command.Parameters.AddWithValue(status); command.Parameters.AddWithValue((object?)detail ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(token);
    }
}
