namespace Harbor.Core.Enums;

/// <summary>
/// Identifie le type de protocole utilisé par un profil de connexion Harbor.
/// Chaque valeur correspond à une implémentation concrète dans un projet
/// <c>Harbor.Protocols.*</c>.
/// </summary>
public enum ProtocolKind
{
    /// <summary>Shell SSH distant (Renci.SshNet). Inclut SFTP et SCP.</summary>
    Ssh,

    /// <summary>Transfert de fichiers SFTP (via SSH).</summary>
    Sftp,

    /// <summary>Transfert de fichiers SCP (via SSH).</summary>
    Scp,

    /// <summary>FTP en clair (non chiffré).</summary>
    Ftp,

    /// <summary>FTP sur TLS (explicite ou implicite).</summary>
    Ftps,

    /// <summary>WebDAV sur HTTP ou HTTPS.</summary>
    WebDav,

    /// <summary>AWS S3 et stockages compatibles (MinIO, Backblaze B2, Wasabi, R2, Scaleway, Hetzner).</summary>
    S3,

    /// <summary>Azure Blob Storage.</summary>
    AzureBlob,

    /// <summary>Google Cloud Storage.</summary>
    GoogleCloudStorage,

    /// <summary>API Docker Engine (local ou distant).</summary>
    Docker,

    /// <summary>Cluster Kubernetes (kubectl exec, cp vers pods).</summary>
    Kubernetes,

    /// <summary>Telnet (legacy, pour matériel réseau uniquement).</summary>
    Telnet,

    /// <summary>Port série (USB serial, pour IoT et routeurs).</summary>
    SerialPort,

    /// <summary>Mosh (shell SSH résilient sur UDP).</summary>
    Mosh,
}
