using Harbor.Core.Enums;

namespace Harbor.Core.Models;

/// <summary>
/// Un transfert de fichier connu du moteur de transferts Harbor.
/// L'ensemble des transferts est persisté en SQLite pour survivre aux crashes
/// et permettre la reprise (<see cref="TransferStatus.Interrupted"/>).
/// </summary>
/// <param name="Id">Identifiant stable (PK SQLite).</param>
/// <param name="Direction">Sens du transfert.</param>
/// <param name="SourcePath">Chemin source (local ou distant selon direction).</param>
/// <param name="DestPath">Chemin destination (local ou distant selon direction).</param>
/// <param name="SourceProfileId">Profil source, ou <c>null</c> si la source est locale.</param>
/// <param name="DestProfileId">Profil destination, ou <c>null</c> si la destination est locale.</param>
/// <param name="TotalBytes">Taille totale attendue en octets (<c>-1</c> si inconnue au démarrage).</param>
/// <param name="TransferredBytes">Octets effectivement transférés (maj périodique pendant l'exécution).</param>
/// <param name="Status">État courant du transfert.</param>
/// <param name="ErrorMessage">Dernier message d'erreur, ou <c>null</c> si pas d'erreur.</param>
/// <param name="Priority">Priorité ordonnée (plus haut = traité plus tôt par le scheduler).</param>
/// <param name="CreatedAt">Date de création dans la queue.</param>
/// <param name="CompletedAt">Date de complétion (succès ou échec définitif), <c>null</c> tant qu'en cours.</param>
public sealed record Transfer(
    Guid Id,
    TransferDirection Direction,
    string SourcePath,
    string DestPath,
    Guid? SourceProfileId,
    Guid? DestProfileId,
    long TotalBytes,
    long TransferredBytes,
    TransferStatus Status,
    string? ErrorMessage,
    int Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
