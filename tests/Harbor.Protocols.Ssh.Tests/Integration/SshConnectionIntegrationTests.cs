using Harbor.Core.Enums;
using Harbor.Core.Events;

namespace Harbor.Protocols.Ssh.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("SSH integration")]
public sealed class SshConnectionIntegrationTests : IClassFixture<SshContainerFixture>
{
    private readonly SshContainerFixture _container;

    public SshConnectionIntegrationTests(SshContainerFixture container)
    {
        _container = container;
    }

    [Fact]
    public async Task ConnectsWithCorrectPasswordAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword(
            _container.Host,
            _container.Port,
            SshContainerFixture.TestUsername,
            SshContainerFixture.TestPassword);

        await conn.ConnectAsync();

        Assert.True(conn.IsConnected);
    }

    [Fact]
    public async Task EmitsExpectedConnectionStateSequenceOnSuccessAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword(
            _container.Host,
            _container.Port,
            SshContainerFixture.TestUsername,
            SshContainerFixture.TestPassword);

        List<ConnectionState> seenStates = [];
        conn.ConnectionStateChanged += (_, args) => seenStates.Add(args.NewState);

        await conn.ConnectAsync();

        Assert.Contains(ConnectionState.Connecting, seenStates);
        Assert.Contains(ConnectionState.Connected, seenStates);
    }

    [Fact]
    public async Task FailsWithWrongPasswordAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword(
            _container.Host,
            _container.Port,
            SshContainerFixture.TestUsername,
            "definitely-not-the-right-password");

        ConnectionStateChangedEventArgs? lastFailure = null;
        conn.ConnectionStateChanged += (_, args) =>
        {
            if (args.NewState == ConnectionState.Failed)
            {
                lastFailure = args;
            }
        };

        _ = await Assert.ThrowsAnyAsync<Exception>(() => conn.ConnectAsync());

        Assert.False(conn.IsConnected);
        Assert.NotNull(lastFailure);
        Assert.Equal(ConnectionState.Failed, lastFailure.NewState);
    }

    [Fact]
    public async Task ReconnectsAfterDisconnectAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword(
            _container.Host,
            _container.Port,
            SshContainerFixture.TestUsername,
            SshContainerFixture.TestPassword);

        await conn.ConnectAsync();
        Assert.True(conn.IsConnected);

        await conn.DisconnectAsync();
        Assert.False(conn.IsConnected);

        await conn.ConnectAsync();
        Assert.True(conn.IsConnected);
    }

    [Fact]
    public async Task DisposeAsyncClosesActiveConnectionAsync()
    {
        SshConnection conn = SshConnection.WithPassword(
            _container.Host,
            _container.Port,
            SshContainerFixture.TestUsername,
            SshContainerFixture.TestPassword);

        await conn.ConnectAsync();
        await conn.DisposeAsync();

        Assert.False(conn.IsConnected);
    }
}
