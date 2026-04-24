namespace Harbor.Core.Events;

/// <summary>
/// Données portées par l'événement <c>Exited</c> d'un <c>IInteractiveSession</c>.
/// </summary>
public sealed class SessionExitedEventArgs : EventArgs
{
    /// <summary>Initialise une nouvelle instance avec le code de sortie et, si applicable, le signal.</summary>
    /// <param name="exitCode">Code de retour du processus.</param>
    /// <param name="signalName">Nom du signal POSIX qui a terminé le processus (ex: <c>SIGTERM</c>), ou <c>null</c>.</param>
    public SessionExitedEventArgs(int exitCode, string? signalName = null)
    {
        ExitCode = exitCode;
        SignalName = signalName;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>Code de retour du processus (0 = succès conventionnel).</summary>
    public int ExitCode { get; }

    /// <summary>Signal qui a terminé le processus, ou <c>null</c> si terminaison normale.</summary>
    public string? SignalName { get; }

    /// <summary>Instant de la fin de session (UTC).</summary>
    public DateTimeOffset Timestamp { get; }
}
