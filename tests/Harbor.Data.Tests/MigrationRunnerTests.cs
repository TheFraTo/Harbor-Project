using Harbor.Data.Migrations;
using Harbor.Data.TestSupport;

namespace Harbor.Data.Tests;

public sealed class MigrationRunnerTests
{
    [Fact]
    public void DiscoverAvailableMigrationsReturnsAtLeastInitial()
    {
        IReadOnlyList<(int Version, string Name)> migrations = MigrationRunner.DiscoverAvailableMigrations();

        Assert.NotEmpty(migrations);
        Assert.Contains(migrations, m => m.Version == 1 && m.Name == "Initial");
    }

    [Fact]
    public async Task MigrateAsyncAppliesInitialMigrationOnEmptyDatabase()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();

        MigrationRunner runner = new(fixture.Context);
        IReadOnlyList<int> applied = await runner.GetAppliedVersionsListAsync();

        Assert.Single(applied);
        Assert.Equal(1, applied[0]);
    }

    [Fact]
    public async Task MigrateAsyncIsIdempotent()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();

        MigrationRunner runner = new(fixture.Context);
        int second = await runner.MigrateAsync();

        Assert.Equal(0, second);
    }

    [Fact]
    public async Task MigrateAsyncThrowsWhenContextNotOpen()
    {
        byte[] key = TestKey.Derive("x");
        string tempPath = Path.Combine(Path.GetTempPath(), $"harbor-fail-{Guid.NewGuid():N}.db");
        HarborDbContext ctx = new(new HarborDbOptions(tempPath, key));
        MigrationRunner runner = new(ctx);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.MigrateAsync());
    }
}
