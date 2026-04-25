using System.Text.Json;
using Harbor.Core.Models;
using Harbor.Data.Json;
using Microsoft.Data.Sqlite;

namespace Harbor.Data.Repositories;

/// <summary>
/// Accès CRUD aux <see cref="Snippet"/> de commandes paramétrées.
/// Les variables (<see cref="SnippetVariable"/>) sont sérialisées en JSON
/// dans la colonne <c>variables_json</c>. Les tags sont stockés en CSV.
/// </summary>
public sealed class SnippetRepository
{
    private readonly HarborDbContext _context;

    /// <summary>Initialise le repo avec un contexte DB ouvert.</summary>
    public SnippetRepository(HarborDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>Insère un nouveau snippet.</summary>
    public async Task InsertAsync(Snippet snippet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snippet);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO snippets (id, name, description, command, variables_json, tags, created_at)
            VALUES ($id, $name, $description, $command, $variablesJson, $tags, $createdAt)
            """;
        _ = cmd.Parameters.AddWithValue("$id", snippet.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$name", snippet.Name);
        _ = cmd.Parameters.AddWithValue("$description", (object?)snippet.Description ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$command", snippet.Command);
        _ = cmd.Parameters.AddWithValue(
            "$variablesJson",
            snippet.Variables.Count == 0
                ? DBNull.Value
                : JsonSerializer.Serialize(snippet.Variables, HarborJsonOptions.Default));
        _ = cmd.Parameters.AddWithValue(
            "$tags",
            snippet.Tags.Count == 0 ? DBNull.Value : string.Join(',', snippet.Tags));
        _ = cmd.Parameters.AddWithValue("$createdAt", snippet.CreatedAt.ToUnixTimeSeconds());

        _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Récupère un snippet par son Id, ou <c>null</c> s'il n'existe pas.</summary>
    public async Task<Snippet?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, description, command, variables_json, tags, created_at FROM snippets WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return Map(reader);
    }

    /// <summary>Liste tous les snippets, ordonnés par nom.</summary>
    public async Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken ct = default)
    {
        List<Snippet> results = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, description, command, variables_json, tags, created_at FROM snippets ORDER BY name";
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    /// <summary>Met à jour un snippet existant. Retourne <c>true</c> si une ligne a été modifiée.</summary>
    public async Task<bool> UpdateAsync(Snippet snippet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snippet);

        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = """
            UPDATE snippets
            SET name = $name, description = $description, command = $command,
                variables_json = $variablesJson, tags = $tags
            WHERE id = $id
            """;
        _ = cmd.Parameters.AddWithValue("$id", snippet.Id.ToString());
        _ = cmd.Parameters.AddWithValue("$name", snippet.Name);
        _ = cmd.Parameters.AddWithValue("$description", (object?)snippet.Description ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("$command", snippet.Command);
        _ = cmd.Parameters.AddWithValue(
            "$variablesJson",
            snippet.Variables.Count == 0
                ? DBNull.Value
                : JsonSerializer.Serialize(snippet.Variables, HarborJsonOptions.Default));
        _ = cmd.Parameters.AddWithValue(
            "$tags",
            snippet.Tags.Count == 0 ? DBNull.Value : string.Join(',', snippet.Tags));

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    /// <summary>Supprime un snippet.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM snippets WHERE id = $id";
        _ = cmd.Parameters.AddWithValue("$id", id.ToString());

        int rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    private static Snippet Map(SqliteDataReader reader)
    {
        IReadOnlyList<SnippetVariable> variables = reader.IsDBNull(4)
            ? []
            : JsonSerializer.Deserialize<List<SnippetVariable>>(
                reader.GetString(4), HarborJsonOptions.Default) ?? [];

        IReadOnlyList<string> tags = reader.IsDBNull(5)
            ? []
            : reader.GetString(5).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new Snippet(
            Id: Guid.Parse(reader.GetString(0)),
            Name: reader.GetString(1),
            Description: reader.IsDBNull(2) ? null : reader.GetString(2),
            Command: reader.GetString(3),
            Variables: variables,
            Tags: tags,
            CreatedAt: DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(6)));
    }
}
