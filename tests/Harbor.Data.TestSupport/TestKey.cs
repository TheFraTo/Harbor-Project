using System.Security.Cryptography;
using System.Text;

namespace Harbor.Data.TestSupport;

/// <summary>
/// Helpers de génération de clés AES-256 destinés exclusivement aux tests
/// d'intégration de Harbor.Data. <b>Ne pas utiliser en production</b> :
/// la dérivation se fait par SHA-256 (raccourci pour les tests), pas par
/// Argon2id comme exigé pour les vrais déploiements.
/// </summary>
public static class TestKey
{
    /// <summary>
    /// Convertit une chaîne en clé déterministe de 32 octets pour SQLCipher.
    /// Le test peut utiliser n'importe quel mot de passe ; la clé sera la même
    /// d'un run à l'autre, ce qui simplifie les scénarios de réouverture.
    /// </summary>
    /// <param name="passphrase">Phrase libre.</param>
    /// <returns>Clé de 32 octets (AES-256) reproductible.</returns>
    public static byte[] Derive(string passphrase)
    {
        ArgumentNullException.ThrowIfNull(passphrase);
        byte[] input = Encoding.UTF8.GetBytes(passphrase);
        return SHA256.HashData(input);
    }
}
