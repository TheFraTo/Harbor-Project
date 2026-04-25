using System.Security.Cryptography;
using System.Text;
using Harbor.Core.Common;
using Harbor.Security.Crypto;

namespace Harbor.Security.Tests.Crypto;

public sealed class SymmetricCryptoTests
{
    private static byte[] NewKey()
    {
        byte[] k = new byte[32];
        RandomNumberGenerator.Fill(k);
        return k;
    }

    [Fact]
    public void EncryptStringRoundTripsToOriginal()
    {
        byte[] key = NewKey();
        EncryptedString envelope = SymmetricCrypto.EncryptString("Bonjour, le monde 🌍", key);
        string back = SymmetricCrypto.DecryptString(envelope, key);

        Assert.Equal("Bonjour, le monde 🌍", back);
    }

    [Fact]
    public void EncryptBytesRoundTripsToOriginal()
    {
        byte[] key = NewKey();
        byte[] payload = Encoding.UTF8.GetBytes("binary content");

        EncryptedBytes envelope = SymmetricCrypto.EncryptBytes(payload, key);
        byte[] back = SymmetricCrypto.DecryptBytes(envelope, key);

        Assert.Equal(payload, back);
    }

    [Fact]
    public void EachEncryptionUsesAFreshNonce()
    {
        byte[] key = NewKey();
        EncryptedString a = SymmetricCrypto.EncryptString("identique", key);
        EncryptedString b = SymmetricCrypto.EncryptString("identique", key);

        Assert.NotEqual(a.Nonce, b.Nonce);
        Assert.NotEqual(a.Ciphertext, b.Ciphertext);
    }

    [Fact]
    public void NonceAndTagHaveStandardSizes()
    {
        byte[] key = NewKey();
        EncryptedString envelope = SymmetricCrypto.EncryptString("payload", key);

        Assert.Equal(SymmetricCrypto.NonceLength, envelope.Nonce.Length);
        Assert.Equal(SymmetricCrypto.TagLength, envelope.Tag.Length);
    }

    [Fact]
    public void DecryptionWithWrongKeyThrows()
    {
        byte[] key1 = NewKey();
        byte[] key2 = NewKey();

        EncryptedString envelope = SymmetricCrypto.EncryptString("secret", key1);

        _ = Assert.Throws<AuthenticationTagMismatchException>(() =>
            SymmetricCrypto.DecryptString(envelope, key2));
    }

    [Fact]
    public void TamperingWithCiphertextIsDetected()
    {
        byte[] key = NewKey();
        EncryptedString envelope = SymmetricCrypto.EncryptString("integrity test", key);

        byte[] tampered = (byte[])envelope.Ciphertext.Clone();
        tampered[0] ^= 0xFF;
        EncryptedString broken = envelope with { Ciphertext = tampered };

        _ = Assert.Throws<AuthenticationTagMismatchException>(() =>
            SymmetricCrypto.DecryptString(broken, key));
    }

    [Fact]
    public void EncryptionRejectsKeyOfWrongLength()
    {
        byte[] tooShort = new byte[16];
        _ = Assert.Throws<ArgumentException>(() => SymmetricCrypto.EncryptString("x", tooShort));
    }
}
