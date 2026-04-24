namespace Harbor.Core.Enums;

/// <summary>
/// État courant d'une connexion distante (<c>IRemoteFileSystem</c> ou <c>IRemoteShell</c>).
/// Les transitions d'état sont notifiées via l'événement <c>ConnectionStateChanged</c>.
/// </summary>
public enum ConnectionState
{
    /// <summary>La connexion est inactive (état initial et après <c>DisconnectAsync</c>).</summary>
    Disconnected,

    /// <summary>La connexion est en cours d'établissement (résolution DNS, handshake, auth).</summary>
    Connecting,

    /// <summary>La connexion est établie et prête à servir des opérations.</summary>
    Connected,

    /// <summary>La connexion est en train de se fermer proprement.</summary>
    Disconnecting,

    /// <summary>La connexion a échoué ou a été coupée de manière inattendue.</summary>
    Failed,

    /// <summary>Tentative de reconnexion automatique après une coupure (keep-alive, reprise).</summary>
    Reconnecting,
}
