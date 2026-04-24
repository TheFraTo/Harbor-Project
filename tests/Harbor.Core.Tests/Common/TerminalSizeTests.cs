using Harbor.Core.Common;

namespace Harbor.Core.Tests.Common;

public sealed class TerminalSizeTests
{
    [Fact]
    public void DefaultIsEightyByTwentyFour()
    {
        TerminalSize size = TerminalSize.Default;

        Assert.Equal(80, size.Columns);
        Assert.Equal(24, size.Rows);
    }

    [Fact]
    public void TwoInstancesWithSameValuesAreEqual()
    {
        TerminalSize a = new(120, 40);
        TerminalSize b = new(120, 40);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void TwoInstancesWithDifferentValuesAreNotEqual()
    {
        TerminalSize a = new(80, 24);
        TerminalSize b = new(80, 25);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }
}
