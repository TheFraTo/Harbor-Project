using Harbor.Core.Enums;
using Harbor.Core.Models;
using Microsoft.Data.Sqlite;

namespace Harbor.Data.Repositories;

/// <summary>
/// Accès aux <see cref="Transfer"/> de la file d'attente persistante.
/// Le moteur de transferts (<c>TransferEngine</c>) utilise ce repo pour
/// hydrater la queue au démarrage et persister les changements de statut
/// et de progression.
/// </summary>
public sealed class TransferRepository
{
    private readonly HarborDbContext _context;

    /// <summary>Initialise le repo avec un contexte DB ouvert.</summary>
    public TransferRepository(HarborDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>Insère un nouveau transfert.</summary>
    public async Task InsertAsync(Transfer transfer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transfer);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO transfers (
                id, direction, source_path, dest_path,
                source_profile_id, dest_profile_id,
                total_bytes, transferred_bytes,
                status, error_message, priority,
                created_at, completed_at
            )
            VALUES (
                $id, $direction, $sourcePath, $destPath,
                $sourceProfileId, $destProfileId,
                $totalBytes, $transferredBytes,
                $status, $errorMessage, $priority,
                $createdAt, $completedAt
            )
            """;
        _ = cmd.Parameters.AddWithValue("$id", transfer.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$direction", transfer.Direction.ToString());
        _ = cmd.Parameters.AddWithValue("$sourcePath", transfer.SourcePath);
        _ = cmd.Parameters.AddWithValue("$destPath", transfer.DestPath);
        _ = cmd.Parameters.AddWithValue(
            "$sourceProfileId",
            (object?)transfer.SourceProfileId?.ToString() ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue(
            "$destProfileId",
            (object?)transfer.DestProfileId?.ToString() ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$totalBytes", transfer.TotalBytes);
        _ = cmd.Parameters.AddWithValue("$transferredBytes", transfer.TransferredBytes);
        _ = cmd.Parameters.AddWithValue("$status", transfer.Status.ToString());
        _ = cmd.Parameters.AddWithValue("$errorMessage", (object?)transfer.ErrorMessage ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$priority", transfer.Priority);
        _ = cmd.Parameters.AddWithValue("$createdAt", transfer.CreatedAt.ToUnixTimeSeconds());
        _ = cmd.Parameters.AddWithValue(
            "$completedAt",
            (object?)transfer.CompletedAt?.ToUnixTimeSeconds() ?? DBNull.Value);

        _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Récupère un transfert par son Id, ou <c>null</c> s'il n'existe pas.</summary>
    public async Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = SelectColumns + "FROM transfers WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return Map(reader);
    }

    /// <summary>
    /// Liste les transferts, optionnellement filtrés par statut, ordonnés par
    /// priorité décroissante puis date de création croissante (ordre de
    /// scheduling utilisé par le moteur).
    /// </summary>
    public async Task<IReadOnlyList<Transfer>> GetAllAsync(
        TransferStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        List<Transfer> results = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = statusFilter is null
            ? SelectColumns + "FROM transfers ORDER BY priority DESC, created_at ASC"
            : SelectColumns + "FROM transfers WHERE status = $status ORDER BY priority DESC, created_at ASC";

        if (statusFilter is { } status)
        {
            _ = cmd.Parameters.AddWithValue("$status", status.ToString());
        }

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>Met à jour la progression et le statut d'un transfert (champs mutables).</summary>
    public async Task<bool> UpdateProgressAsync(
        Guid id,
        long transferredBytes,
        TransferStatus status,
        string? errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            UPDATE transfers
            SET transferred_bytes = $transferred,
                status = $status,
                error_message = $error,
                completed_at = $completedAt
            WHERE id = $id
            """;
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());
        _ = cmd.Parameters.AddWithValue("$transferred", transferredBytes);
        _ = cmd.Parameters.AddWithValue("$status", status.ToString());
        _ = cmd.Parameters.AddWithValue("$error", (object?)errorMessage ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue(
            "$completedAt",
            (object?)completedAt?.ToUnixTimeSeconds() ?? DBNull.Value);

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    /// <summary>Supprime un transfert (typiquement après nettoyage des transferts terminés anciens).</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM transfers WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    private const string SelectColumns = """
        SELECT id, direction, source_path, dest_path,
               source_profile_id, dest_profile_id,
               total_bytes, transferred_bytes,
               status, error_message, priority,
               created_at, completed_at
        """ + " ";

    private static Transfer Map(SqliteDataReader reader) => new(
        Id: Guid.Parse(reader.GetString(0)),
        Direction: Enum.Parse<TransferDirection>(reader.GetString(1)),
        SourcePath: reader.GetString(2),
        DestPath: reader.GetString(3),
        SourceProfileId: reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
        DestProfileId: reader.IsDBNull(5) ? null : Guid.Parse(reader.GetString(5)),
        TotalBytes: reader.GetInt64(6),
        TransferredBytes: reader.GetInt64(7),
        Status: Enum.Parse<TransferStatus>(reader.GetString(8)),
        ErrorMessage: reader.IsDBNull(9) ? null : reader.GetString(9),
        Priority: reader.GetInt32(10),
        CreatedAt: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(11)),
        CompletedAt: reader.IsDBNull(12) ? null : DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(12)));
}
