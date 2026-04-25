using System.Security.Cryptography;
using System.Text;
using Harbor.Core.Common;

namespace Harbor.Security.Crypto;

/// <summary>
/// Chiffrement symétrique authentifié AES-256-GCM, utilisé par le keystore
/// pour sceller chaque secret individuellement (mots de passe, clés privées,
/// passphrases, tokens).
/// </summary>
/// <remarks>
/// <para>
/// Chaque appel à <see cref="EncryptString"/> ou <see cref="EncryptBytes"/>
/// génère un nonce aléatoire de 12 octets (taille standard GCM). Le tag
/// d'authentification fait 16 octets (128 bits).
/// </para>
/// <para>
/// La clé fournie doit faire 32 octets exactement (AES-256). Elle est
/// typiquement la master key dérivée du master password via <see cref="KeyDerivation"/>.
/// </para>
/// </remarks>
public static class SymmetricCrypto
{
    /// <summary>Taille du nonce : 12 octets (96 bits) — recommandé par le NIST pour GCM.</summary>
    public const int NonceLength = 12;

    /// <summary>Taille du tag d'authentification : 16 octets (128 bits).</summary>
    public const int TagLength = 16;

    /// <summary>
    /// Chiffre une chaîne UTF-8 et renvoie l'enveloppe <see cref="EncryptedString"/>.
    /// Délègue à <see cref="EncryptBytes"/> et zéroïse la copie UTF-8 intermédiaire.
    /// </summary>
    public static EncryptedString EncryptString(string plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        byte[] plain = Encoding.UTF8.GetBytes(plaintext);
        try
        {
            EncryptedBytes raw = EncryptBytes(plain, key);
            return new EncryptedString(raw.Nonce, raw.Ciphertext, raw.Tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    /// <summary>Chiffre un blob binaire et renvoie l'enveloppe <see cref="EncryptedBytes"/>.</summary>
    public static EncryptedBytes EncryptBytes(byte[] plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);

        byte[] nonce = new byte[NonceLength];
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagLength];

        using AesGcm aes = new(key, TagLength);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return new EncryptedBytes(nonce, ciphertext, tag);
    }

    /// <summary>Déchiffre une <see cref="EncryptedString"/> et renvoie la chaîne UTF-8 d'origine.</summary>
    /// <exception cref="CryptographicException">Si le tag d'authentification est invalide (clé incorrecte ou ciphertext altéré).</exception>
    public static string DecryptString(EncryptedString envelope, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        byte[] plain = DecryptToBytes(envelope.Nonce, envelope.Ciphertext, envelope.Tag, key);
        try
        {
            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    /// <summary>Déchiffre une <see cref="EncryptedBytes"/> et renvoie le blob binaire d'origine.</summary>
    public static byte[] DecryptBytes(EncryptedBytes envelope, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return DecryptToBytes(envelope.Nonce, envelope.Ciphertext, envelope.Tag, key);
    }

    private static byte[] DecryptToBytes(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] key)
    {
        ValidateKey(key);
        if (nonce.Length != NonceLength)
        {
            throw new ArgumentException($"Nonce doit faire {NonceLength} octets.", nameof(nonce));
        }

        if (tag.Length != TagLength)
        {
            throw new ArgumentException($"Tag doit faire {TagLength} octets.", nameof(tag));
        }

        byte[] plaintext = new byte[ciphertext.Length];
        using AesGcm aes = new(key, TagLength);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    private static void ValidateKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeyDerivation.KeyLength)
        {
            throw new ArgumentException(
                $"La clé AES-256 doit faire {KeyDerivation.KeyLength} octets, reçu {key.Length}.",
                nameof(key));
        }
    }
}
