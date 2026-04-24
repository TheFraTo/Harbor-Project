using Harbor.Core.Common;
using Harbor.Core.Events;

namespace Harbor.Core.Abstractions;

/// <summary>
/// Contrat pour un shell distant. Supporte l'exécution non-interactive
/// (<see cref="ExecuteAsync"/>), les sessions interactives avec PTY
/// (<see cref="StartInteractiveSessionAsync"/>) et le port forwarding SSH.
/// </summary>
public interface IRemoteShell : IAsyncDisposable
{
    /// <summary><c>true</c> si la connexion est établie.</summary>
    bool IsConnected { get; }

    /// <summary>Émis à chaque transition d'état de la connexion.</summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>Établit la connexion au shell distant.</summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>Ferme proprement la connexion et toutes les sessions ouvertes.</summary>
    Task DisconnectAsync();

    /// <summary>
    /// Exécute une commande non-interactive et retourne son code de sortie.
    /// Les flux <paramref name="stdout"/> et <paramref name="stderr"/> reçoivent
    /// les sorties en streaming ; s'ils sont <c>null</c>, les sorties sont ignorées.
    /// </summary>
    Task<int> ExecuteAsync(
        string command,
        Stream? stdout = null,
        Stream? stderr = null,
        CancellationToken ct = default);

    /// <summary>
    /// Démarre une session interactive avec un PTY. Utilisé par le terminal
    /// intégré et par tout plugin nécessitant une session shell.
    /// </summary>
    Task<IInteractiveSession> StartInteractiveSessionAsync(
        TerminalSize size,
        CancellationToken ct = default);

    /// <summary>Crée un tunnel <c>ssh -L localPort:remoteHost:remotePort</c>.</summary>
    Task<IPortForward> CreateLocalForwardAsync(
        int localPort,
        string remoteHost,
        int remotePort,
        CancellationToken ct = default);

    /// <summary>Crée un tunnel <c>ssh -R remotePort:localHost:localPort</c>.</summary>
    Task<IPortForward> CreateRemoteForwardAsync(
        int remotePort,
        string localHost,
        int localPort,
        CancellationToken ct = default);

    /// <summary>Crée un tunnel SOCKS dynamique <c>ssh -D localPort</c>.</summary>
    Task<IPortForward> CreateDynamicForwardAsync(
        int localPort,
        CancellationToken ct = default);
}
