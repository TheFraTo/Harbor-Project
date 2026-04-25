using System.Text;
using Harbor.Core.Abstractions;
using Harbor.Core.Common;
using Harbor.Core.Enums;
using Harbor.Core.Events;
using Renci.SshNet;

namespace Harbor.Protocols.Ssh;

/// <summary>
/// Implémentation SSH de <see cref="IRemoteShell"/> au-dessus de SSH.NET.
/// Possède son propre <see cref="SshClient"/> indépendant (canal séparé du
/// <see cref="SftpProvider"/>) pour respecter la séparation shell/SFTP de SSH.NET.
/// </summary>
/// <remarks>
/// <para>
/// Brique 2.5 : seule la méthode <see cref="ExecuteAsync"/> non-interactive est
/// pleinement implémentée. Les méthodes <see cref="StartInteractiveSessionAsync"/>
/// (sessions PTY) et <c>CreateXxxForwardAsync</c> (port forwarding) lèvent
/// <see cref="NotImplementedException"/> avec un message pointant sur leurs
/// briques respectives (2.6 et 2.7).
/// </para>
/// <para>
/// <see cref="ExecuteAsync"/> capture stdout et stderr <b>après exécution</b>
/// (post-hoc), pas en temps réel. Acceptable pour les commandes courtes
/// (uptime, df, systemctl status). Pour du streaming progressif (ex: <c>tail -f</c>),
/// utiliser <see cref="StartInteractiveSessionAsync"/> qui ouvrira un PTY (brique 2.6).
/// </para>
/// </remarks>
public sealed class SshShell : IRemoteShell
{
    private readonly SshConnection _connection;
    private SshClient? _client;
    private ConnectionState _state = ConnectionState.Disconnected;
    private bool _disposed;

    /// <summary>Initialise le shell avec une <see cref="SshConnection"/> source.</summary>
    /// <param name="connection">
    /// Connexion SSH dont on récupère le <c>ConnectionInfo</c>. La connexion
    /// peut ne pas être encore ouverte ; <see cref="ConnectAsync"/> ouvrira
    /// le canal shell indépendamment.
    /// </param>
    public SshShell(SshConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

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
            _client = new SshClient(_connection.GetConnectionInfo())
            {
                KeepAliveInterval = _connection.KeepAliveInterval,
            };
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
    /// <remarks>
    /// Stdout et stderr sont capturés intégralement par SSH.NET pendant l'exécution
    /// puis copiés (encoding UTF-8) vers les streams fournis <i>après</i> la fin
    /// de la commande. Pour du streaming temps-réel, utiliser une session
    /// interactive (brique 2.6).
    /// </remarks>
    public async Task<int> ExecuteAsync(
        string command,
        Stream? stdout = null,
        Stream? stderr = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(command);
        SshClient client = EnsureConnected();

        using SshCommand cmd = client.CreateCommand(command);
        await cmd.ExecuteAsync(ct).ConfigureAwait(false);

        if (stdout is not null && !string.IsNullOrEmpty(cmd.Result))
        {
            byte[] outBytes = Encoding.UTF8.GetBytes(cmd.Result);
            await stdout.WriteAsync(outBytes, ct).ConfigureAwait(false);
        }

        if (stderr is not null && !string.IsNullOrEmpty(cmd.Error))
        {
            byte[] errBytes = Encoding.UTF8.GetBytes(cmd.Error);
            await stderr.WriteAsync(errBytes, ct).ConfigureAwait(false);
        }

        return cmd.ExitStatus ?? -1;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Crée un <see cref="ShellStream"/> avec un terminal <c>xterm-256color</c>
    /// et les dimensions fournies, puis l'enveloppe dans une
    /// <see cref="SshInteractiveSession"/>. Les dimensions en pixels (width/height)
    /// sont calculées approximativement (8x16 par cellule) et utilisées seulement
    /// par les applications qui rendent du graphique inline (rare).
    /// </remarks>
    public Task<IInteractiveSession> StartInteractiveSessionAsync(
        TerminalSize size,
        CancellationToken ct = default)
    {
        SshClient client = EnsureConnected();
        if (size.Columns <= 0 || size.Rows <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Les dimensions du terminal doivent être strictement positives.");
        }

        ct.ThrowIfCancellationRequested();

        ShellStream stream = client.CreateShellStream(
            terminalName: "xterm-256color",
            columns: (uint)size.Columns,
            rows: (uint)size.Rows,
            width: (uint)(size.Columns * 8),
            height: (uint)(size.Rows * 16),
            bufferSize: 4096);

        IInteractiveSession session = new SshInteractiveSession(stream);
        return Task.FromResult(session);
    }

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">À implémenter dans la brique 2.7 (port forwarding + jump hosts).</exception>
    public Task<IPortForward> CreateLocalForwardAsync(
        int localPort,
        string remoteHost,
        int remotePort,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "Port forwarding local non encore disponible. Implémentation prévue brique 2.7.");
    }

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">À implémenter dans la brique 2.7.</exception>
    public Task<IPortForward> CreateRemoteForwardAsync(
        int remotePort,
        string localHost,
        int localPort,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "Port forwarding remote non encore disponible. Implémentation prévue brique 2.7.");
    }

    /// <inheritdoc />
    /// <exception cref="NotImplementedException">À implémenter dans la brique 2.7.</exception>
    public Task<IPortForward> CreateDynamicForwardAsync(
        int localPort,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "Port forwarding dynamique (SOCKS) non encore disponible. Implémentation prévue brique 2.7.");
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

    private SshClient EnsureConnected()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_client is null || !_client.IsConnected)
        {
            throw new InvalidOperationException(
                $"Le shell SSH n'est pas ouvert. Appelez {nameof(ConnectAsync)} d'abord.");
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
}
