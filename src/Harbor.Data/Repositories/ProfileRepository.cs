using System.Text.Json;
using Harbor.Core.Enums;
using Harbor.Core.Models;
using Harbor.Data.Json;
using Microsoft.Data.Sqlite;

namespace Harbor.Data.Repositories;

/// <summary>
/// Accès CRUD aux <see cref="Profile"/> persistés. Les champs polymorphiques
/// <see cref="Profile.Connection"/> et <see cref="Profile.Auth"/> sont
/// sérialisés en JSON via <c>System.Text.Json</c> avec discriminant <c>$kind</c>.
/// Tags en CSV, EnvVars en JSON.
/// </summary>
public sealed class ProfileRepository
{
    private readonly HarborDbContext _context;

    /// <summary>Initialise le repo avec un contexte DB ouvert.</summary>
    public ProfileRepository(HarborDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>Insère un nouveau profil.</summary>
    public async Task InsertAsync(Profile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO profiles (
                id, name, protocol, workspace_id, parent_folder_id,
                connection_json, auth_json, tags, env_vars_json,
                post_connect_script, notes,
                created_at, updated_at, last_used_at
            )
            VALUES (
                $id, $name, $protocol, $workspaceId, $parentFolderId,
                $connectionJson, $authJson, $tags, $envVarsJson,
                $postConnectScript, $notes,
                $createdAt, $updatedAt, $lastUsedAt
            )
            """;
        BindInsertOrUpdateParameters(cmd, profile);
        _ = cmd.Parameters.AddWithValue("$id", profile.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$createdAt", profile.CreatedAt.ToUnixTimeSeconds());

        _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Récupère un profil par son Id, ou <c>null</c> s'il n'existe pas.</summary>
    public async Task<Profile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = SelectColumns + "FROM profiles WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return Map(reader);
    }

    /// <summary>Liste tous les profils, ordonnés par nom.</summary>
    public async Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken ct = default)
    {
        List<Profile> results = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = SelectColumns + "FROM profiles ORDER BY name";
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>Liste les profils d'un workspace donné.</summary>
    public async Task<IReadOnlyList<Profile>> GetByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct = default)
    {
        List<Profile> results = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = SelectColumns + "FROM profiles WHERE workspace_id = $workspaceId ORDER BY name";
        _ = cmd.Parameters.AddWithValue("$workspaceId", workspaceId.ToString());
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>
    /// Récupère uniquement les Ids des profils appartenant à un workspace,
    /// utilisé par <c>WorkspaceService</c> pour hydrater <see cref="Workspace.ProfileIds"/>.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetIdsByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct = default)
    {
        List<Guid> ids = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM profiles WHERE workspace_id = $workspaceId ORDER BY name";
        _ = cmd.Parameters.AddWithValue("$workspaceId", workspaceId.ToString());
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            ids.Add(Guid.Parse(reader.GetString(0)));
        }

        return ids;
    }

    /// <summary>Met à jour un profil existant. Retourne <c>true</c> si une ligne a été modifiée.</summary>
    public async Task<bool> UpdateAsync(Profile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            UPDATE profiles
            SET name = $name, protocol = $protocol,
                workspace_id = $workspaceId, parent_folder_id = $parentFolderId,
                connection_json = $connectionJson, auth_json = $authJson,
                tags = $tags, env_vars_json = $envVarsJson,
                post_connect_script = $postConnectScript, notes = $notes,
                updated_at = $updatedAt, last_used_at = $lastUsedAt
            WHERE id = $id
            """;
        BindInsertOrUpdateParameters(cmd, profile);
        _ = cmd.Parameters.AddWithValue("$id", profile.Id.ToString());

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    /// <summary>Supprime un profil.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM profiles WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    private const string SelectColumns = """
        SELECT id, name, protocol, workspace_id, parent_folder_id,
               connection_json, auth_json, tags, env_vars_json,
               post_connect_script, notes,
               created_at, updated_at, last_used_at
        """ + " ";

    private static void BindInsertOrUpdateParameters(SqliteCommand cmd, Profile profile)
    {
        string connectionJson = JsonSerializer.Serialize(profile.Connection, HarborJsonOptions.Default);
        string authJson = JsonSerializer.Serialize(profile.Auth, HarborJsonOptions.Default);
        string envVarsJson = profile.EnvVars.Count == 0
            ? "{}"
            : JsonSerializer.Serialize(profile.EnvVars, HarborJsonOptions.Default);
        string? tagsCsv = profile.Tags.Count == 0 ? null : string.Join(',', profile.Tags);

        _ = cmd.Parameters.AddWithValue("$name", profile.Name);
        _ = cmd.Parameters.AddWithValue("$protocol", profile.Protocol.ToString());
        _ = cmd.Parameters.AddWithValue("$connectionJson", connectionJson);
        _ = cmd.Parameters.AddWithValue("$authJson", authJson);
        _ = cmd.Parameters.AddWithValue("$tags", (object?)tagsCsv ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$envVarsJson", envVarsJson);
        _ = cmd.Parameters.AddWithValue("$postConnectScript", (object?)profile.PostConnectScript ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$notes", (object?)profile.Notes ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$workspaceId", DBNull.Value); // Le workspace_id est géré séparément
        _ = cmd.Parameters.AddWithValue(
            "$parentFolderId",
            (object?)profile.ParentFolderId?.ToString() ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$updatedAt", profile.UpdatedAt.ToUnixTimeSeconds());
        _ = cmd.Parameters.AddWithValue(
            "$lastUsedAt",
            (object?)profile.LastUsedAt?.ToUnixTimeSeconds() ?? DBNull.Value);
    }

    private static Profile Map(SqliteDataReader reader)
    {
        ConnectionDetails connection = JsonSerializer.Deserialize<ConnectionDetails>(
            reader.GetString(5), HarborJsonOptions.Default)
            ?? throw new InvalidOperationException("connection_json invalide.");

        AuthenticationMethod auth = JsonSerializer.Deserialize<AuthenticationMethod>(
            reader.GetString(6), HarborJsonOptions.Default)
            ?? throw new InvalidOperationException("auth_json invalide.");

        IReadOnlyList<string> tags = reader.IsDBNull(7)
            ? []
            : reader.GetString(7).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IReadOnlyDictionary<string, string> envVars = reader.IsDBNull(8)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(
                reader.GetString(8), HarborJsonOptions.Default)
                ?? new Dictionary<string, string>();

        return new Profile(
            Id: Guid.Parse(reader.GetString(0)),
            Name: reader.GetString(1),
            Protocol: Enum.Parse<ProtocolKind>(reader.GetString(2)),
            Connection: connection,
            Auth: auth,
            Tags: tags,
            ParentFolderId: reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
            EnvVars: envVars,
            PostConnectScript: reader.IsDBNull(9) ? null : reader.GetString(9),
            Notes: reader.IsDBNull(10) ? null : reader.GetString(10),
            CreatedAt: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(11)),
            UpdatedAt: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(12)),
            LastUsedAt: reader.IsDBNull(13) ? null : DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(13)));
    }
}
