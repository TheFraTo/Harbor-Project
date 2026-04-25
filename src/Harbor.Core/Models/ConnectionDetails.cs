using System.Text.Json.Serialization;
using Harbor.Core.Common;

namespace Harbor.Core.Models;

/// <summary>
/// Base abstraite des détails de connexion d'un profil. Chaque protocole
/// supporté par Harbor dérive un type concret avec les champs qui lui sont
/// propres (host/port pour SSH, endpoint/region/bucket pour S3, etc.).
/// </summary>
/// <remarks>
/// La sérialisation JSON polymorphique utilise le discriminant <c>$kind</c>
/// avec les valeurs déclarées par les attributs <see cref="JsonDerivedTypeAttribute"/>.
/// Ce contrat est stable et fait partie du format de persistance — toute
/// modification doit faire l'objet d'une migration.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(SshConnectionDetails), "ssh")]
[JsonDerivedType(typeof(FtpConnectionDetails), "ftp")]
[JsonDerivedType(typeof(S3ConnectionDetails), "s3")]
[JsonDerivedType(typeof(AzureBlobConnectionDetails), "azure-blob")]
[JsonDerivedType(typeof(GoogleCloudStorageConnectionDetails), "gcs")]
[JsonDerivedType(typeof(WebDavConnectionDetails), "webdav")]
[JsonDerivedType(typeof(DockerConnectionDetails), "docker")]
[JsonDerivedType(typeof(KubernetesConnectionDetails), "k8s")]
[JsonDerivedType(typeof(TelnetConnectionDetails), "telnet")]
[JsonDerivedType(typeof(SerialPortConnectionDetails), "serial")]
[JsonDerivedType(typeof(MoshConnectionDetails), "mosh")]
public abstract record ConnectionDetails;

/// <summary>Détails pour une connexion SSH / SFTP / SCP.</summary>
/// <param name="Host">Nom DNS ou IP de la cible.</param>
/// <param name="Port">Port SSH (22 par défaut).</param>
/// <param name="Username">Utilisateur distant.</param>
/// <param name="Jump">Chaîne optionnelle de bastions pour atteindre la cible.</param>
public sealed record SshConnectionDetails(
    string Host,
    int Port,
    string Username,
    JumpHost? Jump) : ConnectionDetails;

/// <summary>Détails pour une connexion FTP en clair ou FTPS (explicite/implicite).</summary>
/// <param name="Host">Nom DNS ou IP du serveur.</param>
/// <param name="Port">Port FTP (21 en clair, 990 implicite, 21 avec STARTTLS explicite).</param>
/// <param name="TlsMode">Mode TLS : None, Explicit (AUTH TLS), Implicit.</param>
public sealed record FtpConnectionDetails(
    string Host,
    int Port,
    FtpTlsMode TlsMode) : ConnectionDetails;

/// <summary>Mode TLS pour une connexion FTP.</summary>
public enum FtpTlsMode
{
    /// <summary>FTP en clair (non chiffré).</summary>
    None,

    /// <summary>FTPS explicite : connexion claire, puis AUTH TLS pour chiffrer.</summary>
    Explicit,

    /// <summary>FTPS implicite : TLS dès l'établissement du socket.</summary>
    Implicit,
}

/// <summary>Détails pour une connexion S3 ou S3-compatible.</summary>
/// <param name="Endpoint">
/// URL de l'endpoint. <c>null</c> pour AWS par défaut ; requis pour MinIO,
/// Backblaze B2, Wasabi, Cloudflare R2, Scaleway, Hetzner.
/// </param>
/// <param name="Region">Région AWS (ex: <c>eu-west-3</c>). Obligatoire.</param>
/// <param name="BucketName">Nom du bucket cible.</param>
/// <param name="UsePathStyle">
/// <c>true</c> pour forcer le style path (<c>https://endpoint/bucket/key</c>),
/// requis par MinIO et certains compatibles. <c>false</c> pour virtual-hosted.
/// </param>
public sealed record S3ConnectionDetails(
    string? Endpoint,
    string Region,
    string BucketName,
    bool UsePathStyle) : ConnectionDetails;

