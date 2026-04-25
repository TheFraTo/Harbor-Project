using Harbor.Core.Enums;
using Harbor.Core.Models;
using Microsoft.Data.Sqlite;

namespace Harbor.Data.Repositories;

/// <summary>
/// Accès au journal d'audit local. Sémantique append-only : les entrées
/// existantes ne sont jamais modifiées. La purge périodique se fait par
/// suppression en bloc selon une date butoir (<see cref="DeleteOlderThanAsync"/>).
/// </summary>
public sealed class AuditLogRepository
{
    private readonly HarborDbContext _context;

    /// <summary>Initialise le repo avec un contexte DB ouvert.</summary>
    public AuditLogRepository(HarborDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>Ajoute une entrée au journal.</summary>
    public async Task InsertAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO audit_log (id, timestamp, type, profile_id, description, metadata_json)
            VALUES ($id, $timestamp, $type, $profileId, $description, $metadataJson)
            """;
        _ = cmd.Parameters.AddWithValue("$id", entry.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$timestamp", entry.Timestamp.ToUnixTimeSeconds());
        _ = cmd.Parameters.AddWithValue("$type", entry.Type.ToString());
        _ = cmd.Parameters.AddWithValue(
            "$profileId",
            (object?)entry.ProfileId?.ToString() ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$description", entry.Description);
        _ = cmd.Parameters.AddWithValue(
            "$metadataJson",
            (object?)entry.MetadataJson ?? DBNull.Value);

        _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Liste les entrées les plus récentes, jusqu'à <paramref name="limit"/> au total.
    /// </summary>
    public async Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        if (limit <= 0)
        {
            return [];
        }

        List<AuditLogEntry> results = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, timestamp, type, profile_id, description, metadata_json
            FROM audit_log
            ORDER BY timestamp DESC
            LIMIT $limit
            """;
        _ = cmd.Parameters.AddWithValue("$limit", limit);

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>Liste les entrées concernant un profil donné.</summary>
    public async Task<IReadOnlyList<AuditLogEntry>> GetByProfileAsync(
        Guid profileId,
        int limit = 100,
        CancellationToken ct = default)
    {
        List<AuditLogEntry> results = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, timestamp, type, profile_id, description, metadata_json
            FROM audit_log
            WHERE profile_id = $profileId
            ORDER BY timestamp DESC
            LIMIT $limit
            """;
        _ = cmd.Parameters.AddWithValue("$profileId", profileId.ToString());
        _ = cmd.Parameters.AddWithValue("$limit", limit);

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>
    /// Supprime toutes les entrées antérieures à <paramref name="cutoff"/>.
    /// Retourne le nombre d'entrées supprimées.
    /// </summary>
    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM audit_log WHERE timestamp < $cutoff";
        _ = cmd.Parameters.AddWithValue("$cutoff", cutoff.ToUnixTimeSeconds());

        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static AuditLogEntry Map(SqliteDataReader reader) => new(
        Id: Guid.Parse(reader.GetString(0)),
        Timestamp: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(1)),
        Type: Enum.Parse<AuditEventType>(reader.GetString(2)),
        ProfileId: reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
        Description: reader.GetString(4),
        MetadataJson: reader.IsDBNull(5) ? null : reader.GetString(5));
}
