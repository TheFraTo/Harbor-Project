using Harbor.Core.Enums;

namespace Harbor.Core.Models;

/// <summary>
/// Description d'un transfert à ajouter à la file d'attente du <c>ITransferEngine</c>.
/// Contrairement à <see cref="Transfer"/>, un <c>TransferRequest</c> n'a pas encore d'<c>Id</c>,
/// ni de statut, ni de progression — ces champs seront attribués par le moteur.
/// </summary>
/// <param name="Direction">Sens du transfert.</param>
/// <param name="SourcePath">Chemin source.</param>
/// <param name="DestPath">Chemin destination.</param>
/// <param name="SourceProfileId">Profil source, ou <c>null</c> pour une source locale.</param>
/// <param name="DestProfileId">Profil destination, ou <c>null</c> pour une destination locale.</param>
/// <param name="Priority">Priorité initiale (0 = normal ; valeurs plus élevées = prioritaires).</param>
public sealed record TransferRequest(
    TransferDirection Direction,
    string SourcePath,
    string DestPath,
    Guid? SourceProfileId,
    Guid? DestProfileId,
    int Priority = 0);
