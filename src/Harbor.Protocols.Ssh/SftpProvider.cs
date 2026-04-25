using Harbor.Core.Abstractions;
using Harbor.Core.Enums;
using Harbor.Core.Events;
using Harbor.Core.Models;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Harbor.Protocols.Ssh;

/// <summary>
/// Implémentation SFTP de <see cref="IRemoteFileSystem"/> au-dessus de SSH.NET.
/// Possède son propre <see cref="SftpClient"/> indépendant — le canal SFTP
/// est séparé du shell qui sera exposé par <c>SshShell</c> (Phase 2.5),
/// même quand les deux pointent vers le même serveur (deux connexions TCP
/// authentifiées).
/// </summary>
/// <remarks>
/// <para>
/// Capabilities exposées : <see cref="RemoteFileSystemCapabilities.UnixPermissions"/>,
/// <see cref="RemoteFileSystemCapabilities.Symlinks"/>,
/// <see cref="RemoteFileSystemCapabilities.AtomicRename"/>,
/// <see cref="RemoteFileSystemCapabilities.PartialReads"/>,
/// <see cref="RemoteFileSystemCapabilities.PartialWrites"/>.
/// </para>
/// <para>
/// SFTP ne supporte pas nativement les hard links, les attributs étendus ni
/// la surveillance temps réel — <see cref="WatchAsync"/> lève
/// <see cref="NotSupportedException"/>. Un consommateur qui voudrait du
/// pseudo-watching peut implémenter du polling au-dessus de <see cref="ListAsync"/>.
/// </para>
/// </remarks>
public sealed class SftpProvider : IRemoteFileSystem
{
    private const RemoteFileSystemCapabilities SupportedCapabilities =
        RemoteFileSystemCapabilities.UnixPermissions
        | RemoteFileSystemCapabilities.Symlinks
        | RemoteFileSystemCapabilities.AtomicRename
        | RemoteFileSystemCapabilities.PartialReads
        | RemoteFileSystemCapabilities.PartialWrites;

    private readonly SshConnection _connection;
    private SftpClient? _client;
    private ConnectionState _state = ConnectionState.Disconnected;
    private bool _disposed;

    /// <summary>Initialise le provider avec une <see cref="SshConnection"/> source.</summary>
    /// <param name="connection">
    /// Connexion SSH dont on récupère le <c>ConnectionInfo</c>. La connexion
    /// peut ne pas être encore ouverte ; <see cref="ConnectAsync"/> ouvrira
    /// le canal SFTP indépendamment.
    /// </param>
    public SftpProvider(SshConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    /// <inheritdoc />
    public RemoteFileSystemCapabilities Capabilities => SupportedCapabilities;

    /// <inheritdoc />
    public bool IsConnected => _client?.IsConnected ?? false;

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsConnected)
        {
            return;
        }

        TransitionTo(ConnectionState.Connecting);
        try
        {
            _client = new SftpClient(_connection.GetConnectionInfo());
            await _client.ConnectAsync(ct).ConfigureAwait(false);
            TransitionTo(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            TransitionTo(ConnectionState.Failed, ex.Message);
            _client?.Dispose();
            _client = null;
            throw;
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        if (_client is null)
        {
            return Task.CompletedTask;
        }

        if (_client.IsConnected)
        {
            TransitionTo(ConnectionState.Disconnecting);
            _client.Disconnect();
        }

        TransitionTo(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteFile>> ListAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        SftpClient client = EnsureConnected();

        List<RemoteFile> result = [];
        await foreach (ISftpFile entry in client.ListDirectoryAsync(path, ct).ConfigureAwait(false))
        {
            // SFTP renvoie systématiquement les entrées « . » et « .. ». On les masque
            // pour rester cohérent avec ce qu'attend l'UI (vue de répertoire).
            if (entry.Name is "." or "..")
            {
                continue;
            }

            result.Add(MapToRemoteFile(entry));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string path, long offset = 0, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "L'offset doit être positif.");
        }

        SftpClient client = EnsureConnected();
        Stream stream = await Task.Run(() => client.OpenRead(path), ct).ConfigureAwait(false);

        if (offset > 0)
        {
            if (!stream.CanSeek)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
                throw new NotSupportedException(
                    "Le flux SFTP ouvert ne supporte pas Seek ; impossible de positionner l'offset.");
            }

            _ = stream.Seek(offset, SeekOrigin.Begin);
        }

        return stream;
    }

