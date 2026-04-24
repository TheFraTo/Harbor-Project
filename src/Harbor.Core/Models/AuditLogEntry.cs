using Harbor.Core.Enums;

namespace Harbor.Core.Models;

/// <summary>
/// Une entrée du journal d'audit local Harbor. Toutes les actions sensibles
/// (connexions, accès aux secrets, modifications de profils, imports/exports,
/// verrouillages) y sont enregistrées pour assurer la traçabilité utilisateur.
/// </summary>
/// <param name="Id">Identifiant stable (PK SQLite).</param>
/// <param name="Timestamp">Horodatage précis de l'événement (UTC).</param>
/// <param name="Type">Catégorie de l'événement.</param>
/// <param name="ProfileId">Profil associé, ou <c>null</c> si l'événement ne cible pas de profil (ex: verrouillage).</param>
/// <param name="Description">Description courte et lisible par l'utilisateur.</param>
/// <param name="MetadataJson">
/// Métadonnées additionnelles sérialisées en JSON, ou <c>null</c>.
/// Les secrets n'apparaissent jamais ici — uniquement des détails contextuels
/// (nom d'hôte, chemins, codes d'erreur, etc.).
/// </param>
public sealed record AuditLogEntry(
    Guid Id,
    DateTimeOffset Timestamp,
    AuditEventType Type,
    Guid? ProfileId,
    string Description,
    string? MetadataJson);
