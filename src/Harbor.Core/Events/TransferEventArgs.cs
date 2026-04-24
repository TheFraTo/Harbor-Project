using Harbor.Core.Models;

namespace Harbor.Core.Events;

/// <summary>
/// Données portées par les événements du <c>ITransferEngine</c>
/// (<c>TransferStarted</c>, <c>TransferProgress</c>, <c>TransferCompleted</c>, <c>TransferFailed</c>).
/// </summary>
public sealed class TransferEventArgs : EventArgs
{
    /// <summary>Initialise une nouvelle instance avec le snapshot du transfert.</summary>
    /// <param name="transfer">État courant du transfert au moment de l'événement.</param>
    public TransferEventArgs(Transfer transfer)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        Transfer = transfer;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>Snapshot du transfert (immuable, peut être stocké tel quel).</summary>
    public Transfer Transfer { get; }

    /// <summary>Instant de l'événement (UTC).</summary>
    public DateTimeOffset Timestamp { get; }
}
