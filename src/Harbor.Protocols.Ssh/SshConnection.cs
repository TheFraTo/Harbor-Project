using System.Net;
using System.Net.Sockets;
using Harbor.Core.Enums;
using Harbor.Core.Events;
using Renci.SshNet;

namespace Harbor.Protocols.Ssh;

/// <summary>
/// Encapsule une connexion SSH (auth + client) construite à partir d'un mot de
/// passe ou d'une clé privée déjà déchiffrée. Pierre angulaire utilisée par
/// <c>SftpProvider</c> (2.3) et <c>SshShell</c> (2.5). Supporte aussi la
/// connexion via bastion unique (brique 2.7) en orchestrant un port forward
/// local sur le bastion.
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

    private readonly string _targetHost;
    private readonly int _targetPort;
    private readonly string _targetUsername;
    private readonly AuthenticationMethod _targetAuthMethod;
    private readonly SshConnection? _bastion;

    private SshClient? _client;
    private ForwardedPortLocal? _bastionForward;
    private ConnectionInfo? _liveConnectionInfo;
    private ConnectionState _state = ConnectionState.Disconnected;
    private bool _disposed;

    private SshConnection(
        string targetHost,
        int targetPort,
        string targetUsername,
        AuthenticationMethod targetAuthMethod,
        TimeSpan keepAliveInterval,
        SshConnection? bastion = null)
    {
        _targetHost = targetHost;
        _targetPort = targetPort;
        _targetUsername = targetUsername;
        _targetAuthMethod = targetAuthMethod;
        KeepAliveInterval = keepAliveInterval;
        _bastion = bastion;
    }

    /// <summary>Hôte cible (DNS ou IP) tel que vu côté logique. Pour une connexion via bastion, <i>pas</i> l'adresse intermédiaire 127.0.0.1.</summary>
    public string Host => _targetHost;

    /// <summary>Port TCP cible côté logique (ex: 22 même si la session passe par un forward local sur 127.0.0.1).</summary>
    public int Port => _targetPort;

    /// <summary>Identifiant utilisateur sur la cible.</summary>
    public string Username => _targetUsername;

    /// <summary>Intervalle de keep-alive appliqué au <see cref="SshClient"/> sous-jacent.</summary>
    public TimeSpan KeepAliveInterval { get; }

    /// <summary><c>true</c> si la connexion SSH est ouverte et opérationnelle.</summary>
    public bool IsConnected => _client?.IsConnected ?? false;

    /// <summary><c>true</c> si la connexion route à travers un bastion (jump host).</summary>
    public bool UsesJumpHost => _bastion is not null;

    /// <summary>Émis à chaque transition de l'état de la connexion.</summary>
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>Crée une connexion authentifiée par mot de passe.</summary>
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

        AuthenticationMethod auth = new PasswordAuthenticationMethod(username, password);
        return new SshConnection(
            host,
            port,
            username,
            auth,
            keepAliveInterval ?? DefaultKeepAliveInterval);
    }

    /// <summary>
    /// Crée une connexion authentifiée par clé privée (Ed25519, RSA, ECDSA).
    /// La clé est fournie en bytes au format OpenSSH ou PEM, déjà déchiffrée
    /// du keystore Harbor.
    /// </summary>
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

        AuthenticationMethod auth = BuildKeyAuthMethod(username, privateKeyMaterial, passphrase);
        return new SshConnection(
            host,
            port,
            username,
            auth,
            keepAliveInterval ?? DefaultKeepAliveInterval);
    }

    /// <summary>
    /// Crée une connexion routée à travers un bastion unique (équivalent
    /// <c>ssh -J bastion target</c> en OpenSSH).
    /// </summary>
    /// <param name="bastion">Endpoint du bastion (host, port, username).</param>
    /// <param name="bastionAuth">Auth pour s'authentifier sur le bastion.</param>
    /// <param name="target">Endpoint final, accessible depuis le bastion.</param>
    /// <param name="targetAuth">Auth pour s'authentifier sur la cible.</param>
    /// <param name="keepAliveInterval">Keep-alive appliqué aux DEUX connexions (bastion et cible).</param>
    /// <remarks>
    /// Implémentation : ouvre la connexion bastion, démarre un
    /// <see cref="ForwardedPortLocal"/> 127.0.0.1:auto → cible:port, puis
    /// connecte la cible à travers ce forward. Les bastions chaînés
    /// multi-niveaux sont prévus en Phase 8.3.
    /// </remarks>
    public static SshConnection WithJumpHost(
        SshEndpoint bastion,
        SshAuthProvider bastionAuth,
        SshEndpoint target,
        SshAuthProvider targetAuth,
        TimeSpan? keepAliveInterval = null)
    {
        ArgumentNullException.ThrowIfNull(bastion);
        ArgumentNullException.ThrowIfNull(bastionAuth);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetAuth);
        ArgumentException.ThrowIfNullOrEmpty(bastion.Host);
        ArgumentException.ThrowIfNullOrEmpty(bastion.Username);
        ArgumentException.ThrowIfNullOrEmpty(target.Host);
        ArgumentException.ThrowIfNullOrEmpty(target.Username);
        ValidatePort(bastion.Port);
        ValidatePort(target.Port);

        TimeSpan keepAlive = keepAliveInterval ?? DefaultKeepAliveInterval;

        SshConnection bastionConn = BuildFromAuth(bastion, bastionAuth, keepAlive);
        AuthenticationMethod targetAuthMethod = BuildAuthMethod(target.Username, targetAuth);

        return new SshConnection(
            target.Host,
            target.Port,
            target.Username,
            targetAuthMethod,
            keepAlive,
            bastionConn);
    }

    /// <summary>
    /// Établit la connexion SSH. Pour une connexion bastion, ouvre d'abord le
    /// bastion, monte un forward local vers la cible, puis connecte la cible
    /// via ce forward. Idempotent.
    /// </summary>
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
            string actualHost = _targetHost;
            int actualPort = _targetPort;

            if (_bastion is not null)
            {
                await _bastion.ConnectAsync(ct).ConfigureAwait(false);

                int localPort = PickFreeLocalTcpPort();
                _bastionForward = new ForwardedPortLocal(
                    "127.0.0.1",
                    (uint)localPort,
                    _targetHost,
                    (uint)_targetPort);
                _bastion.GetClient().AddForwardedPort(_bastionForward);
                _bastionForward.Start();

                actualHost = "127.0.0.1";
                actualPort = localPort;
            }

            _liveConnectionInfo = new ConnectionInfo(actualHost, actualPort, _targetUsername, _targetAuthMethod)
            {
                RetryAttempts = 0,
            };
            _client = new SshClient(_liveConnectionInfo)
            {
                KeepAliveInterval = KeepAliveInterval,
            };
            await _client.ConnectAsync(ct).ConfigureAwait(false);
            TransitionTo(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            TransitionTo(ConnectionState.Failed, ex.Message);
            await CleanupOnFailureAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Ferme proprement la connexion (et le bastion s'il y en a un).</summary>
    public async Task DisconnectAsync()
    {
        if (_client is null && _bastionForward is null && _bastion?.IsConnected != true)
        {
            return;
        }

        bool wasConnected = IsConnected;
        if (wasConnected)
        {
            TransitionTo(ConnectionState.Disconnecting);
        }

        if (_client?.IsConnected == true)
        {
            _client.Disconnect();
        }

        if (_bastionForward is not null && _bastionForward.IsStarted)
        {
            _bastionForward.Stop();
        }

        if (_bastion is not null)
        {
            await _bastion.DisconnectAsync().ConfigureAwait(false);
        }

        TransitionTo(ConnectionState.Disconnected);
    }

    /// <summary>Accès interne au client SSH sous-jacent.</summary>
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
    /// Accès interne aux paramètres de connexion live (incluant le routage
    /// 127.0.0.1:port pour les bastions). Disponible uniquement après
    /// <see cref="ConnectAsync"/>.
    /// </summary>
    internal ConnectionInfo GetConnectionInfo()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _liveConnectionInfo
            ?? throw new InvalidOperationException(
                $"La connexion n'a pas encore été ouverte. Appelez {nameof(ConnectAsync)} d'abord.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisconnectAsync().ConfigureAwait(false);
        _bastionForward?.Dispose();
        _bastionForward = null;
        _client?.Dispose();
        _client = null;
        if (_bastion is not null)
        {
            await _bastion.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }

    private async Task CleanupOnFailureAsync()
    {
        if (_bastionForward is not null)
        {
            if (_bastionForward.IsStarted)
            {
                _bastionForward.Stop();
            }

            _bastionForward.Dispose();
            _bastionForward = null;
        }

        _client?.Dispose();
        _client = null;

        if (_bastion is not null)
        {
            await _bastion.DisconnectAsync().ConfigureAwait(false);
        }
    }

    private static AuthenticationMethod BuildAuthMethod(string username, SshAuthProvider provider)
    {
        return provider switch
        {
            SshPasswordAuth p => new PasswordAuthenticationMethod(username, p.Password),
            SshKeyAuth k => BuildKeyAuthMethod(username, k.KeyMaterial, k.Passphrase),
            _ => throw new ArgumentException(
                $"Type d'authentification SSH non supporté : {provider.GetType().Name}.",
                nameof(provider)),
        };
    }

    private static PrivateKeyAuthenticationMethod BuildKeyAuthMethod(string username, byte[] keyMaterial, string? passphrase)
    {
        if (keyMaterial.Length == 0)
        {
            throw new ArgumentException("Le matériau de clé privée ne peut pas être vide.", nameof(keyMaterial));
        }

        using MemoryStream keyStream = new(keyMaterial);
        PrivateKeyFile keyFile = string.IsNullOrEmpty(passphrase)
            ? new PrivateKeyFile(keyStream)
            : new PrivateKeyFile(keyStream, passphrase);

        return new PrivateKeyAuthenticationMethod(username, keyFile);
    }

    private static SshConnection BuildFromAuth(
        SshEndpoint endpoint,
        SshAuthProvider auth,
        TimeSpan keepAlive)
    {
        return auth switch
        {
            SshPasswordAuth p => WithPassword(endpoint.Host, endpoint.Port, endpoint.Username, p.Password, keepAlive),
            SshKeyAuth k => WithPrivateKey(endpoint.Host, endpoint.Port, endpoint.Username, k.KeyMaterial, k.Passphrase, keepAlive),
            _ => throw new ArgumentException(
                $"Type d'authentification SSH non supporté : {auth.GetType().Name}.",
                nameof(auth)),
        };
    }

    private static int PickFreeLocalTcpPort()
    {
        // Demande à l'OS d'allouer un port libre via TcpListener(0).
        // Le port est libéré juste après — toléré dans 99 % des cas (race condition
        // théorique mais extrêmement improbable sur localhost en pratique).
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
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
