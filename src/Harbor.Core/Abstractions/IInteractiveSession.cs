using Harbor.Core.Common;
using Harbor.Core.Events;

namespace Harbor.Core.Abstractions;

/// <summary>
/// Une session shell interactive avec PTY, produite par
/// <see cref="IRemoteShell.StartInteractiveSessionAsync"/>.
/// </summary>
/// <remarks>
/// La session expose deux flux :
/// <list type="bullet">
///   <item><see cref="Input"/> : en écriture, alimente le stdin du shell distant.</item>
///   <item><see cref="Output"/> : en lecture, contient stdout et stderr mélangés (comportement standard d'un PTY).</item>
/// </list>
/// La fermeture du <c>Input</c> envoie un EOF ; <see cref="IAsyncDisposable.DisposeAsync"/>
/// termine la session et libère les ressources PTY.
/// </remarks>
public interface IInteractiveSession : IAsyncDisposable
{
    /// <summary>Flux d'écriture vers le stdin du shell distant.</summary>
    Stream Input { get; }

    /// <summary>Flux de lecture combinant stdout et stderr (merge par le PTY).</summary>
    Stream Output { get; }

    /// <summary>Code de sortie du processus, ou <c>null</c> tant que la session est active.</summary>
    int? ExitCode { get; }

    /// <summary>Émis quand le processus distant se termine.</summary>
    event EventHandler<SessionExitedEventArgs>? Exited;

    /// <summary>Propage un redimensionnement du terminal (SIGWINCH).</summary>
    Task ResizeAsync(TerminalSize size, CancellationToken ct = default);

    /// <summary>Attend la fin du processus distant et retourne son code de sortie.</summary>
    Task<int> WaitForExitAsync(CancellationToken ct = default);
}
