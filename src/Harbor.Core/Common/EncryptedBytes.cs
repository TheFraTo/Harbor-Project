namespace Harbor.Core.Common;

/// <summary>
/// Bloc binaire chiffré via AES-256-GCM par la couche <c>Harbor.Security</c>.
/// Même structure que <see cref="EncryptedString"/> mais utilisé pour les secrets
/// dont la forme en clair est naturellement binaire (clés privées SSH, tokens
/// binaires, données arbitraires).
/// </summary>
public sealed record EncryptedBytes(byte[] Nonce, byte[] Ciphertext, byte[] Tag);
