using Harbor.Core.Models;
using Harbor.Data.Repositories;
using Harbor.Data.TestSupport;

namespace Harbor.Data.Tests.Repositories;

public sealed class SnippetRepositoryTests
{
    [Fact]
    public async Task InsertThenGetByIdRoundTripsVariablesAndTags()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        SnippetRepository repo = new(fixture.Context);

        IReadOnlyList<SnippetVariable> vars =
        [
            new("service", "nginx", "Nom du service systemd"),
            new("file", "access.log", null),
        ];

        Snippet original = new(
            Id: Guid.NewGuid(),
            Name: "Tail log",
            Description: "Suit un fichier de log d'un service donné",
            Command: "tail -f /var/log/${service}/${file}",
            Variables: vars,
            Tags: ["log", "ops"],
            CreatedAt: DateTimeOffset.UtcNow);

        await repo.InsertAsync(original);
        Snippet? retrieved = await repo.GetByIdAsync(original.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(original.Name, retrieved.Name);
        Assert.Equal(original.Command, retrieved.Command);
        Assert.Equal(2, retrieved.Variables.Count);
        Assert.Equal("service", retrieved.Variables[0].Name);
        Assert.Equal("nginx", retrieved.Variables[0].DefaultValue);
        Assert.Equal("Nom du service systemd", retrieved.Variables[0].Description);
        Assert.Equal("file", retrieved.Variables[1].Name);
        Assert.Null(retrieved.Variables[1].Description);
        Assert.Equal(["log", "ops"], retrieved.Tags);
    }

    [Fact]
    public async Task SnippetWithoutVariablesOrTagsRoundTripsEmpty()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        SnippetRepository repo = new(fixture.Context);

        Snippet original = new(
            Guid.NewGuid(), "Nu", null, "echo 'minimal'", [], [], DateTimeOffset.UtcNow);

        await repo.InsertAsync(original);
        Snippet? retrieved = await repo.GetByIdAsync(original.Id);

        Assert.NotNull(retrieved);
        Assert.Empty(retrieved.Variables);
        Assert.Empty(retrieved.Tags);
    }

    [Fact]
    public async Task UpdateChangesCommandAndDelete()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        SnippetRepository repo = new(fixture.Context);
        Guid id = Guid.NewGuid();

        await repo.InsertAsync(new Snippet(id, "x", null, "echo old", [], [], DateTimeOffset.UtcNow));

        bool ok = await repo.UpdateAsync(new Snippet(id, "x", null, "echo new", [], [], DateTimeOffset.UtcNow));
        Assert.True(ok);
        Snippet? r = await repo.GetByIdAsync(id);
        Assert.Equal("echo new", r?.Command);

        Assert.True(await repo.DeleteAsync(id));
        Assert.Null(await repo.GetByIdAsync(id));
    }
}