/// <summary>Détails pour une connexion Azure Blob Storage.</summary>
/// <param name="AccountName">Nom du compte de stockage Azure.</param>
/// <param name="ContainerName">Nom du container ciblé.</param>
/// <param name="EndpointSuffix">Suffixe d'endpoint (<c>core.windows.net</c> par défaut, différent pour Azure Gov/China).</param>
public sealed record AzureBlobConnectionDetails(
    string AccountName,
    string ContainerName,
    string EndpointSuffix) : ConnectionDetails;

/// <summary>Détails pour une connexion Google Cloud Storage.</summary>
/// <param name="ProjectId">Identifiant du projet GCP.</param>
/// <param name="BucketName">Nom du bucket.</param>
public sealed record GoogleCloudStorageConnectionDetails(
    string ProjectId,
    string BucketName) : ConnectionDetails;

/// <summary>Détails pour une connexion WebDAV (HTTP ou HTTPS).</summary>
/// <param name="BaseUri">URI racine du partage WebDAV.</param>
public sealed record WebDavConnectionDetails(
    Uri BaseUri) : ConnectionDetails;

/// <summary>Détails pour une connexion à un démon Docker.</summary>
/// <param name="Endpoint">
/// URL du daemon Docker. Exemples :
/// <list type="bullet">
///   <item><c>npipe://./pipe/docker_engine</c> (Windows local)</item>
///   <item><c>unix:///var/run/docker.sock</c> (Linux/macOS local)</item>
///   <item><c>tcp://host:2375</c> (TCP distant non chiffré)</item>
///   <item><c>tcp://host:2376</c> (TCP distant avec TLS)</item>
/// </list>
/// </param>
public sealed record DockerConnectionDetails(
    string Endpoint) : ConnectionDetails;

/// <summary>Détails pour une connexion à un cluster Kubernetes.</summary>
/// <param name="KubeConfigPath">Chemin vers un <c>kubeconfig</c>. <c>null</c> pour utiliser <c>~/.kube/config</c>.</param>
/// <param name="Context">Contexte Kubernetes à utiliser dans le kubeconfig. <c>null</c> pour le contexte courant.</param>
/// <param name="Namespace">Namespace par défaut pour les opérations. <c>null</c> = namespace courant.</param>
public sealed record KubernetesConnectionDetails(
    string? KubeConfigPath,
    string? Context,
    string? Namespace) : ConnectionDetails;

/// <summary>Détails pour une connexion Telnet (legacy, matériel réseau).</summary>
/// <param name="Host">Nom DNS ou IP de la cible.</param>
/// <param name="Port">Port Telnet (23 par défaut).</param>
public sealed record TelnetConnectionDetails(
    string Host,
    int Port) : ConnectionDetails;

/// <summary>Détails pour une connexion port série (USB serial, IoT).</summary>
/// <param name="PortName">Nom du port (<c>COM3</c> sur Windows, <c>/dev/ttyUSB0</c> sur Linux).</param>
/// <param name="BaudRate">Débit en bauds (9600, 115200, etc.).</param>
/// <param name="DataBits">Bits de données (typ. 8).</param>
/// <param name="Parity">Parité (<c>None</c>, <c>Even</c>, <c>Odd</c>).</param>
/// <param name="StopBits">Bits de stop (<c>One</c>, <c>Two</c>).</param>
public sealed record SerialPortConnectionDetails(
    string PortName,
    int BaudRate,
    int DataBits,
    SerialParity Parity,
    SerialStopBits StopBits) : ConnectionDetails;

/// <summary>Parité d'une connexion port série.</summary>
public enum SerialParity { None, Even, Odd, Mark, Space }

/// <summary>Bits de stop d'une connexion port série.</summary>
public enum SerialStopBits { One, OnePointFive, Two }

/// <summary>Détails pour une connexion Mosh (SSH résilient sur UDP).</summary>
/// <param name="Host">Nom DNS ou IP de la cible.</param>
/// <param name="SshPort">Port SSH utilisé pour la session initiale (22 par défaut).</param>
/// <param name="Username">Utilisateur distant.</param>
/// <param name="UdpPortRange">Plage de ports UDP autorisée (ex: <c>60001:60999</c>).</param>
public sealed record MoshConnectionDetails(
    string Host,
    int SshPort,
    string Username,
    string UdpPortRange) : ConnectionDetails;
