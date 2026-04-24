using Harbor.Core.Models;
using Harbor.Data.Repositories;
using Harbor.Data.Tests.Fixtures;

namespace Harbor.Data.Tests.Repositories;

public sealed class WorkspaceRepositoryTests
{
    [Fact]
    public async Task InsertThenGetByIdRoundTripsAllFields()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository repo = new(fixture.Context);

        DateTimeOffset now = new(2026, 4, 24, 22, 0, 0, TimeSpan.Zero);
        Workspace original = new(
            Id: Guid.NewGuid(),
            Name: "Client X — Prod",
            Icon: "briefcase",
            Color: "#7aa2f7",
            ProfileIds: [],
            Notes: "Serveur principal + bastion",
            CreatedAt: now,
            UpdatedAt: now);

        await repo.InsertAsync(original);
        Workspace? retrieved = await repo.GetByIdAsync(original.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(original.Id, retrieved.Id);
        Assert.Equal(original.Name, retrieved.Name);
        Assert.Equal(original.Icon, retrieved.Icon);
        Assert.Equal(original.Color, retrieved.Color);
        Assert.Equal(original.Notes, retrieved.Notes);
        Assert.Equal(original.CreatedAt, retrieved.CreatedAt);
        Assert.Equal(original.UpdatedAt, retrieved.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdReturnsNullForUnknownId()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository repo = new(fixture.Context);

        Workspace? result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllReturnsWorkspacesOrderedByName()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository repo = new(fixture.Context);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await repo.InsertAsync(new Workspace(Guid.NewGuid(), "Zoo", null, null, [], null, now, now));
        await repo.InsertAsync(new Workspace(Guid.NewGuid(), "Alpha", null, null, [], null, now, now));
        await repo.InsertAsync(new Workspace(Guid.NewGuid(), "Mike", null, null, [], null, now, now));

        IReadOnlyList<Workspace> all = await repo.GetAllAsync();

        Assert.Equal(3, all.Count);
        Assert.Equal("Alpha", all[0].Name);
        Assert.Equal("Mike", all[1].Name);
        Assert.Equal("Zoo", all[2].Name);
    }

    [Fact]
    public async Task UpdateChangesFieldsButPreservesId()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository repo = new(fixture.Context);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Guid id = Guid.NewGuid();
        await repo.InsertAsync(new Workspace(id, "Avant", null, null, [], null, now, now));
        bool updated = await repo.UpdateAsync(
            new Workspace(id, "Après", "star", "#f7768e", [], "notes edit", now, now.AddMinutes(5)));

        Assert.True(updated);
        Workspace? retrieved = await repo.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal("Après", retrieved.Name);
        Assert.Equal("star", retrieved.Icon);
        Assert.Equal("#f7768e", retrieved.Color);
        Assert.Equal("notes edit", retrieved.Notes);
    }

    [Fact]
    public async Task UpdateReturnsFalseForUnknownId()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository repo = new(fixture.Context);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        bool updated = await repo.UpdateAsync(
            new Workspace(Guid.NewGuid(), "Fantôme", null, null, [], null, now, now));

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteRemovesRowAndReturnsTrue()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository repo = new(fixture.Context);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Guid id = Guid.NewGuid();
        await repo.InsertAsync(new Workspace(id, "À supprimer", null, null, [], null, now, now));

        bool deleted = await repo.DeleteAsync(id);

        Assert.True(deleted);
        Assert.Null(await repo.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteReturnsFalseForUnknownId()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository repo = new(fixture.Context);

        bool deleted = await repo.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }
}
