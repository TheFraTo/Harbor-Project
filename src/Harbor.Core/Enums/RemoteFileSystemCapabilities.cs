namespace Harbor.Core.Enums;

/// <summary>
/// Capacités d'un <c>IRemoteFileSystem</c>. Chaque protocole déclare ses
/// capacités via <c>IRemoteFileSystem.Capabilities</c> ; l'UI adapte ce
/// qu'elle affiche (menus contextuels, colonnes) en fonction.
/// </summary>
/// <remarks>
/// Exemple : S3 ne supporte pas les permissions Unix ni les liens
/// symboliques, donc un provider S3 renverra <c>None</c> pour les deux.
/// </remarks>
[Flags]
public enum RemoteFileSystemCapabilities
{
    /// <summary>Aucune capacité optionnelle.</summary>
    None = 0,

    /// <summary>
    /// Le système de fichiers expose des permissions Unix (owner/group/mode).
    /// Active le menu <c>chmod</c> graphique dans l'UI.
    /// </summary>
    UnixPermissions = 1 << 0,

    /// <summary>Supporte les liens symboliques (création, résolution).</summary>
    Symlinks = 1 << 1,

    /// <summary>Supporte les liens en dur (hard links).</summary>
    HardLinks = 1 << 2,

    /// <summary>Supporte les attributs étendus (xattr).</summary>
    ExtendedAttributes = 1 << 3,

    /// <summary>
    /// Le renommage est atomique (POSIX rename). Sans cette capacité,
    /// les opérations de "move" peuvent être implémentées en copy+delete.
    /// </summary>
    AtomicRename = 1 << 4,

    /// <summary>Supporte la surveillance (watch) des changements en temps réel.</summary>
    Watch = 1 << 5,

    /// <summary>Supporte la lecture d'une portion d'un fichier (range requests, offset).</summary>
    PartialReads = 1 << 6,

    /// <summary>Supporte l'écriture à un offset précis, sans réécrire tout le fichier.</summary>
    PartialWrites = 1 << 7,
}
