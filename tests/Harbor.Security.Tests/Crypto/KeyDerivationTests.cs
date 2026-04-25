using Harbor.Security.Crypto;

namespace Harbor.Security.Tests.Crypto;

public sealed class KeyDerivationTests
{
    // Paramètres rapides pour les tests : Argon2id reste lent même au minimum,
    // mais on évite les 200-400 ms du défaut en testant la mécanique uniquement.
    private static readonly KeyDerivationParameters FastForTests = new(
        MemoryKb: 1024, Iterations: 1, Parallelism: 1);

    [Fact]
    public void GenerateSaltReturnsSixteenRandomBytes()
    {
        byte[] s1 = KeyDerivation.GenerateSalt();
        byte[] s2 = KeyDerivation.GenerateSalt();

        Assert.Equal(KeyDerivation.SaltLength, s1.Length);
        Assert.Equal(KeyDerivation.SaltLength, s2.Length);
        Assert.NotEqual(s1, s2);
    }

    [Fact]
    public void DeriveKeyReturnsThirtyTwoBytes()
    {
        byte[] salt = KeyDerivation.GenerateSalt();
        byte[] key = KeyDerivation.DeriveKey("hunter2", salt, FastForTests);

        Assert.Equal(KeyDerivation.KeyLength, key.Length);
    }

    [Fact]
    public void DeriveKeyIsDeterministicForSameInputs()
    {
        byte[] salt = KeyDerivation.GenerateSalt();
        byte[] k1 = KeyDerivation.DeriveKey("hunter2", salt, FastForTests);
        byte[] k2 = KeyDerivation.DeriveKey("hunter2", salt, FastForTests);

        Assert.Equal(k1, k2);
    }

    [Fact]
    public void DeriveKeyDiffersWithDifferentSalt()
    {
        byte[] salt1 = KeyDerivation.GenerateSalt();
        byte[] salt2 = KeyDerivation.GenerateSalt();
        byte[] k1 = KeyDerivation.DeriveKey("hunter2", salt1, FastForTests);
        byte[] k2 = KeyDerivation.DeriveKey("hunter2", salt2, FastForTests);

        Assert.NotEqual(k1, k2);
    }

    [Fact]
    public void DeriveKeyDiffersWithDifferentPassword()
    {
        byte[] salt = KeyDerivation.GenerateSalt();
        byte[] k1 = KeyDerivation.DeriveKey("password-a", salt, FastForTests);
        byte[] k2 = KeyDerivation.DeriveKey("password-b", salt, FastForTests);

        Assert.NotEqual(k1, k2);
    }

    [Fact]
    public void DeriveKeyRejectsWrongSaltLength()
    {
        _ = Assert.Throws<ArgumentException>(() =>
            KeyDerivation.DeriveKey("pwd", new byte[8], FastForTests));
    }
}
