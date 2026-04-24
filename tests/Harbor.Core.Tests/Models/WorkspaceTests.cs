using Harbor.Core.Models;

namespace Harbor.Core.Tests.Models;

public sealed class WorkspaceTests
{
    [Fact]
    public void RecordEqualityIsByValue()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<Guid> profiles = [Guid.NewGuid(), Guid.NewGuid()];

        Workspace a = new(id, "Client X", "briefcase", "#7aa2f7", profiles, null, now, now);
        Workspace b = new(id, "Client X", "briefcase", "#7aa2f7", profiles, null, now, now);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void WithExpressionProducesUpdatedCopyLeavingOriginalUntouched()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Workspace original = new(
            Guid.NewGuid(),
            "Avant",
            null,
            null,
            [],
            null,
            now,
            now);

        Workspace renamed = original with { Name = "Après" };

        Assert.Equal("Avant", original.Name);
        Assert.Equal("Après", renamed.Name);
        Assert.NotEqual(original, renamed);
        Assert.Equal(original.Id, renamed.Id);
    }
}
