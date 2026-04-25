namespace Harbor.Protocols.Ssh.Tests;

public sealed class SshConnectionJumpHostTests
{
    private static readonly SshEndpoint Bastion = new("bastion.example.com", 22, "jumper");
    private static readonly SshEndpoint Target = new("target.internal", 22, "deploy");
    private static readonly SshAuthProvider AnyAuth = new SshPasswordAuth("p");

    [Fact]
    public async Task WithJumpHostExposesTargetAsPublicHostNotBastionAsync()
    {
        await using SshConnection conn = SshConnection.WithJumpHost(
            Bastion, AnyAuth, Target, AnyAuth);

        Assert.Equal(Target.Host, conn.Host);
        Assert.Equal(Target.Port, conn.Port);
        Assert.Equal(Target.Username, conn.Username);
    }

    [Fact]
    public async Task WithJumpHostFlagsUsesJumpHostAsync()
    {
        await using SshConnection conn = SshConnection.WithJumpHost(
            Bastion, AnyAuth, Target, AnyAuth);

        Assert.True(conn.UsesJumpHost);
    }

    [Fact]
    public async Task WithoutJumpHostUsesJumpHostIsFalseAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword("h", 22, "u", "p");

        Assert.False(conn.UsesJumpHost);
    }

    [Fact]
    public void WithJumpHostRejectsNullBastion()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            SshConnection.WithJumpHost(null!, AnyAuth, Target, AnyAuth));
    }

    [Fact]
    public void WithJumpHostRejectsNullBastionAuth()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            SshConnection.WithJumpHost(Bastion, null!, Target, AnyAuth));
    }

    [Fact]
    public void WithJumpHostRejectsNullTarget()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            SshConnection.WithJumpHost(Bastion, AnyAuth, null!, AnyAuth));
    }

    [Fact]
    public void WithJumpHostRejectsNullTargetAuth()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            SshConnection.WithJumpHost(Bastion, AnyAuth, Target, null!));
    }

    [Fact]
    public void WithJumpHostRejectsEmptyBastionHost()
    {
        SshEndpoint badBastion = new("", 22, "jumper");
        _ = Assert.ThrowsAny<ArgumentException>(() =>
            SshConnection.WithJumpHost(badBastion, AnyAuth, Target, AnyAuth));
    }

    [Fact]
    public void WithJumpHostRejectsEmptyTargetHost()
    {
        SshEndpoint badTarget = new("", 22, "deploy");
        _ = Assert.ThrowsAny<ArgumentException>(() =>
            SshConnection.WithJumpHost(Bastion, AnyAuth, badTarget, AnyAuth));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void WithJumpHostRejectsInvalidBastionPort(int badPort)
    {
        SshEndpoint badBastion = new("bastion", badPort, "jumper");
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SshConnection.WithJumpHost(badBastion, AnyAuth, Target, AnyAuth));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void WithJumpHostRejectsInvalidTargetPort(int badPort)
    {
        SshEndpoint badTarget = new("target", badPort, "deploy");
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SshConnection.WithJumpHost(Bastion, AnyAuth, badTarget, AnyAuth));
    }

    [Fact]
    public async Task WithJumpHostUsesDefaultKeepAliveWhenUnspecifiedAsync()
    {
        await using SshConnection conn = SshConnection.WithJumpHost(
            Bastion, AnyAuth, Target, AnyAuth);

        Assert.Equal(SshConnection.DefaultKeepAliveInterval, conn.KeepAliveInterval);
    }

    [Fact]
    public async Task WithJumpHostHonorsCustomKeepAliveAsync()
    {
        TimeSpan custom = TimeSpan.FromMinutes(5);
        await using SshConnection conn = SshConnection.WithJumpHost(
            Bastion, AnyAuth, Target, AnyAuth, custom);

        Assert.Equal(custom, conn.KeepAliveInterval);
    }

    [Fact]
    public async Task DisposeAsyncOnJumpHostConnectionDoesNotThrowAsync()
    {
        SshConnection conn = SshConnection.WithJumpHost(
            Bastion, AnyAuth, Target, AnyAuth);

        await conn.DisposeAsync();
    }
}
