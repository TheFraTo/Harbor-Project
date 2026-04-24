namespace Harbor.Core.Abstractions;

/// <summary>
/// Extension de <see cref="IRemoteFileSystem"/> avec les opérations spécifiques
/// aux stockages objets (S3 et compatibles, Azure Blob, Google Cloud Storage).
/// </summary>
public interface ICloudStorage : IRemoteFileSystem
{
    /// <summary>Liste les buckets/containers disponibles pour le compte courant.</summary>
    Task<IReadOnlyList<string>> ListBucketsAsync(CancellationToken ct = default);

    /// <summary>
    /// Génère une URL présignée permettant à un tiers d'accéder à l'objet
    /// sans authentification, pendant la durée <paramref name="expiresIn"/>.
    /// </summary>
    /// <param name="path">Clé de l'objet dans le bucket.</param>
    /// <param name="expiresIn">Durée de validité.</param>
    /// <param name="isUpload"><c>true</c> pour une URL PUT (upload), <c>false</c> pour GET (download).</param>
    /// <param name="ct">Jeton d'annulation.</param>
    Task<Uri> GetPresignedUrlAsync(
        string path,
        TimeSpan expiresIn,
        bool isUpload,
        CancellationToken ct = default);

    /// <summary>
    /// Récupère les métadonnées arbitraires (en-têtes <c>x-amz-meta-*</c>,
    /// métadonnées Azure, labels GCS) associées à un objet.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetObjectMetadataAsync(
        string path,
        CancellationToken ct = default);

    /// <summary>
    /// Modifie l'accès public (ACL) d'un objet : <c>true</c> pour rendre public
    /// en lecture, <c>false</c> pour privé.
    /// </summary>
    Task SetPublicAccessAsync(
        string path,
        bool isPublic,
        CancellationToken ct = default);
}
