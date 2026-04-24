using Harbor.Core.Common;
using Harbor.Core.Enums;

namespace Harbor.Core.Models;

/// <summary>
/// Une clé SSH gérée par le keystore Harbor. La partie privée est chiffrée
/// par AES-256-GCM avec la master key dérivée du master password ; la partie
/// publique est stockée en clair (elle n'est pas secrète).
/// </summary>
/// <param name="Id">Identifiant stable (PK SQLite, référencé par <see cref="KeyAuth.KeyId"/>).</param>
/// <param name="Name">Nom descriptif choisi par l'utilisateur (ex: <c>"Clé principale Hetzner"</c>).</param>
/// <param name="Algorithm">Algorithme cryptographique de la clé.</param>
/// <param name="PrivateKey">Clé privée chiffrée (blob binaire, format OpenSSH en clair avant chiffrement).</param>
/// <param name="PublicKey">Clé publique en clair (format OpenSSH : <c>ssh-ed25519 AAAA...</c>).</param>
/// <param name="Comment">Commentaire OpenSSH associé à la clé publique (souvent <c>user@host</c>).</param>
/// <param name="CreatedAt">Date de création ou d'import dans Harbor.</param>
/// <param name="LastUsedAt">Date de dernière utilisation, ou <c>null</c> si jamais utilisée.</param>
public sealed record SshKey(
    Guid Id,
    string Name,
    KeyAlgorithm Algorithm,
    EncryptedBytes PrivateKey,
    byte[] PublicKey,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt);
