using Harbor.Core.Common;

namespace Harbor.Protocols.Ssh.Tests;

public sealed class SshShellTests
{
    private static SshConnection NewConnection() =>
        SshConnection.WithPassword("example.com", 22, "deploy", "hunter2");

    [Fact]
    public async Task ConstructorRejectsNullConnectionAsync()
    {
        await Task.Yield();
        _ = Assert.Throws<ArgumentNullException>(() => new SshShell(null!));
    }

    [Fact]
    public async Task IsConnectedIsFalseAfterConstructionAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);

        Assert.False(shell.IsConnected);
    }

    [Fact]
    public async Task ExecuteAsyncBeforeConnectThrowsInvalidOperationAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => shell.ExecuteAsync("uptime"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ExecuteAsyncRejectsNullOrEmptyCommandAsync(string? badCommand)
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => shell.ExecuteAsync(badCommand!));
    }

    [Fact]
    public async Task StartInteractiveSessionAsyncBeforeConnectThrowsInvalidOperationAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);

        // Depuis la brique 2.6, StartInteractiveSessionAsync est implémentée et
        // lève InvalidOperationException si on n'est pas encore connecté
        // (au lieu du précédent NotImplementedException de la brique 2.5).
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => shell.StartInteractiveSessionAsync(TerminalSize.Default));
    }

    [Fact]
    public async Task CreateLocalForwardAsyncThrowsNotImplementedForBrick25Async()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);

        _ = await Assert.ThrowsAsync<NotImplementedException>(
            () => shell.CreateLocalForwardAsync(localPort: 8080, remoteHost: "internal.example.com", remotePort: 80));
    }

    [Fact]
    public async Task CreateRemoteForwardAsyncThrowsNotImplementedForBrick25Async()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);

        _ = await Assert.ThrowsAsync<NotImplementedException>(
            () => shell.CreateRemoteForwardAsync(remotePort: 8081, localHost: "127.0.0.1", localPort: 80));
    }

    [Fact]
    public async Task CreateDynamicForwardAsyncThrowsNotImplementedForBrick25Async()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);

        _ = await Assert.ThrowsAsync<NotImplementedException>(
            () => shell.CreateDynamicForwardAsync(localPort: 1080));
    }

    [Fact]
    public async Task DisposeAsyncOnFreshShellDoesNotThrowAsync()
    {
        SshConnection conn = NewConnection();
        SshShell shell = new(conn);

        await shell.DisposeAsync();
        await conn.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsyncIsIdempotentAsync()
    {
        await using SshConnection conn = NewConnection();
        SshShell shell = new(conn);

        await shell.DisposeAsync();
        await shell.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsyncAfterDisposeThrowsAsync()
    {
        await using SshConnection conn = NewConnection();
        SshShell shell = new(conn);
        await shell.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => shell.ConnectAsync());
    }
}
