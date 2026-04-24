using Harbor.Core.Enums;
using Harbor.Core.Events;
using Harbor.Core.Models;

namespace Harbor.Core.Abstractions;

/// <summary>
/// Contrat unifié pour tout système de fichiers distant exposé par Harbor :
/// SFTP, FTP/FTPS, WebDAV, S3 (via <see cref="ICloudStorage"/>), Docker volumes,
/// pods Kubernetes, etc.
/// </summary>
/// <remarks>
/// <para>
/// Les implémentations déclarent leurs capacités via <see cref="Capabilities"/>.
/// L'UI adapte dynamiquement les actions proposées en fonction.
/// </para>
/// <para>
/// Toutes les opérations sont asynchrones et supportent l'annulation.
/// La fermeture propre se fait via <see cref="IAsyncDisposable.DisposeAsync"/>
/// ou <see cref="DisconnectAsync"/>.
/// </para>
/// </remarks>
public interface IRemoteFileSystem : IAsyncDisposable
{
    /// <summary>Capacités optionnelles exposées par ce provider.</summary>
    RemoteFileSystemCapabilities Capabilities { get; }

    /// <summary><c>true</c> si la connexion est établie et prête à servir des opérations.</summary>
    bool IsConnected { get; }

    /// <summary>Émis à chaque transition d'état de la connexion.</summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>Établit la connexion au système distant.</summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>Ferme proprement la connexion.</summary>
    Task DisconnectAsync();

    /// <summary>Liste le contenu d'un dossier distant.</summary>
    /// <param name="path">Chemin absolu du dossier.</param>
    /// <param name="ct">Jeton d'annulation.</param>
    /// <returns>Entrées trouvées (fichiers, dossiers, liens).</returns>
    Task<IReadOnlyList<RemoteFile>> ListAsync(string path, CancellationToken ct = default);

    /// <summary>Ouvre un flux en lecture sur un fichier distant.</summary>
    /// <param name="path">Chemin absolu du fichier.</param>
    /// <param name="offset">Offset de départ (utile pour la reprise de téléchargement ; <c>0</c> = début).</param>
    /// <param name="ct">Jeton d'annulation.</param>
    Task<Stream> OpenReadAsync(string path, long offset = 0, CancellationToken ct = default);

    /// <summary>Ouvre un flux en écriture sur un fichier distant.</summary>
    /// <param name="path">Chemin absolu du fichier.</param>
    /// <param name="append"><c>true</c> pour ajouter à la fin d'un fichier existant, <c>false</c> pour écraser ou créer.</param>
    /// <param name="ct">Jeton d'annulation.</param>
    Task<Stream> OpenWriteAsync(string path, bool append = false, CancellationToken ct = default);

    /// <summary>Récupère les métadonnées d'une entrée distante.</summary>
    Task<RemoteFile> StatAsync(string path, CancellationToken ct = default);

    /// <summary>Supprime un fichier ou un dossier (récursivement pour les dossiers non vides).</summary>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>Crée un dossier (parents créés au besoin, selon le protocole).</summary>
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Renomme ou déplace une entrée.</summary>
    Task RenameAsync(string oldPath, string newPath, CancellationToken ct = default);

    /// <summary>
    /// Modifie les permissions Unix d'une entrée. Disponible uniquement si
    /// <see cref="Capabilities"/> contient <see cref="RemoteFileSystemCapabilities.UnixPermissions"/>.
    /// </summary>
    Task SetPermissionsAsync(string path, UnixFileMode mode, CancellationToken ct = default);

    /// <summary>
    /// Observe les changements dans un dossier. Disponible uniquement si
    /// <see cref="Capabilities"/> contient <see cref="RemoteFileSystemCapabilities.Watch"/>.
    /// </summary>
    IAsyncEnumerable<RemoteFile> WatchAsync(string path, CancellationToken ct = default);
}
