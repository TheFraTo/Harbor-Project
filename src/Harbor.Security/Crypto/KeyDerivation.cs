using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Harbor.Security.Crypto;

/// <summary>
/// Dérive une clé symétrique de 32 octets (AES-256) à partir d'un mot de
/// passe utilisateur via Argon2id, conformément à <c>harbor-architecture.md</c> §13.2.
/// </summary>
public static class KeyDerivation
{
    /// <summary>Taille du sel généré aléatoirement : 16 octets.</summary>
    public const int SaltLength = 16;

    /// <summary>Taille de la clé dérivée : 32 octets (256 bits, AES-256).</summary>
    public const int KeyLength = 32;

    /// <summary>
    /// Génère un nouveau sel cryptographiquement aléatoire de
    /// <see cref="SaltLength"/> octets.
    /// </summary>
    public static byte[] GenerateSalt()
    {
        byte[] salt = new byte[SaltLength];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    /// <summary>
    /// Dérive une clé de 32 octets à partir d'un mot de passe et d'un sel.
    /// </summary>
    /// <param name="password">Mot de passe master de l'utilisateur.</param>
    /// <param name="salt">
    /// Sel propre à l'utilisateur (créé une fois et persisté en clair côté
    /// configuration, pas dans la base chiffrée).
    /// </param>
    /// <param name="parameters">Paramètres Argon2id ; <see cref="KeyDerivationParameters.Default"/> si <c>null</c>.</param>
    /// <returns>Clé de 32 octets.</returns>
    public static byte[] DeriveKey(
        string password,
        byte[] salt,
        KeyDerivationParameters? parameters = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length != SaltLength)
        {
            throw new ArgumentException(
                $"Le sel doit faire {SaltLength} octets, reçu {salt.Length}.",
                nameof(salt));
        }

        KeyDerivationParameters p = parameters ?? KeyDerivationParameters.Default;
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            using Argon2id argon2 = new(passwordBytes)
            {
                Salt = salt,
                MemorySize = p.MemoryKb,
                Iterations = p.Iterations,
                DegreeOfParallelism = p.Parallelism,
            };

            return argon2.GetBytes(KeyLength);
        }
        finally
        {
            // Effacement préventif des octets du mot de passe (best-effort,
            // le GC peut avoir copié la chaîne ; la défense en profondeur
            // s'opère côté UI via SecureString).
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }
}