    /// <inheritdoc />
    public async Task<Stream> OpenWriteAsync(string path, bool append = false, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        SftpClient client = EnsureConnected();

        return append
            ? await Task.Run(() => client.Open(path, FileMode.Append, FileAccess.Write), ct).ConfigureAwait(false)
            : await Task.Run(() => client.OpenWrite(path), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RemoteFile> StatAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        SftpClient client = EnsureConnected();
        ISftpFile entry = await Task.Run(() => client.Get(path), ct).ConfigureAwait(false);
        return MapToRemoteFile(entry);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        SftpClient client = EnsureConnected();

        SftpFileAttributes attrs = await Task.Run(() => client.GetAttributes(path), ct).ConfigureAwait(false);
        if (attrs.IsDirectory)
        {
            await Task.Run(() => client.DeleteDirectory(path), ct).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(() => client.DeleteFile(path), ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Crée uniquement le dossier final ; les parents inexistants ne sont pas
    /// créés (SFTP standard ne propose pas de <c>mkdir -p</c>). Un appelant qui
    /// veut ce comportement doit l'implémenter au-dessus.
    /// </remarks>
    public async Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        SftpClient client = EnsureConnected();
        await Task.Run(() => client.CreateDirectory(path), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RenameAsync(string oldPath, string newPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(oldPath);
        ArgumentException.ThrowIfNullOrEmpty(newPath);
        SftpClient client = EnsureConnected();
        await Task.Run(() => client.RenameFile(oldPath, newPath), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetPermissionsAsync(string path, UnixFileMode mode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        SftpClient client = EnsureConnected();

        // UnixFileMode en C# utilise les mêmes bits POSIX (rwxrwxrwx + sticky/setgid/setuid)
        // que le mode octal Unix. SSH.NET attend un short, on tronque les 12 bits utiles.
        short octalMode = (short)((int)mode & 0xFFF);
        await Task.Run(() => client.ChangePermissions(path, octalMode), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Non supporté : SFTP n'a pas de mécanisme de notification serveur
    /// (l'équivalent inotify n'existe pas dans le protocole SFTP standard).
    /// </remarks>
    /// <exception cref="NotSupportedException">Toujours, voir <see cref="Capabilities"/>.</exception>
    public IAsyncEnumerable<RemoteFile> WatchAsync(string path, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Le protocole SFTP ne supporte pas la surveillance temps réel. " +
            "Utiliser du polling au-dessus de ListAsync si nécessaire.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisconnectAsync().ConfigureAwait(false);
        _client?.Dispose();
        _client = null;
        _disposed = true;
    }

    private SftpClient EnsureConnected()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_client is null || !_client.IsConnected)
        {
            throw new InvalidOperationException(
                $"Le canal SFTP n'est pas ouvert. Appelez {nameof(ConnectAsync)} d'abord.");
        }

        return _client;
    }

    private void TransitionTo(ConnectionState newState, string? error = null)
    {
        ConnectionState oldState = _state;
        if (oldState == newState)
        {
            return;
        }

        _state = newState;
        ConnectionStateChanged?.Invoke(
            this,
            new ConnectionStateChangedEventArgs(oldState, newState, error));
    }

    private static RemoteFile MapToRemoteFile(ISftpFile entry)
    {
        SftpFileAttributes attrs = entry.Attributes;
        UnixFileMode? permissions = MapPermissions(attrs);

        DateTimeOffset? lastModified = attrs.LastWriteTime != default
            ? new DateTimeOffset(attrs.LastWriteTime, TimeSpan.Zero).ToUniversalTime()
            : null;

        return new RemoteFile(
            Name: entry.Name,
            FullPath: entry.FullName,
            Size: entry.Length,
            IsDirectory: entry.IsDirectory,
            IsSymlink: entry.IsSymbolicLink,
            LastModified: lastModified,
            Permissions: permissions,
            // SFTP standard expose UID/GID numériques uniquement, pas les noms.
            // Le nom symbolique nécessite un appel shell séparé (id, getent passwd).
            OwnerName: null,
            GroupName: null,
            // La cible d'un lien symbolique n'est pas dans l'attribut SFTP standard ;
            // résolution paresseuse possible via une commande readlink ultérieure.
            SymlinkTarget: null);
    }

    private static UnixFileMode? MapPermissions(SftpFileAttributes attrs)
    {
        UnixFileMode mode = UnixFileMode.None;

        if (attrs.OwnerCanRead)
        { mode |= UnixFileMode.UserRead; }
        if (attrs.OwnerCanWrite)
        { mode |= UnixFileMode.UserWrite; }
        if (attrs.OwnerCanExecute)
        { mode |= UnixFileMode.UserExecute; }

        if (attrs.GroupCanRead)
        { mode |= UnixFileMode.GroupRead; }
        if (attrs.GroupCanWrite)
        { mode |= UnixFileMode.GroupWrite; }
        if (attrs.GroupCanExecute)
        { mode |= UnixFileMode.GroupExecute; }

        if (attrs.OthersCanRead)
        { mode |= UnixFileMode.OtherRead; }
        if (attrs.OthersCanWrite)
        { mode |= UnixFileMode.OtherWrite; }
        if (attrs.OthersCanExecute)
        { mode |= UnixFileMode.OtherExecute; }

        return mode;
    }
}
