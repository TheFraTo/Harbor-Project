using System.Text.Json.Serialization;
using Harbor.Core.Common;

namespace Harbor.Core.Models;

/// <summary>
/// Base abstraite de la méthode d'authentification associée à un profil.
/// Chaque variante encapsule le matériel nécessaire pour prouver l'identité
/// du client auprès du système distant.
/// </summary>
/// <remarks>
/// La sérialisation JSON polymorphique utilise le discriminant <c>$kind</c>.
/// Les secrets (champs <see cref="Common.EncryptedString"/> et
/// <see cref="Common.EncryptedBytes"/>) sont sérialisés en base64 par
/// System.Text.Json — ils ne sont jamais en clair dans le JSON, mais ils
/// restent chiffrés sous-jacent par le keystore Harbor.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(PasswordAuth), "password")]
[JsonDerivedType(typeof(KeyAuth), "key")]
[JsonDerivedType(typeof(AgentAuth), "agent")]
[JsonDerivedType(typeof(FidoAuth), "fido")]
[JsonDerivedType(typeof(AccessKeyAuth), "access-key")]
[JsonDerivedType(typeof(ServiceAccountJsonAuth), "service-account")]
[JsonDerivedType(typeof(ConnectionStringAuth), "connection-string")]
[JsonDerivedType(typeof(BearerTokenAuth), "bearer-token")]
[JsonDerivedType(typeof(AnonymousAuth), "anonymous")]
public abstract record AuthenticationMethod;

/// <summary>Authentification par mot de passe.</summary>
/// <param name="Password">Mot de passe chiffré via le keystore Harbor.</param>
public sealed record PasswordAuth(EncryptedString Password) : AuthenticationMethod;

/// <summary>Authentification par clé SSH gérée dans le keystore.</summary>
/// <param name="KeyId">Identifiant de la clé dans le keystore (cf. <see cref="SshKey"/>).</param>
/// <param name="Passphrase">Passphrase chiffrée protégeant la clé privée, ou <c>null</c> si la clé n'en a pas.</param>
public sealed record KeyAuth(Guid KeyId, EncryptedString? Passphrase) : AuthenticationMethod;

/// <summary>
/// Authentification déléguée à un agent SSH système (<c>ssh-agent</c>,
/// Pageant, Windows OpenSSH Agent). Aucun secret géré par Harbor.
/// </summary>
public sealed record AgentAuth : AuthenticationMethod;

/// <summary>
/// Authentification via un token matériel FIDO2 / Yubikey (<c>ed25519-sk</c>
/// ou <c>ecdsa-sk</c>). Le toucher du token est requis à chaque connexion.
/// </summary>
/// <param name="KeyHandle">Handle de la clé résidente, ou <c>null</c> pour une clé non-résidente.</param>
public sealed record FidoAuth(string? KeyHandle) : AuthenticationMethod;

/// <summary>
/// Authentification par access key (S3, S3-compatibles, et certains
/// endpoints Azure / divers).
/// </summary>
/// <param name="AccessKeyId">Identifiant public de la clé d'accès.</param>
/// <param name="SecretAccessKey">Secret chiffré.</param>
public sealed record AccessKeyAuth(
    string AccessKeyId,
    EncryptedString SecretAccessKey) : AuthenticationMethod;

/// <summary>Authentification par fichier de compte de service JSON (Google Cloud).</summary>
/// <param name="ServiceAccountJson">Contenu JSON du service account, chiffré.</param>
public sealed record ServiceAccountJsonAuth(
    EncryptedBytes ServiceAccountJson) : AuthenticationMethod;

/// <summary>Authentification par connection string (Azure Blob Storage typiquement).</summary>
/// <param name="ConnectionString">Chaîne de connexion chiffrée.</param>
public sealed record ConnectionStringAuth(
    EncryptedString ConnectionString) : AuthenticationMethod;

/// <summary>Authentification par token Bearer (WebDAV, HTTP APIs).</summary>
/// <param name="Token">Token chiffré.</param>
public sealed record BearerTokenAuth(EncryptedString Token) : AuthenticationMethod;

/// <summary>Aucune authentification (ressources publiques en lecture seule).</summary>
public sealed record AnonymousAuth : AuthenticationMethod;
