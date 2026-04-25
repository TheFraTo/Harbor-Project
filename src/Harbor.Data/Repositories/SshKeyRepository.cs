using Harbor.Core.Common;
using Harbor.Core.Enums;
using Harbor.Core.Models;
using Microsoft.Data.Sqlite;

namespace Harbor.Data.Repositories;

/// <summary>
/// Accès CRUD aux <see cref="SshKey"/> du keystore. La clé privée
/// (<see cref="EncryptedBytes"/>) est stockée éclatée en 3 BLOB :
/// <c>private_key_nonce</c>, <c>private_key_ciphertext</c>, <c>private_key_tag</c>.
/// </summary>
public sealed class SshKeyRepository
{
    private readonly HarborDbContext _context;

    /// <summary>Initialise le repo avec un contexte DB ouvert.</summary>
    public SshKeyRepository(HarborDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>Insère une nouvelle clé SSH dans le keystore.</summary>
    public async Task InsertAsync(SshKey key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ssh_keys (
                id, name, algorithm,
                private_key_nonce, private_key_ciphertext, private_key_tag,
                public_key, comment, created_at, last_used_at
            )
            VALUES (
                $id, $name, $algorithm,
                $nonce, $ciphertext, $tag,
                $publicKey, $comment, $createdAt, $lastUsedAt
            )
            """;
        _ = cmd.Parameters.AddWithValue("$id", key.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$name", key.Name);
        _ = cmd.Parameters.AddWithValue("$algorithm", key.Algorithm.ToString());
        _ = cmd.Parameters.AddWithValue("$nonce", key.PrivateKey.Nonce);
        _ = cmd.Parameters.AddWithValue("$ciphertext", key.PrivateKey.Ciphertext);
        _ = cmd.Parameters.AddWithValue("$tag", key.PrivateKey.Tag);
        _ = cmd.Parameters.AddWithValue("$publicKey", key.PublicKey);
        _ = cmd.Parameters.AddWithValue("$comment", (object?)key.Comment ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$createdAt", key.CreatedAt.ToUnixTimeSeconds());
        _ = cmd.Parameters.AddWithValue(
            "$lastUsedAt",
            (object?)key.LastUsedAt?.ToUnixTimeSeconds() ?? DBNull.Value);

        _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Récupère une clé par son Id, ou <c>null</c> si elle n'existe pas.</summary>
    public async Task<SshKey?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, algorithm,
                   private_key_nonce, private_key_ciphertext, private_key_tag,
                   public_key, comment, created_at, last_used_at
            FROM ssh_keys WHERE id = $id
            """;
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return Map(reader);
    }

    /// <summary>Liste toutes les clés, ordonnées par nom.</summary>
    public async Task<IReadOnlyList<SshKey>> GetAllAsync(CancellationToken ct = default)
    {
        List<SshKey> results = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, algorithm,
                   private_key_nonce, private_key_ciphertext, private_key_tag,
                   public_key, comment, created_at, last_used_at
            FROM ssh_keys ORDER BY name
            """;
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>Met à jour le nom, le commentaire et la date de dernière utilisation.</summary>
    /// <remarks>
    /// La rotation d'une clé (changement de matériau cryptographique) passe par
    /// <see cref="DeleteAsync"/> + <see cref="InsertAsync"/>, jamais par UPDATE,
    /// pour garantir une trace claire dans l'audit.
    /// </remarks>
    public async Task<bool> UpdateMetadataAsync(
        Guid id,
        string name,
        string? comment,
        DateTimeOffset? lastUsedAt,
        CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            UPDATE ssh_keys
            SET name = $name, comment = $comment, last_used_at = $lastUsedAt
            WHERE id = $id
            """;
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());
        _ = cmd.Parameters.AddWithValue("$name", name);
        _ = cmd.Parameters.AddWithValue("$comment", (object?)comment ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue(
            "$lastUsedAt",
            (object?)lastUsedAt?.ToUnixTimeSeconds() ?? DBNull.Value);

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    /// <summary>Supprime une clé. Retourne <c>true</c> si la ligne existait.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ssh_keys WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    private static SshKey Map(SqliteDataReader reader)
    {
        EncryptedBytes privateKey = new(
            Nonce: (byte[])reader[3],
            Ciphertext: (byte[])reader[4],
            Tag: (byte[])reader[5]);

        return new SshKey(
            Id: Guid.Parse(reader.GetString(0)),
            Name: reader.GetString(1),
            Algorithm: Enum.Parse<KeyAlgorithm>(reader.GetString(2)),
            PrivateKey: privateKey,
            PublicKey: (byte[])reader[6],
            Comment: reader.IsDBNull(7) ? null : reader.GetString(7),
            CreatedAt: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(8)),
            LastUsedAt: reader.IsDBNull(9) ? null : DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(9)));
    }
}
