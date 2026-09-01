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
          channel_id text PRIMARY KEY, document jsonb NOT NULL, revision bigint NOT NULL DEFAULT 0, updated_at timestamptz NOT NULL
        );
        ALTER TABLE channel_configurations ADD COLUMN IF NOT EXISTS revision bigint NOT NULL DEFAULT 0;
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

    public async Task<ChannelConfiguration?> SaveConfigurationAsync(string channel, ChannelConfiguration configuration, CancellationToken token)
    {
        var updated = configuration with { SchemaVersion = 1, Revision = configuration.Revision + 1, UpdatedAt = DateTimeOffset.UtcNow };
        var json = JsonSerializer.Serialize(updated);
        await using var command = dataSource.CreateCommand("""
          INSERT INTO channel_configurations(channel_id,document,revision,updated_at)
          SELECT $1,$2::jsonb,$3,$4 WHERE $5=0
          ON CONFLICT(channel_id) DO UPDATE SET document=$2::jsonb,revision=$3,updated_at=$4
          WHERE channel_configurations.revision=$5 RETURNING revision
          """);
        command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(json); command.Parameters.AddWithValue(updated.Revision); command.Parameters.AddWithValue(updated.UpdatedAt); command.Parameters.AddWithValue(configuration.Revision);
        return await command.ExecuteScalarAsync(token) is long ? updated : null;
    }

    public async Task<ChannelConfiguration> GetConfigurationAsync(string channel, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("SELECT document::text FROM channel_configurations WHERE channel_id=$1");
        command.Parameters.AddWithValue(channel);
        var json = (string?)await command.ExecuteScalarAsync(token);
        if (json is null) return new ChannelConfiguration(1, true, [], 0, DateTimeOffset.UtcNow);
        var stored = JsonSerializer.Deserialize<ChannelConfiguration>(json)!;
        return stored.SchemaVersion == 0 ? stored with { SchemaVersion = 1, ExtensionEnabled = true } : stored;
    }

    public async Task<bool> IsActionEnabledAsync(string channel, string actionId, CancellationToken token)
    {
        var configuration = await GetConfigurationAsync(channel, token);
        var preference = configuration.Commands.FirstOrDefault(item => string.Equals(item.ActionId, actionId, StringComparison.Ordinal));
        return configuration.ExtensionEnabled && (preference?.Enabled ?? true);
    }

    public async Task<IReadOnlyList<InstallationSummary>> ListInstallationsAsync(string channel, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("SELECT installation_id,created_at,last_seen_at,revoked_at FROM installations WHERE channel_id=$1 ORDER BY created_at DESC"); command.Parameters.AddWithValue(channel);
        await using var reader = await command.ExecuteReaderAsync(token); var result = new List<InstallationSummary>();
        while (await reader.ReadAsync(token)) result.Add(new(reader.GetGuid(0), reader.GetFieldValue<DateTimeOffset>(1), reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2), reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3)));
        return result;
    }

    public async Task<bool> RevokeInstallationAsync(string channel, Guid installationId, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("UPDATE installations SET revoked_at=now() WHERE channel_id=$1 AND installation_id=$2 AND revoked_at IS NULL"); command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(installationId);
        return await command.ExecuteNonQueryAsync(token) == 1;
    }

    public async Task AuditAsync(Guid requestId, string channel, string user, string action, string status, string? detail, CancellationToken token)
    {
        await using var command = dataSource.CreateCommand("INSERT INTO action_audit(request_id,channel_id,user_id,action_id,status,detail,created_at) VALUES($1,$2,$3,$4,$5,$6,now()) ON CONFLICT(request_id) DO NOTHING");
        command.Parameters.AddWithValue(requestId); command.Parameters.AddWithValue(channel); command.Parameters.AddWithValue(user);
        command.Parameters.AddWithValue(action); command.Parameters.AddWithValue(status); command.Parameters.AddWithValue((object?)detail ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(token);
    }
}
