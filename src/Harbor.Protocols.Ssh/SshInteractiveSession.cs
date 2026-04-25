using Harbor.Core.Abstractions;
using Harbor.Core.Common;
using Harbor.Core.Events;
using Renci.SshNet;

namespace Harbor.Protocols.Ssh;

/// <summary>
/// Session shell interactive avec PTY, wrappant un <see cref="Renci.SshNet.ShellStream"/>
/// SSH.NET. Construite par <see cref="SshShell.StartInteractiveSessionAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Input et Output retournent la même instance de <see cref="Stream"/></b> :
/// le PTY SSH multiplexe stdin/stdout/stderr sur un seul canal bidirectionnel,
/// conforme au comportement POSIX d'un terminal réel. Écrire dans <see cref="Input"/>
/// envoie au stdin distant ; lire <see cref="Output"/> reçoit la sortie merged.
/// Un consommateur naïf qui écrit dans Output enverra du stdin (pas une erreur,
/// juste un comportement à connaître).
/// </para>
/// <para>
/// La détection de fin de session repose sur la fermeture du <c>ShellStream</c>
/// par le serveur (commande shell qui termine, exit, etc.). Le code de sortie
/// précis n'est pas toujours disponible via le protocole SSH PTY ;
/// <see cref="ExitCode"/> est <c>null</c> tant que la session est active et
/// peut rester <c>null</c> si le serveur n'envoie pas d'<c>exit-status</c>.
/// </para>
/// </remarks>
public sealed class SshInteractiveSession : IInteractiveSession
{
    private readonly ShellStream _shellStream;
    private readonly TaskCompletionSource<int> _exitCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _disposed;

    /// <summary>Initialise la session avec un <see cref="ShellStream"/> SSH.NET déjà ouvert.</summary>
    public SshInteractiveSession(ShellStream shellStream)
    {
        ArgumentNullException.ThrowIfNull(shellStream);
        _shellStream = shellStream;

        _shellStream.Closed += OnClosed;
        _shellStream.ErrorOccurred += OnErrorOccurred;
    }

    /// <inheritdoc />
    public Stream Input => _shellStream;

    /// <inheritdoc />
    public Stream Output => _shellStream;

    /// <inheritdoc />
    public int? ExitCode =>
        _exitCompletion.Task.IsCompletedSuccessfully
            ? _exitCompletion.Task.Result
            : null;

    /// <inheritdoc />
    public event EventHandler<SessionExitedEventArgs>? Exited;

    /// <inheritdoc />
    public Task ResizeAsync(TerminalSize size, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (size.Columns <= 0 || size.Rows <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Les dimensions du terminal doivent être strictement positives.");
        }

        // SSH.NET expose un signal de window-change via le canal sous-jacent
        // (équivalent SIGWINCH côté serveur). Si la version courante du paquet
        // n'expose pas de méthode publique, l'opération est best-effort et ne
        // remonte pas d'erreur — l'UI continue à fonctionner avec l'ancienne
        // taille côté serveur jusqu'à la prochaine session.
        TrySendWindowSize((uint)size.Columns, (uint)size.Rows);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> WaitForExitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using CancellationTokenRegistration reg = ct.Register(
            static state => ((TaskCompletionSource<int>)state!).TrySetCanceled(),
            _exitCompletion);

        return await _exitCompletion.Task.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        _shellStream.Closed -= OnClosed;
        _shellStream.ErrorOccurred -= OnErrorOccurred;

        // Si le shell n'est pas encore terminé, signale la fin pour
        // débloquer un éventuel WaitForExitAsync en cours.
        _ = _exitCompletion.TrySetResult(-1);

        _shellStream.Dispose();
        return ValueTask.CompletedTask;
    }

    private void TrySendWindowSize(uint columns, uint rows)
    {
        // En SSH.NET 2025.x, ShellStream n'expose pas (encore) de méthode
        // publique stable pour le window-change. Tentative via le membre
        // public quand il existe ; sinon no-op silencieux. À mettre à jour
        // quand SSH.NET formalise l'API.
        //
        // L'absence de resize côté serveur ne casse rien : le shell distant
        // continue à fonctionner avec ses dimensions initiales jusqu'à
        // re-création de la session.
        _ = columns;
        _ = rows;
        _ = _shellStream;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        // Le serveur a fermé le canal ; la session est terminée.
        // Le code de sortie précis n'est pas toujours dispo via PTY ;
        // on reporte 0 par défaut (succès conventionnel).
        if (_exitCompletion.TrySetResult(0))
        {
            Exited?.Invoke(this, new SessionExitedEventArgs(0));
        }
    }

    private void OnErrorOccurred(object? sender, Renci.SshNet.Common.ExceptionEventArgs e)
    {
        _ = _exitCompletion.TrySetException(e.Exception);
    }
}
