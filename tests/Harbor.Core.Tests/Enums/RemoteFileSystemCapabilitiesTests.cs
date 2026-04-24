using Harbor.Core.Enums;

namespace Harbor.Core.Tests.Enums;

public sealed class RemoteFileSystemCapabilitiesTests
{
    [Fact]
    public void NoneIsZero()
    {
        Assert.Equal(0, (int)RemoteFileSystemCapabilities.None);
    }

    [Fact]
    public void FlagsCombineAndReportViaHasFlag()
    {
        RemoteFileSystemCapabilities caps =
            RemoteFileSystemCapabilities.UnixPermissions
            | RemoteFileSystemCapabilities.Symlinks
            | RemoteFileSystemCapabilities.Watch;

        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.UnixPermissions));
        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.Symlinks));
        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.Watch));
        Assert.False(caps.HasFlag(RemoteFileSystemCapabilities.HardLinks));
        Assert.False(caps.HasFlag(RemoteFileSystemCapabilities.PartialReads));
    }

    [Fact]
    public void AllFlagsArePowersOfTwo()
    {
        foreach (RemoteFileSystemCapabilities value in Enum.GetValues<RemoteFileSystemCapabilities>())
        {
            int n = (int)value;
            if (n == 0)
            {
                continue;
            }

            Assert.Equal(0, n & (n - 1));
        }
    }
}
