namespace Harbor.Core.Common;

/// <summary>
/// Chaîne chiffrée via AES-256-GCM par la couche <c>Harbor.Security</c>.
/// Enveloppe (<c>Nonce</c>, <c>Ciphertext</c>, <c>Tag</c>) au format standard GCM :
/// nonce de 12 octets, tag d'authentification de 16 octets.
/// </summary>
/// <remarks>
/// Le contenu chiffré est une chaîne UTF-8 encodée en octets avant chiffrement.
/// Utilisé pour les mots de passe, passphrases, et tout secret manipulé comme texte.
/// </remarks>
public sealed record EncryptedString(byte[] Nonce, byte[] Ciphertext, byte[] Tag);
