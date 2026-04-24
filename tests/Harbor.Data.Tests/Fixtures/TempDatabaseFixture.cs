using Harbor.Data.Migrations;

namespace Harbor.Data.Tests.Fixtures;

/// <summary>
/// Fournit une base SQLite temporaire chiffrée et initialisée (migrations
/// appliquées) pour un test d'intégration. Le fichier est supprimé à la fin.
/// </summary>
public sealed class TempDatabaseFixture : IAsyncDisposable
{
    private readonly string _path;

    private TempDatabaseFixture(HarborDbContext context, string path)
    {
        Context = context;
        _path = path;
    }

    /// <summary>Contexte DB ouvert et migré.</summary>
    public HarborDbContext Context { get; }

    /// <summary>Crée une nouvelle base temporaire, l'ouvre et applique les migrations.</summary>
    public static async Task<TempDatabaseFixture> CreateAsync(CancellationToken ct = default)
    {
        string path = Path.Combine(Path.GetTempPath(), $"harbor-test-{Guid.NewGuid():N}.db");
        byte[] key = HarborDbContext.DeriveTestKey("integration-tests-passphrase");
        HarborDbOptions options = new(path, key);

        HarborDbContext ctx = new(options);
        await ctx.OpenAsync(ct).ConfigureAwait(false);

        MigrationRunner runner = new(ctx);
        _ = await runner.MigrateAsync(ct).ConfigureAwait(false);

        return new TempDatabaseFixture(ctx, path);
    }

    /// <summary>Ferme la connexion et supprime le fichier DB ainsi que ses annexes WAL.</summary>
    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync().ConfigureAwait(false);

        // SQLite peut laisser des annexes -wal et -shm à côté du fichier principal.
        foreach (string suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            string file = _path + suffix;
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // Best-effort cleanup : on ignore si le fichier est encore verrouillé par SQLite
                    // sur certaines configurations Windows. Le tempdir sera nettoyé par l'OS.
                }
            }
        }
    }
}
