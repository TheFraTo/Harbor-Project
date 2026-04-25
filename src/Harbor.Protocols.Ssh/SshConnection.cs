using Harbor.Core.Enums;
using Harbor.Core.Events;
using Renci.SshNet;

namespace Harbor.Protocols.Ssh;

/// <summary>
/// Encapsule une connexion SSH (ConnectionInfo + SshClient) construite à
/// partir d'un mot de passe ou d'une clé privée déjà déchiffrée. Pierre
/// angulaire utilisée par <c>SftpProvider</c> (2.3) et <c>SshShell</c> (2.5).
/// </summary>
/// <remarks>
/// <para>
/// La classe attend des credentials <b>déjà en clair</b> : la couche services
/// (Phase 3) s'occupera de demander au keystore de déchiffrer avant
/// d'instancier la connexion. Cela permet à <c>Harbor.Protocols.Ssh</c>
/// de rester indépendant de <c>Harbor.Security</c>.
/// </para>
/// <para>
/// Pas de retry intégré (<c>RetryAttempts = 0</c>) — la reconnexion automatique
/// après coupure est la responsabilité du <c>ConnectionManager</c> (Phase 3).
/// </para>
/// <para>
/// Cette classe n'est pas thread-safe. Un consommateur unique est attendu pour
/// le cycle de vie (<see cref="ConnectAsync"/>, <see cref="DisconnectAsync"/>).
/// </para>
/// </remarks>
public sealed class SshConnection : IAsyncDisposable
{
    /// <summary>Intervalle de keep-alive par défaut : 30 secondes.</summary>
    public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(30);

    private readonly ConnectionInfo _connectionInfo;
    private SshClient? _client;
    private ConnectionState _state = ConnectionState.Disconnected;
    private bool _disposed;

    private SshConnection(ConnectionInfo connectionInfo, TimeSpan keepAliveInterval)
    {
        _connectionInfo = connectionInfo;
        KeepAliveInterval = keepAliveInterval;
    }

    /// <summary>Hôte cible (DNS ou IP).</summary>
    public string Host => _connectionInfo.Host;

    /// <summary>Port TCP cible (22 par défaut côté factory).</summary>
    public int Port => _connectionInfo.Port;

    /// <summary>Identifiant utilisateur sur la cible.</summary>
    public string Username => _connectionInfo.Username;

    /// <summary>Intervalle de keep-alive appliqué au <see cref="SshClient"/> sous-jacent.</summary>
    public TimeSpan KeepAliveInterval { get; }

    /// <summary><c>true</c> si la connexion SSH est ouverte et opérationnelle.</summary>
    public bool IsConnected => _client?.IsConnected ?? false;

    /// <summary>Émis à chaque transition de l'état de la connexion.</summary>
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Crée une connexion authentifiée par mot de passe.
    /// </summary>
    /// <param name="host">Nom DNS ou adresse IP du serveur.</param>
    /// <param name="port">Port SSH (22 par défaut).</param>
    /// <param name="username">Utilisateur distant.</param>
    /// <param name="password">Mot de passe en clair (déjà déchiffré).</param>
    /// <param name="keepAliveInterval">Intervalle de keep-alive ; <see cref="DefaultKeepAliveInterval"/> si <c>null</c>.</param>
    public static SshConnection WithPassword(
        string host,
        int port,
        string username,
        string password,
        TimeSpan? keepAliveInterval = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(host);
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentNullException.ThrowIfNull(password);
        ValidatePort(port);

        PasswordAuthenticationMethod auth = new(username, password);
        ConnectionInfo info = new(host, port, username, auth)
        {
            RetryAttempts = 0,
        };

        return new SshConnection(info, keepAliveInterval ?? DefaultKeepAliveInterval);
    }

    /// <summary>
    /// Crée une connexion authentifiée par clé privée (Ed25519, RSA, ECDSA).
    /// La clé est fournie en bytes au format OpenSSH ou PEM, déjà déchiffrée
    /// du keystore Harbor. Une <paramref name="passphrase"/> est requise si
    /// la clé elle-même est chiffrée (ex: clé OpenSSH protégée).
    /// </summary>
    /// <param name="host">Nom DNS ou adresse IP du serveur.</param>
    /// <param name="port">Port SSH.</param>
    /// <param name="username">Utilisateur distant.</param>
    /// <param name="privateKeyMaterial">Bytes de la clé privée au format OpenSSH/PEM.</param>
    /// <param name="passphrase">Passphrase de la clé, ou <c>null</c> si la clé n'en a pas.</param>
    /// <param name="keepAliveInterval">Intervalle de keep-alive ; défaut si <c>null</c>.</param>
    public static SshConnection WithPrivateKey(
        string host,
        int port,
        string username,
        byte[] privateKeyMaterial,
        string? passphrase = null,
        TimeSpan? keepAliveInterval = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(host);
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentNullException.ThrowIfNull(privateKeyMaterial);
        if (privateKeyMaterial.Length == 0)
        {
            throw new ArgumentException("Le matériau de clé privée ne peut pas être vide.", nameof(privateKeyMaterial));
        }

        ValidatePort(port);

        using MemoryStream keyStream = new(privateKeyMaterial);
        PrivateKeyFile keyFile = string.IsNullOrEmpty(passphrase)
            ? new PrivateKeyFile(keyStream)
            : new PrivateKeyFile(keyStream, passphrase);

        PrivateKeyAuthenticationMethod auth = new(username, keyFile);
        ConnectionInfo info = new(host, port, username, auth)
        {
            RetryAttempts = 0,
        };

        return new SshConnection(info, keepAliveInterval ?? DefaultKeepAliveInterval);
    }

    /// <summary>
    /// Établit la connexion SSH au serveur. Idempotent : si la connexion est
    /// déjà ouverte, retourne immédiatement.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Si la connexion a déjà été disposée.</exception>
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
            _client = new SshClient(_connectionInfo)
            {
                KeepAliveInterval = KeepAliveInterval,
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

    /// <summary>
    /// Ferme proprement la connexion. Ne lève pas si la connexion n'a jamais
    /// été ouverte ou est déjà fermée.
    /// </summary>
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

    /// <summary>
    /// Accès interne au client SSH sous-jacent pour <c>SshShell</c> et autres
    /// consommateurs internes du provider. Lance une exception si la connexion
    /// n'est pas active.
    /// </summary>
    internal SshClient GetClient()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_client is null || !_client.IsConnected)
        {
            throw new InvalidOperationException(
                $"La connexion n'est pas ouverte. Appelez {nameof(ConnectAsync)} d'abord.");
        }

        return _client;
    }

    /// <summary>
    /// Accès interne aux paramètres de connexion pour permettre à
    /// <c>SftpProvider</c> de spawner son propre <c>SftpClient</c> sur la
    /// même cible avec les mêmes credentials.
    /// </summary>
    internal ConnectionInfo GetConnectionInfo()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _connectionInfo;
    }

    /// <summary>Ferme la connexion et libère les ressources SSH.NET.</summary>
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

    private static void ValidatePort(int port)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                port,
                "Le port doit être entre 1 et 65535.");
        }
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
