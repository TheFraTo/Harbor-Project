using Harbor.Core.Enums;

namespace Harbor.Protocols.Ssh.Tests;

public sealed class SftpProviderTests
{
    private static SshConnection NewConnection() =>
        SshConnection.WithPassword("example.com", 22, "deploy", "hunter2");

    [Fact]
    public async Task ConstructorRejectsNullConnectionAsync()
    {
        await Task.Yield();
        _ = Assert.Throws<ArgumentNullException>(() => new SftpProvider(null!));
    }

    [Fact]
    public async Task IsConnectedIsFalseAfterConstructionAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        Assert.False(provider.IsConnected);
    }

    [Fact]
    public async Task CapabilitiesAdvertiseTheExpectedFlagsAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        RemoteFileSystemCapabilities caps = provider.Capabilities;

        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.UnixPermissions));
        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.Symlinks));
        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.AtomicRename));
        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.PartialReads));
        Assert.True(caps.HasFlag(RemoteFileSystemCapabilities.PartialWrites));
    }

    [Fact]
    public async Task CapabilitiesExcludeUnsupportedFlagsAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        RemoteFileSystemCapabilities caps = provider.Capabilities;

        Assert.False(caps.HasFlag(RemoteFileSystemCapabilities.HardLinks));
        Assert.False(caps.HasFlag(RemoteFileSystemCapabilities.ExtendedAttributes));
        Assert.False(caps.HasFlag(RemoteFileSystemCapabilities.Watch));
    }

    [Fact]
    public async Task WatchAsyncThrowsNotSupportedAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        _ = Assert.Throws<NotSupportedException>(() => provider.WatchAsync("/var/log"));
    }

    [Fact]
    public async Task ListAsyncBeforeConnectThrowsInvalidOperationAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ListAsync("/"));
    }

    [Fact]
    public async Task StatAsyncBeforeConnectThrowsInvalidOperationAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.StatAsync("/etc/hostname"));
    }

    [Fact]
    public async Task OpenReadAsyncBeforeConnectThrowsInvalidOperationAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.OpenReadAsync("/etc/hostname"));
    }

    [Fact]
    public async Task OpenReadAsyncRejectsNegativeOffsetAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.OpenReadAsync("/foo", offset: -1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ListAsyncRejectsNullOrEmptyPathAsync(string? badPath)
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => provider.ListAsync(badPath!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RenameAsyncRejectsNullOrEmptyOldPathAsync(string? badPath)
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider provider = new(conn);

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => provider.RenameAsync(badPath!, "/new"));
    }

    [Fact]
    public async Task DisposeAsyncOnFreshProviderDoesNotThrowAsync()
    {
        SshConnection conn = NewConnection();
        SftpProvider provider = new(conn);

        await provider.DisposeAsync();
        await conn.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsyncIsIdempotentAsync()
    {
        await using SshConnection conn = NewConnection();
        SftpProvider provider = new(conn);

        await provider.DisposeAsync();
        await provider.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsyncAfterDisposeThrowsAsync()
    {
        await using SshConnection conn = NewConnection();
        SftpProvider provider = new(conn);
        await provider.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.ConnectAsync());
    }
}
