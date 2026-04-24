using Harbor.Core.Models;
using Microsoft.Data.Sqlite;

namespace Harbor.Data.Repositories;

/// <summary>
/// Accès CRUD aux <see cref="Workspace"/> persistés en SQLite.
/// </summary>
/// <remarks>
/// La collection <see cref="Workspace.ProfileIds"/> retournée par les méthodes
/// de lecture est <b>toujours vide</b>. Elle est calculée par jointure côté
/// service applicatif à partir de <c>ProfileRepository</c> lorsque cette
/// information est nécessaire, pour éviter une sur-jointure automatique.
/// </remarks>
public sealed class WorkspaceRepository
{
    private readonly HarborDbContext _context;

    /// <summary>Initialise le repo avec un contexte DB ouvert.</summary>
    public WorkspaceRepository(HarborDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>Insère un nouveau workspace.</summary>
    public async Task InsertAsync(Workspace workspace, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO workspaces (id, name, icon, color, notes, created_at, updated_at)
            VALUES ($id, $name, $icon, $color, $notes, $createdAt, $updatedAt)
            """;
        _ = cmd.Parameters.AddWithValue("$id", workspace.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$name", workspace.Name);
        _ = cmd.Parameters.AddWithValue("$icon", (object?)workspace.Icon ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$color", (object?)workspace.Color ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$notes", (object?)workspace.Notes ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$createdAt", workspace.CreatedAt.ToUnixTimeSeconds());
        _ = cmd.Parameters.AddWithValue("$updatedAt", workspace.UpdatedAt.ToUnixTimeSeconds());

        _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Récupère un workspace par son Id, ou <c>null</c> s'il n'existe pas.</summary>
    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, icon, color, notes, created_at, updated_at FROM workspaces WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return Map(reader);
    }

    /// <summary>Liste tous les workspaces, ordonnés par nom.</summary>
    public async Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default)
    {
        List<Workspace> results = [];

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, icon, color, notes, created_at, updated_at FROM workspaces ORDER BY name";
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>Met à jour un workspace existant. Retourne <c>true</c> si une ligne a été modifiée.</summary>
    public async Task<bool> UpdateAsync(Workspace workspace, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            UPDATE workspaces
            SET name = $name, icon = $icon, color = $color, notes = $notes, updated_at = $updatedAt
            WHERE id = $id
            """;
        _ = cmd.Parameters.AddWithValue("$id", workspace.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$name", workspace.Name);
        _ = cmd.Parameters.AddWithValue("$icon", (object?)workspace.Icon ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$color", (object?)workspace.Color ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$notes", (object?)workspace.Notes ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$updatedAt", workspace.UpdatedAt.ToUnixTimeSeconds());

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    /// <summary>
    /// Supprime un workspace. Les profils qui lui étaient rattachés conservent
    /// leur identité mais voient leur <c>workspace_id</c> nullifié (FK ON DELETE SET NULL).
    /// Retourne <c>true</c> si le workspace existait et a été supprimé.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM workspaces WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    private static Workspace Map(SqliteDataReader reader) => new(
        Id: Guid.Parse(reader.GetString(0)),
        Name: reader.GetString(1),
        Icon: reader.IsDBNull(2) ? null : reader.GetString(2),
        Color: reader.IsDBNull(3) ? null : reader.GetString(3),
        ProfileIds: [],
        Notes: reader.IsDBNull(4) ? null : reader.GetString(4),
        CreatedAt: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(5)),
        UpdatedAt: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(6)));
}
