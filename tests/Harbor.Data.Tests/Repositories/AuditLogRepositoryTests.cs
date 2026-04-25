using Harbor.Core.Enums;
using Harbor.Core.Models;
using Harbor.Data.Repositories;
using Harbor.Data.Tests.Fixtures;

namespace Harbor.Data.Tests.Repositories;

public sealed class AuditLogRepositoryTests
{
    [Fact]
    public async Task InsertAndGetRecentReturnsLatestFirst()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        AuditLogRepository repo = new(fixture.Context);
        DateTimeOffset baseTime = new(2026, 4, 24, 0, 0, 0, TimeSpan.Zero);

        await repo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), baseTime, AuditEventType.ConnectionOpened, null, "ssh prod-1", null));
        await repo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), baseTime.AddSeconds(10), AuditEventType.SecretRead, null, "key fetch", null));
        await repo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), baseTime.AddSeconds(5), AuditEventType.ProfileCreated, null, "new profile", null));

        IReadOnlyList<AuditLogEntry> recent = await repo.GetRecentAsync();

        Assert.Equal(3, recent.Count);
        Assert.Equal(AuditEventType.SecretRead, recent[0].Type);
        Assert.Equal(AuditEventType.ProfileCreated, recent[1].Type);
        Assert.Equal(AuditEventType.ConnectionOpened, recent[2].Type);
    }

    [Fact]
    public async Task GetByProfileFiltersOnProfileId()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        AuditLogRepository auditRepo = new(fixture.Context);
        ProfileRepository profileRepo = new(fixture.Context);

        // FK profile_id → profiles(id) impose que les profils existent réellement.
        Guid targetProfile = Guid.NewGuid();
        Guid otherProfile = Guid.NewGuid();
        await profileRepo.InsertAsync(BuildMinimalProfile(targetProfile, "Target"));
        await profileRepo.InsertAsync(BuildMinimalProfile(otherProfile, "Other"));

        await auditRepo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AuditEventType.ConnectionOpened, targetProfile, "x", null));
        await auditRepo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AuditEventType.ConnectionClosed, targetProfile, "y", null));
        await auditRepo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AuditEventType.ConnectionOpened, otherProfile, "z", null));

        IReadOnlyList<AuditLogEntry> filtered = await auditRepo.GetByProfileAsync(targetProfile);

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, e => Assert.Equal(targetProfile, e.ProfileId));
    }

    [Fact]
    public async Task DeleteOlderThanRemovesPastEntriesAndReturnsCount()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        AuditLogRepository repo = new(fixture.Context);
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        await repo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), baseTime.AddDays(-40), AuditEventType.ConnectionOpened, null, "old", null));
        await repo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), baseTime.AddDays(-35), AuditEventType.ConnectionOpened, null, "old2", null));
        await repo.InsertAsync(new AuditLogEntry(
            Guid.NewGuid(), baseTime.AddDays(-1), AuditEventType.ConnectionOpened, null, "recent", null));

        int deleted = await repo.DeleteOlderThanAsync(baseTime.AddDays(-30));
        Assert.Equal(2, deleted);

        IReadOnlyList<AuditLogEntry> remaining = await repo.GetRecentAsync();
        Assert.Single(remaining);
        Assert.Equal("recent", remaining[0].Description);
    }

    private static Profile BuildMinimalProfile(Guid id, string name) => new(
        Id: id,
        Name: name,
        Protocol: ProtocolKind.Ssh,
        Connection: new SshConnectionDetails("h", 22, "u", null),
        Auth: new AgentAuth(),
        Tags: [],
        ParentFolderId: null,
        EnvVars: new Dictionary<string, string>(),
        PostConnectScript: null,
        Notes: null,
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow,
        LastUsedAt: null);
}
