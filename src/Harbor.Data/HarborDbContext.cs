using Microsoft.Data.Sqlite;

namespace Harbor.Data;

/// <summary>
/// Contexte d'accès à la base SQLite chiffrée de Harbor. Ouvre une connexion
/// unique persistante, applique la clé SQLCipher, configure les pragmas
/// standards (WAL, foreign_keys, busy_timeout).
/// </summary>
/// <remarks>
/// <para>
/// Un seul <see cref="HarborDbContext"/> est attendu par processus : il
/// encapsule la connexion partagée utilisée par tous les repositories.
/// Les opérations de lecture/écriture doivent utiliser <see cref="Connection"/>
/// après appel réussi à <see cref="OpenAsync"/>.
/// </para>
/// <para>
/// SQLite autorise plusieurs lecteurs concurrents mais un seul writer ;
/// les repositories sérialisent leurs écritures via un verrou interne de niveau
/// supérieur (cf. <c>Harbor.Data.Internal</c>).
/// </para>
/// </remarks>
public sealed class HarborDbContext : IAsyncDisposable, IDisposable
{
    private readonly HarborDbOptions _options;
    private SqliteConnection? _connection;
    private bool _disposed;

    /// <summary>Initialise le contexte avec les options fournies (ne connecte pas encore).</summary>
    public HarborDbContext(HarborDbOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.EncryptionKey.Length != 32)
        {
            throw new ArgumentException(
                $"La clé de chiffrement doit faire exactement 32 octets (AES-256). Reçu : {options.EncryptionKey.Length}.",
                nameof(options));
        }

        _options = options;
    }

    /// <summary>Indique si la connexion est ouverte et prête à servir des requêtes.</summary>
    public bool IsOpen => _connection is { State: System.Data.ConnectionState.Open };

    /// <summary>
    /// Connexion SQLite partagée. Disponible uniquement après <see cref="OpenAsync"/>.
    /// Lance <see cref="InvalidOperationException"/> si appelée avant ouverture.
    /// </summary>
    public SqliteConnection Connection =>
        _connection ?? throw new InvalidOperationException(
            $"La connexion n'est pas ouverte. Appelez {nameof(OpenAsync)} d'abord.");

    /// <summary>
    /// Ouvre la connexion SQLite, applique la clé SQLCipher et configure les pragmas.
    /// Idempotent : un second appel ne fait rien si la connexion est déjà ouverte.
    /// </summary>
    public async Task OpenAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsOpen)
        {
            return;
        }

        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = _options.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            DefaultTimeout = (int)_options.EffectiveBusyTimeout.TotalSeconds,
        };

        _connection = new SqliteConnection(builder.ConnectionString);
        await _connection.OpenAsync(ct).ConfigureAwait(false);

        await ApplyEncryptionKeyAsync(ct).ConfigureAwait(false);
        await ApplyPragmasAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Ferme proprement la connexion.</summary>
    public async Task CloseAsync()
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.CloseAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
        _connection = null;
    }

    /// <summary>Libération synchrone (préférer <see cref="DisposeAsync"/>).</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _connection?.Dispose();
        _connection = null;
        _disposed = true;
    }

    /// <summary>Libération asynchrone : ferme proprement la connexion.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await CloseAsync().ConfigureAwait(false);
        _disposed = true;
    }

    private async Task ApplyEncryptionKeyAsync(CancellationToken ct)
    {
        // SQLCipher accepte la clé en hex via le format x'HHHH...'.
        // Cette forme évite toute dérivation interne (PBKDF2) : les 32 octets
        // sont utilisés tels quels comme clé de chiffrement AES-256.
        string hex = Convert.ToHexString(_options.EncryptionKey);
        string sql = $"PRAGMA key = \"x'{hex}'\";";

        await using SqliteCommand cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task ApplyPragmasAsync(CancellationToken ct)
    {
        List<string> pragmas =
        [
            "PRAGMA foreign_keys = ON;",
            $"PRAGMA busy_timeout = {(int)_options.EffectiveBusyTimeout.TotalMilliseconds};",
            "PRAGMA synchronous = NORMAL;",
            "PRAGMA temp_store = MEMORY;",
        ];

        if (_options.EnableWal)
        {
            pragmas.Add("PRAGMA journal_mode = WAL;");
        }

        foreach (string sql in pragmas)
        {
            await using SqliteCommand cmd = _connection!.CreateCommand();
            cmd.CommandText = sql;
            _ = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

}
