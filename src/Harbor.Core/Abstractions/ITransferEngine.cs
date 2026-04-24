using Harbor.Core.Enums;
using Harbor.Core.Events;
using Harbor.Core.Models;

namespace Harbor.Core.Abstractions;

/// <summary>
/// API publique du moteur de transferts Harbor. Expose la gestion de la file
/// d'attente persistante, des opérations pause/resume/cancel/retry et les
/// événements de progression.
/// </summary>
public interface ITransferEngine
{
    /// <summary>Émis quand un transfert démarre (passage de Queued à InProgress).</summary>
    event EventHandler<TransferEventArgs>? TransferStarted;

    /// <summary>Émis périodiquement pendant un transfert pour notifier la progression.</summary>
    event EventHandler<TransferEventArgs>? TransferProgress;

    /// <summary>Émis quand un transfert se termine avec succès.</summary>
    event EventHandler<TransferEventArgs>? TransferCompleted;

    /// <summary>Émis quand un transfert échoue définitivement (après épuisement des retries).</summary>
    event EventHandler<TransferEventArgs>? TransferFailed;

    /// <summary>Démarre le scheduler et reprend les transferts <see cref="TransferStatus.Interrupted"/>.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Arrête le scheduler. Les transferts en cours sont marqués <see cref="TransferStatus.Interrupted"/>.</summary>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>Ajoute un transfert à la file d'attente.</summary>
    /// <returns>Le <see cref="Transfer"/> créé (avec un <c>Id</c> assigné et l'état <see cref="TransferStatus.Queued"/>).</returns>
    Task<Transfer> EnqueueAsync(TransferRequest request, CancellationToken ct = default);

    /// <summary>Suspend temporairement un transfert en cours.</summary>
    Task PauseAsync(Guid transferId, CancellationToken ct = default);

    /// <summary>Reprend un transfert précédemment suspendu ou interrompu.</summary>
    Task ResumeAsync(Guid transferId, CancellationToken ct = default);

    /// <summary>Annule définitivement un transfert.</summary>
    Task CancelAsync(Guid transferId, CancellationToken ct = default);

    /// <summary>Relance un transfert en échec depuis zéro.</summary>
    Task RetryAsync(Guid transferId, CancellationToken ct = default);

    /// <summary>
    /// Énumère les transferts connus, filtrés optionnellement par statut.
    /// Le résultat est une vue ponctuelle — utiliser les événements pour observer les changements.
    /// </summary>
    IAsyncEnumerable<Transfer> GetTransfersAsync(
        TransferStatus? filter = null,
        CancellationToken ct = default);

    /// <summary>Récupère un transfert par son identifiant, ou <c>null</c> s'il n'existe pas.</summary>
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
