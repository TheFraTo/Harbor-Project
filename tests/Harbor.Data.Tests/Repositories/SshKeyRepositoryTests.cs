using Harbor.Core.Common;
using Harbor.Core.Enums;
using Harbor.Core.Models;
using Harbor.Data.Repositories;
using Harbor.Data.TestSupport;

namespace Harbor.Data.Tests.Repositories;

public sealed class SshKeyRepositoryTests
{
    [Fact]
    public async Task InsertThenGetByIdRoundTripsEncryptedPrivateKey()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        SshKeyRepository repo = new(fixture.Context);

        EncryptedBytes privateKey = new(
            Nonce: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            Ciphertext: [0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45],
            Tag: [0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0xFF]);

        SshKey original = new(
            Id: Guid.NewGuid(),
            Name: "Hetzner main",
            Algorithm: KeyAlgorithm.Ed25519,
            PrivateKey: privateKey,
            PublicKey: [0xAA, 0xBB, 0xCC],
            Comment: "fralawks@workstation",
            CreatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null);

        await repo.InsertAsync(original);
        SshKey? retrieved = await repo.GetByIdAsync(original.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(original.Id, retrieved.Id);
        Assert.Equal(original.Name, retrieved.Name);
        Assert.Equal(KeyAlgorithm.Ed25519, retrieved.Algorithm);
        Assert.Equal(privateKey.Nonce, retrieved.PrivateKey.Nonce);
        Assert.Equal(privateKey.Ciphertext, retrieved.PrivateKey.Ciphertext);
        Assert.Equal(privateKey.Tag, retrieved.PrivateKey.Tag);
        Assert.Equal(original.PublicKey, retrieved.PublicKey);
        Assert.Equal("fralawks@workstation", retrieved.Comment);
    }

    [Fact]
    public async Task UpdateMetadataChangesNameCommentAndLastUsed()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        SshKeyRepository repo = new(fixture.Context);

        Guid id = Guid.NewGuid();
        EncryptedBytes pk = new([1], [2], [3]);
        await repo.InsertAsync(new SshKey(id, "Initial", KeyAlgorithm.Rsa, pk, [0x99], null, DateTimeOffset.UtcNow, null));

        DateTimeOffset usedAt = new(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        bool ok = await repo.UpdateMetadataAsync(id, "Renommée", "rotation 2026-Q2", usedAt);

        Assert.True(ok);
        SshKey? r = await repo.GetByIdAsync(id);
        Assert.NotNull(r);
        Assert.Equal("Renommée", r.Name);
        Assert.Equal("rotation 2026-Q2", r.Comment);
        Assert.Equal(usedAt, r.LastUsedAt);
    }

    [Fact]
    public async Task DeleteRemovesKey()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        SshKeyRepository repo = new(fixture.Context);

        Guid id = Guid.NewGuid();
        await repo.InsertAsync(new SshKey(id, "À jeter", KeyAlgorithm.Ed25519,
            new EncryptedBytes([1], [2], [3]), [0xFF], null, DateTimeOffset.UtcNow, null));

        Assert.True(await repo.DeleteAsync(id));
        Assert.Null(await repo.GetByIdAsync(id));
    }
}
