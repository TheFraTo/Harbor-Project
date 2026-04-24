using Harbor.Core.Enums;

namespace Harbor.Core.Events;

/// <summary>
/// Données portées par l'événement <c>ConnectionStateChanged</c> d'un
/// <c>IRemoteFileSystem</c> ou <c>IRemoteShell</c>.
/// </summary>
public sealed class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>Initialise une nouvelle instance avec les états passés et actuels.</summary>
    /// <param name="oldState">État précédent.</param>
    /// <param name="newState">Nouvel état.</param>
    /// <param name="error">Message d'erreur en cas de transition vers <see cref="ConnectionState.Failed"/>.</param>
    public ConnectionStateChangedEventArgs(
        ConnectionState oldState,
        ConnectionState newState,
        string? error = null)
    {
        OldState = oldState;
        NewState = newState;
        Error = error;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>État précédent.</summary>
    public ConnectionState OldState { get; }

    /// <summary>Nouvel état.</summary>
    public ConnectionState NewState { get; }

    /// <summary>Message d'erreur, ou <c>null</c> si le changement n'est pas dû à une erreur.</summary>
    public string? Error { get; }

    /// <summary>Instant de la transition (UTC).</summary>
    public DateTimeOffset Timestamp { get; }
}
