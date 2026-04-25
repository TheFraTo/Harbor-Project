using Harbor.Core.Enums;
using Harbor.Core.Models;
using Harbor.Data.Repositories;

namespace Harbor.Security.Audit;

/// <summary>
/// Façade de logging d'audit. Encapsule l'écriture vers
/// <see cref="AuditLogRepository"/> avec des helpers typés pour les
/// événements les plus courants.
/// </summary>
/// <remarks>
/// Toutes les méthodes utilisent l'horodatage UTC du <see cref="TimeProvider"/>
/// fourni à la construction. Les écritures sont append-only ; aucune
/// modification rétroactive n'est possible côté API.
/// </remarks>
public sealed class AuditLogger
{
    private readonly AuditLogRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initialise le logger avec un repo et une source de temps.</summary>
    public AuditLogger(AuditLogRepository repository, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Enregistre une connexion réussie pour un profil.</summary>
    public Task LogConnectionOpenedAsync(Guid profileId, string description, CancellationToken ct = default) =>
        WriteAsync(AuditEventType.ConnectionOpened, profileId, description, metadataJson: null, ct);

    /// <summary>Enregistre la fermeture d'une connexion.</summary>
    public Task LogConnectionClosedAsync(Guid profileId, string description, CancellationToken ct = default) =>
        WriteAsync(AuditEventType.ConnectionClosed, profileId, description, metadataJson: null, ct);

    /// <summary>Enregistre un échec de connexion (auth, réseau, timeout).</summary>
    public Task LogConnectionFailedAsync(
        Guid profileId,
        string description,
        string? metadataJson = null,
        CancellationToken ct = default) =>
        WriteAsync(AuditEventType.ConnectionFailed, profileId, description, metadataJson, ct);

    /// <summary>Enregistre une lecture de secret.</summary>
    public Task LogSecretReadAsync(Guid? profileId, string description, CancellationToken ct = default) =>
        WriteAsync(AuditEventType.SecretRead, profileId, description, metadataJson: null, ct);

    /// <summary>Enregistre une écriture de secret.</summary>
    public Task LogSecretWrittenAsync(Guid? profileId, string description, CancellationToken ct = default) =>
        WriteAsync(AuditEventType.SecretWritten, profileId, description, metadataJson: null, ct);

    /// <summary>Enregistre un verrouillage automatique du keystore pour inactivité.</summary>
    public Task LogLockedByInactivityAsync(string description, CancellationToken ct = default) =>
        WriteAsync(AuditEventType.LockedByInactivity, profileId: null, description, metadataJson: null, ct);

    /// <summary>Enregistre un déverrouillage du keystore.</summary>
    public Task LogUnlockedAsync(string description, CancellationToken ct = default) =>
        WriteAsync(AuditEventType.Unlocked, profileId: null, description, metadataJson: null, ct);

    /// <summary>
    /// Enregistre un événement d'audit arbitraire. Préférer les helpers typés
    /// quand un type d'événement existe déjà.
    /// </summary>
    public Task LogAsync(
        AuditEventType type,
        Guid? profileId,
        string description,
        string? metadataJson = null,
        CancellationToken ct = default) =>
        WriteAsync(type, profileId, description, metadataJson, ct);

    private Task WriteAsync(
        AuditEventType type,
        Guid? profileId,
        string description,
        string? metadataJson,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(description);

        AuditLogEntry entry = new(
            Id: Guid.NewGuid(),
            Timestamp: _timeProvider.GetUtcNow(),
            Type: type,
            ProfileId: profileId,
            Description: description,
            MetadataJson: metadataJson);

        return _repository.InsertAsync(entry, ct);
    }
}
