namespace Harbor.Core.Enums;

/// <summary>
/// Direction d'un transfert de fichier géré par le moteur de transferts.
/// </summary>
public enum TransferDirection
{
    /// <summary>Du système de fichiers local vers un système distant.</summary>
    Upload,

    /// <summary>D'un système distant vers le système de fichiers local.</summary>
    Download,

    /// <summary>
    /// Entre deux systèmes distants, sans passer par le disque local
    /// (streaming direct quand le protocole le permet).
    /// </summary>
    ServerToServer,
}
