using System.Security.Cryptography;
using Harbor.Core.Common;
using Harbor.Security.Crypto;

namespace Harbor.Security.Keystore;

/// <summary>
/// Coffre-fort en mémoire détenant la master key dérivée du master password.
/// Permet de chiffrer et déchiffrer des secrets individuels via AES-256-GCM
/// tant que le coffre est déverrouillé. Verrouille automatiquement après une
/// période d'inactivité configurable.
/// </summary>
/// <remarks>
/// <para>
/// La master key est conservée en RAM dans un buffer aligné, et zéroïsée à la
/// fermeture (<see cref="Lock"/>, <see cref="Dispose"/>) ou à expiration du
/// délai d'inactivité.
/// </para>
/// <para>
/// Le master password lui-même n'est jamais conservé : seule la clé dérivée
/// l'est. La dérivation est lente (Argon2id, ~200 ms), donc une copie unique
/// est faite à <see cref="Unlock"/>.
/// </para>
/// </remarks>
public sealed class Keystore : IDisposable
{
    private readonly TimeSpan _inactivityTimeout;
    private readonly TimeProvider _timeProvider;

    private byte[]? _masterKey;
    private DateTimeOffset _lastActivityUtc;
    private bool _disposed;

    /// <summary>
    /// Initialise un keystore. Le coffre est verrouillé tant que <see cref="Unlock"/>
    /// n'a pas été appelé.
    /// </summary>
    /// <param name="inactivityTimeout">Délai après lequel le coffre se verrouille automatiquement.</param>
    /// <param name="timeProvider">Source de temps (utile pour les tests).</param>
    public Keystore(TimeSpan? inactivityTimeout = null, TimeProvider? timeProvider = null)
    {
        _inactivityTimeout = inactivityTimeout ?? TimeSpan.FromMinutes(30);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// <c>true</c> si le keystore est déverrouillé et le délai d'inactivité non dépassé.
    /// La consultation de cette propriété <b>ne réinitialise pas</b> le compteur d'inactivité.
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            if (_masterKey is null)
            {
                return false;
            }

            if (IsTimeoutElapsed())
            {
                Lock();
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Déverrouille le coffre en dérivant la master key à partir du mot de
    /// passe et du sel fournis. Si le coffre était déjà déverrouillé, l'ancien
    /// contenu est zéroïsé avant remplacement.
    /// </summary>
    public void Unlock(string masterPassword, byte[] salt, KeyDerivationParameters? parameters = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ZeroMasterKey();
        _masterKey = KeyDerivation.DeriveKey(masterPassword, salt, parameters);
        _lastActivityUtc = _timeProvider.GetUtcNow();
    }

    /// <summary>Verrouille le coffre et zéroïse la master key.</summary>
    public void Lock()
    {
        ZeroMasterKey();
    }

    /// <summary>
    /// Chiffre une chaîne UTF-8. Réinitialise le compteur d'inactivité.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si le coffre est verrouillé.</exception>
    public EncryptedString EncryptString(string plaintext)
    {
        byte[] key = TouchAndGetKey();
        return SymmetricCrypto.EncryptString(plaintext, key);
    }

    /// <summary>Chiffre un blob binaire. Réinitialise le compteur d'inactivité.</summary>
    public EncryptedBytes EncryptBytes(byte[] plaintext)
    {
        byte[] key = TouchAndGetKey();
        return SymmetricCrypto.EncryptBytes(plaintext, key);
    }

    /// <summary>Déchiffre une <see cref="EncryptedString"/>. Réinitialise le compteur d'inactivité.</summary>
    public string DecryptString(EncryptedString envelope)
    {
        byte[] key = TouchAndGetKey();
        return SymmetricCrypto.DecryptString(envelope, key);
    }

    /// <summary>Déchiffre une <see cref="EncryptedBytes"/>. Réinitialise le compteur d'inactivité.</summary>
    public byte[] DecryptBytes(EncryptedBytes envelope)
    {
        byte[] key = TouchAndGetKey();
        return SymmetricCrypto.DecryptBytes(envelope, key);
    }

    /// <summary>
    /// Récupère une copie de la master key actuelle (ex: pour passer à <see cref="Data.HarborDbContext"/>
    /// pour ouvrir SQLCipher). Le caller est responsable de zéroïser sa copie après usage.
    /// Réinitialise le compteur d'inactivité.
    /// </summary>
    public byte[] CopyMasterKey()
    {
        byte[] key = TouchAndGetKey();
        byte[] copy = new byte[key.Length];
        Buffer.BlockCopy(key, 0, copy, 0, key.Length);
        return copy;
    }

    /// <summary>Verrouille le coffre et libère les ressources.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Lock();
        _disposed = true;
    }

    private byte[] TouchAndGetKey()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_masterKey is null)
        {
            throw new InvalidOperationException("Keystore verrouillé : appelez Unlock() avant toute opération cryptographique.");
        }

        if (IsTimeoutElapsed())
        {
            Lock();
            throw new InvalidOperationException("Keystore verrouillé pour inactivité.");
        }

        _lastActivityUtc = _timeProvider.GetUtcNow();
        return _masterKey;
    }

    private bool IsTimeoutElapsed() =>
        _timeProvider.GetUtcNow() - _lastActivityUtc > _inactivityTimeout;

    private void ZeroMasterKey()
    {
        if (_masterKey is not null)
        {
            CryptographicOperations.ZeroMemory(_masterKey);
            _masterKey = null;
        }
    }
}
