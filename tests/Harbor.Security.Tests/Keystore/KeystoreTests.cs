using Harbor.Core.Common;
using Harbor.Security.Crypto;
using HarborKeystore = Harbor.Security.Keystore.Keystore;

namespace Harbor.Security.Tests.Keystore;

public sealed class KeystoreTests
{
    private static readonly KeyDerivationParameters FastForTests = new(
        MemoryKb: 1024, Iterations: 1, Parallelism: 1);

    [Fact]
    public void NewKeystoreIsLocked()
    {
        using HarborKeystore keystore = new();
        Assert.False(keystore.IsUnlocked);
    }

    [Fact]
    public void UnlockMakesKeystoreOperational()
    {
        using HarborKeystore keystore = new();
        byte[] salt = KeyDerivation.GenerateSalt();

        keystore.Unlock("master-password", salt, FastForTests);

        Assert.True(keystore.IsUnlocked);
    }

    [Fact]
    public void EncryptThenDecryptRoundTrips()
    {
        using HarborKeystore keystore = new();
        keystore.Unlock("master-password", KeyDerivation.GenerateSalt(), FastForTests);

        EncryptedString envelope = keystore.EncryptString("ssh-key-passphrase");
        string back = keystore.DecryptString(envelope);

        Assert.Equal("ssh-key-passphrase", back);
    }

    [Fact]
    public void EncryptionWhileLockedThrows()
    {
        using HarborKeystore keystore = new();

        _ = Assert.Throws<InvalidOperationException>(() => keystore.EncryptString("x"));
    }

    [Fact]
    public void LockClearsTheMasterKey()
    {
        using HarborKeystore keystore = new();
        keystore.Unlock("p", KeyDerivation.GenerateSalt(), FastForTests);
        Assert.True(keystore.IsUnlocked);

        keystore.Lock();

        Assert.False(keystore.IsUnlocked);
    }

    [Fact]
    public void InactivityTimeoutAutoLocks()
    {
        FakeTimeProvider time = new();
        using HarborKeystore keystore = new(
            inactivityTimeout: TimeSpan.FromMinutes(5),
            timeProvider: time);

        keystore.Unlock("p", KeyDerivation.GenerateSalt(), FastForTests);
        Assert.True(keystore.IsUnlocked);

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.False(keystore.IsUnlocked);
        _ = Assert.Throws<InvalidOperationException>(() => keystore.EncryptString("x"));
    }

    [Fact]
    public void ActivityResetsTheInactivityCounter()
    {
        FakeTimeProvider time = new();
        using HarborKeystore keystore = new(
            inactivityTimeout: TimeSpan.FromMinutes(5),
            timeProvider: time);

        keystore.Unlock("p", KeyDerivation.GenerateSalt(), FastForTests);

        time.Advance(TimeSpan.FromMinutes(4));
        _ = keystore.EncryptString("touch"); // réinitialise le compteur

        time.Advance(TimeSpan.FromMinutes(4));
        Assert.True(keystore.IsUnlocked);
    }

    [Fact]
    public void CopyMasterKeyReturnsThirtyTwoIndependentBytes()
    {
        using HarborKeystore keystore = new();
        keystore.Unlock("p", KeyDerivation.GenerateSalt(), FastForTests);

        byte[] copy = keystore.CopyMasterKey();
        Assert.Equal(32, copy.Length);

        Array.Clear(copy);
        Assert.True(keystore.IsUnlocked); // Le caller a effacé sa copie, le keystore reste OK
    }

    [Fact]
    public void DisposeLocksTheKeystore()
    {
        HarborKeystore keystore = new();
        keystore.Unlock("p", KeyDerivation.GenerateSalt(), FastForTests);

        keystore.Dispose();

        _ = Assert.Throws<ObjectDisposedException>(() =>
            keystore.Unlock("p", KeyDerivation.GenerateSalt(), FastForTests));
    }

    /// <summary>Source de temps contrôlable pour les tests d'inactivité.</summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 4, 24, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
