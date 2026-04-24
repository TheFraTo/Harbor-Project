using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Harbor.Data.Migrations;

/// <summary>
/// Exécute les migrations SQL embarquées dans Harbor.Data pour amener le
/// schéma au niveau attendu par la version courante de l'application.
/// </summary>
/// <remarks>
/// <para>
/// Les scripts de migration sont des fichiers <c>.sql</c> nommés
/// <c>NNNN_Description.sql</c> (ex : <c>0001_Initial.sql</c>) et embarqués
/// sous le dossier <c>Migrations/</c>. Chaque script s'exécute dans une
/// transaction et est enregistré dans la table <c>__harbor_migrations</c>
/// après succès.
/// </para>
/// <para>
/// La numérotation doit être strictement croissante et sans trou. Un script
/// une fois publié ne doit plus être modifié ; les évolutions passent par
/// une nouvelle migration.
/// </para>
/// </remarks>
public sealed class MigrationRunner
{
    private const string MigrationsTableName = "__harbor_migrations";

    private readonly HarborDbContext _context;

    /// <summary>Initialise le runner avec un contexte DB ouvert.</summary>
    public MigrationRunner(HarborDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>
    /// Applique toutes les migrations non encore appliquées. Retourne le nombre
    /// de migrations exécutées par cet appel (0 si la base était déjà à jour).
    /// </summary>
    public async Task<int> MigrateAsync(CancellationToken ct = default)
    {
        if (!_context.IsOpen)
        {
            throw new InvalidOperationException(
                $"Le contexte doit être ouvert ({nameof(HarborDbContext.OpenAsync)}) avant d'exécuter les migrations.");
        }

        List<MigrationInfo> available = DiscoverMigrations();
        HashSet<int> applied = await GetAppliedVersionsAsync(ct).ConfigureAwait(false);

        int executed = 0;
        foreach (MigrationInfo migration in available.OrderBy(m => m.Version))
        {
            if (applied.Contains(migration.Version))
            {
                continue;
            }

            await ApplyMigrationAsync(migration, ct).ConfigureAwait(false);
            executed++;
        }

        return executed;
    }

    /// <summary>Liste les migrations embarquées, triées par version croissante.</summary>
    public static IReadOnlyList<(int Version, string Name)> DiscoverAvailableMigrations() =>
        [.. DiscoverMigrations().OrderBy(m => m.Version).Select(m => (m.Version, m.Name))];

    /// <summary>Liste les versions de migrations déjà appliquées à la base.</summary>
    public async Task<IReadOnlyList<int>> GetAppliedVersionsListAsync(CancellationToken ct = default)
    {
        HashSet<int> set = await GetAppliedVersionsAsync(ct).ConfigureAwait(false);
        return [.. set.OrderBy(v => v)];
    }

    private static List<MigrationInfo> DiscoverMigrations()
    {
        Assembly asm = typeof(MigrationRunner).Assembly;
        string[] names = asm.GetManifestResourceNames();
        List<MigrationInfo> migrations = [];

        foreach (string resourceName in names)
        {
            if (!resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!resourceName.Contains(".Migrations.", StringComparison.Ordinal))
            {
                continue;
            }

            // Exemple : "Harbor.Data.Migrations.0001_Initial.sql"
            //           → on extrait "0001_Initial"
            int lastDot = resourceName.LastIndexOf('.');
            int secondLastDot = resourceName.LastIndexOf('.', lastDot - 1);
            string versionedName = resourceName.Substring(secondLastDot + 1, lastDot - secondLastDot - 1);

            int underscoreIdx = versionedName.IndexOf('_', StringComparison.Ordinal);
            if (underscoreIdx <= 0)
            {
                continue;
            }

            string versionPart = versionedName[..underscoreIdx];
            string namePart = versionedName[(underscoreIdx + 1)..];

            if (!int.TryParse(versionPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int version))
            {
                continue;
            }

            migrations.Add(new MigrationInfo(version, namePart, resourceName));
        }

        return migrations;
    }

    private async Task<HashSet<int>> GetAppliedVersionsAsync(CancellationToken ct)
    {
        if (!await MigrationsTableExistsAsync(ct).ConfigureAwait(false))
        {
            return [];
        }

        HashSet<int> versions = [];
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = $"SELECT version FROM {MigrationsTableName}";
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private async Task<bool> MigrationsTableExistsAsync(CancellationToken ct)
    {
        await using SqliteCommand cmd = _context.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name";
        _ = cmd.Parameters.AddWithValue("$name", MigrationsTableName);

        object? result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result is not null and not DBNull;
    }

    private async Task ApplyMigrationAsync(MigrationInfo migration, CancellationToken ct)
    {
        string sql = LoadEmbeddedSql(migration.ResourceName);

        await using SqliteTransaction tx = (SqliteTransaction)
            await _context.Connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            // Script principal
            await using (SqliteCommand cmd = _context.Connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            // Enregistrement de la migration. Si la table n'existait pas avant
            // (cas de la migration 0001), elle a été créée par le script.
            await using (SqliteCommand record = _context.Connection.CreateCommand())
            {
                record.Transaction = tx;
                record.CommandText = $"""
                    INSERT INTO {MigrationsTableName} (version, name, applied_at)
                    VALUES ($version, $name, $appliedAt)
                    """;
                _ = record.Parameters.AddWithValue("$version", migration.Version);
                _ = record.Parameters.AddWithValue("$name", migration.Name);
                _ = record.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                _ = await record.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private static string LoadEmbeddedSql(string resourceName)
    {
        Assembly asm = typeof(MigrationRunner).Assembly;
        using Stream stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Ressource de migration introuvable : {resourceName}");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
