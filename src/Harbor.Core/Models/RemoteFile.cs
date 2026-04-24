namespace Harbor.Core.Models;

/// <summary>
/// Représente une entrée (fichier, dossier, lien) sur un système de fichiers distant,
/// telle qu'exposée par une implémentation d'<c>IRemoteFileSystem</c>.
/// Les champs optionnels (<see cref="Permissions"/>, <see cref="OwnerName"/>, etc.)
/// sont <c>null</c> pour les protocoles qui ne les supportent pas (ex: S3).
/// </summary>
/// <param name="Name">Nom de base (sans chemin).</param>
/// <param name="FullPath">Chemin complet absolu sur le système distant.</param>
/// <param name="Size">Taille en octets. Pour un dossier, la valeur peut être <c>0</c> ou la taille du nœud selon le protocole.</param>
/// <param name="IsDirectory"><c>true</c> si l'entrée est un dossier, <c>false</c> pour un fichier ou un lien.</param>
/// <param name="IsSymlink"><c>true</c> si l'entrée est un lien symbolique.</param>
/// <param name="LastModified">Date de dernière modification côté serveur, ou <c>null</c> si non disponible.</param>
/// <param name="Permissions">Permissions Unix, ou <c>null</c> si le protocole ne les expose pas.</param>
/// <param name="OwnerName">Nom du propriétaire, ou <c>null</c> si non disponible.</param>
/// <param name="GroupName">Nom du groupe, ou <c>null</c> si non disponible.</param>
/// <param name="SymlinkTarget">Cible du lien symbolique (relatif ou absolu), ou <c>null</c> si ce n'est pas un lien.</param>
public sealed record RemoteFile(
    string Name,
    string FullPath,
    long Size,
    bool IsDirectory,
    bool IsSymlink,
    DateTimeOffset? LastModified,
    UnixFileMode? Permissions,
    string? OwnerName,
    string? GroupName,
    string? SymlinkTarget);
