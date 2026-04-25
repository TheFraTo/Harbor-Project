using Harbor.Core.Enums;
using Harbor.Core.Models;
using Harbor.Data.Repositories;
using Harbor.Data.TestSupport;

namespace Harbor.Data.Tests.Repositories;

public sealed class TransferRepositoryTests
{
    [Fact]
    public async Task InsertThenGetByIdRoundTripsAllFields()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        TransferRepository repo = new(fixture.Context);

        // Note : SourceProfileId et DestProfileId à null pour ce test,
        // car les FK ON DELETE SET NULL nécessitent un profil existant si non-null.
        // Le test FK avec un profil réel est couvert dans ProfileRepositoryTests.
        Transfer original = new(
            Id: Guid.NewGuid(),
            Direction: TransferDirection.Upload,
            SourcePath: "/home/user/dist",
            DestPath: "/var/www/app",
            SourceProfileId: null,
            DestProfileId: null,
            TotalBytes: 1024 * 1024 * 50,
            TransferredBytes: 0,
            Status: TransferStatus.Queued,
            ErrorMessage: null,
            Priority: 5,
            CreatedAt: DateTimeOffset.UtcNow,
            CompletedAt: null);

        await repo.InsertAsync(original);
        Transfer? retrieved = await repo.GetByIdAsync(original.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(TransferDirection.Upload, retrieved.Direction);
        Assert.Null(retrieved.SourceProfileId);
        Assert.Null(retrieved.DestProfileId);
        Assert.Equal(50L * 1024 * 1024, retrieved.TotalBytes);
        Assert.Equal(TransferStatus.Queued, retrieved.Status);
        Assert.Equal(5, retrieved.Priority);
    }

    [Fact]
    public async Task UpdateProgressTransitionsStatusAndCompletedAt()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        TransferRepository repo = new(fixture.Context);
        Guid id = Guid.NewGuid();

        await repo.InsertAsync(new Transfer(
            id, TransferDirection.Download, "/remote/file", "/local/file",
            null, null, 1000, 0, TransferStatus.Queued, null, 0, DateTimeOffset.UtcNow, null));

        DateTimeOffset completed = new(2026, 4, 24, 12, 0, 0, TimeSpan.Zero);
        bool ok = await repo.UpdateProgressAsync(id, 1000, TransferStatus.Completed, null, completed);

        Assert.True(ok);
        Transfer? r = await repo.GetByIdAsync(id);
        Assert.NotNull(r);
        Assert.Equal(TransferStatus.Completed, r.Status);
        Assert.Equal(1000, r.TransferredBytes);
        Assert.Equal(completed, r.CompletedAt);
    }

    [Fact]
    public async Task GetAllOrdersByPriorityDescThenCreatedAtAsc()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        TransferRepository repo = new(fixture.Context);
        DateTimeOffset baseTime = new(2026, 4, 24, 0, 0, 0, TimeSpan.Zero);

        Guid lowPriorityNewer = Guid.NewGuid();
        Guid highPriority = Guid.NewGuid();
        Guid lowPriorityOlder = Guid.NewGuid();

        await repo.InsertAsync(new Transfer(
            lowPriorityNewer, TransferDirection.Upload, "a", "b", null, null, 100, 0,
            TransferStatus.Queued, null, 1, baseTime.AddMinutes(10), null));
        await repo.InsertAsync(new Transfer(
            highPriority, TransferDirection.Upload, "a", "b", null, null, 100, 0,
            TransferStatus.Queued, null, 9, baseTime.AddMinutes(5), null));
        await repo.InsertAsync(new Transfer(
            lowPriorityOlder, TransferDirection.Upload, "a", "b", null, null, 100, 0,
            TransferStatus.Queued, null, 1, baseTime, null));

        IReadOnlyList<Transfer> all = await repo.GetAllAsync();

        Assert.Equal(3, all.Count);
        Assert.Equal(highPriority, all[0].Id);
        Assert.Equal(lowPriorityOlder, all[1].Id);
        Assert.Equal(lowPriorityNewer, all[2].Id);
    }

    [Fact]
    public async Task GetAllWithStatusFilterReturnsOnlyMatching()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        TransferRepository repo = new(fixture.Context);

        Guid queued = Guid.NewGuid();
        Guid failed = Guid.NewGuid();

        await repo.InsertAsync(new Transfer(
            queued, TransferDirection.Upload, "a", "b", null, null, 100, 0,
            TransferStatus.Queued, null, 0, DateTimeOffset.UtcNow, null));
        await repo.InsertAsync(new Transfer(
            failed, TransferDirection.Upload, "a", "b", null, null, 100, 0,
            TransferStatus.Failed, "boom", 0, DateTimeOffset.UtcNow, null));

        IReadOnlyList<Transfer> queuedOnly = await repo.GetAllAsync(TransferStatus.Queued);

        Assert.Single(queuedOnly);
        Assert.Equal(queued, queuedOnly[0].Id);
    }
}
