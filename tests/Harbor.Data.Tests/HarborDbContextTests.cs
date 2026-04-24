using Harbor.Data.Tests.Fixtures;

namespace Harbor.Data.Tests;

public sealed class HarborDbContextTests
{
    [Fact]
    public async Task OpenAsyncIsIdempotent()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();

        Assert.True(fixture.Context.IsOpen);
        await fixture.Context.OpenAsync();
        Assert.True(fixture.Context.IsOpen);
    }

    [Fact]
    public async Task ConnectionThrowsBeforeOpen()
    {
        byte[] key = HarborDbContext.DeriveTestKey("x");
        string path = Path.Combine(Path.GetTempPath(), $"harbor-preopen-{Guid.NewGuid():N}.db");
        HarborDbContext ctx = new(new HarborDbOptions(path, key));

        _ = Assert.Throws<InvalidOperationException>(() => ctx.Connection);

        await ctx.DisposeAsync();
    }

    [Fact]
    public void ConstructorRejectsKeyOfWrongLength()
    {
        byte[] shortKey = new byte[16];
        HarborDbOptions badOptions = new("whatever.db", shortKey);

        _ = Assert.Throws<ArgumentException>(() => new HarborDbContext(badOptions));
    }

    [Fact]
    public async Task DatabaseIsAccessibleAcrossCloseAndReopen()
    {
        string path = Path.Combine(Path.GetTempPath(), $"harbor-reopen-{Guid.NewGuid():N}.db");
        byte[] key = HarborDbContext.DeriveTestKey("reopen-test");
        HarborDbOptions options = new(path, key);

        try
        {
            // Première ouverture + migration
            await using (HarborDbContext ctx1 = new(options))
            {
                await ctx1.OpenAsync();
                Migrations.MigrationRunner runner = new(ctx1);
                _ = await runner.MigrateAsync();
            }

            // Seconde ouverture sur le même fichier : doit pouvoir déchiffrer et lire
            await using HarborDbContext ctx2 = new(options);
            await ctx2.OpenAsync();

            Migrations.MigrationRunner runner2 = new(ctx2);
            IReadOnlyList<int> applied = await runner2.GetAppliedVersionsListAsync();
            Assert.Single(applied);
        }
        finally
        {
            foreach (string suffix in new[] { "", "-wal", "-shm", "-journal" })
            {
                string f = path + suffix;
                if (File.Exists(f))
                {
                    try { File.Delete(f); } catch (IOException) { }
                }
            }
        }
    }
}
