using System.Text;

namespace Harbor.Protocols.Ssh.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("SSH integration")]
public sealed class SshShellIntegrationTests : IClassFixture<SshContainerFixture>
{
    private readonly SshContainerFixture _container;

    public SshShellIntegrationTests(SshContainerFixture container)
    {
        _container = container;
    }

    private SshConnection NewConnection() => SshConnection.WithPassword(
        _container.Host,
        _container.Port,
        SshContainerFixture.TestUsername,
        SshContainerFixture.TestPassword);

    [Fact]
    public async Task ExecuteAsyncEchoCapturesStdoutAndExitZeroAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);
        await shell.ConnectAsync();

        await using MemoryStream stdout = new();

        int exitCode = await shell.ExecuteAsync("echo bonjour", stdout);

        Assert.Equal(0, exitCode);
        string captured = Encoding.UTF8.GetString(stdout.ToArray()).Trim();
        Assert.Equal("bonjour", captured);
    }

    [Fact]
    public async Task ExecuteAsyncFalseReturnsNonZeroExitCodeAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);
        await shell.ConnectAsync();

        int exitCode = await shell.ExecuteAsync("false");

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task ExecuteAsyncCapturesStderrSeparatelyAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);
        await shell.ConnectAsync();

        await using MemoryStream stdout = new();
        await using MemoryStream stderr = new();

        int exitCode = await shell.ExecuteAsync(
            "echo on-stdout; >&2 echo on-stderr",
            stdout,
            stderr);

        Assert.Equal(0, exitCode);
        string outText = Encoding.UTF8.GetString(stdout.ToArray()).Trim();
        string errText = Encoding.UTF8.GetString(stderr.ToArray()).Trim();
        Assert.Equal("on-stdout", outText);
        Assert.Equal("on-stderr", errText);
    }

    [Fact]
    public async Task ExecuteAsyncOnUnknownCommandReturnsErrorAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);
        await shell.ConnectAsync();

        int exitCode = await shell.ExecuteAsync("commande-totalement-inexistante-12345");

        // 127 = command not found, mais on accepte n'importe quel code ≠ 0
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task ExecuteAsyncMultipleCommandsInARowOnSameShellAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SshShell shell = new(conn);
        await shell.ConnectAsync();

        await using MemoryStream out1 = new();
        await using MemoryStream out2 = new();

        int e1 = await shell.ExecuteAsync("echo first", out1);
        int e2 = await shell.ExecuteAsync("echo second", out2);

        Assert.Equal(0, e1);
        Assert.Equal(0, e2);
        Assert.Equal("first", Encoding.UTF8.GetString(out1.ToArray()).Trim());
        Assert.Equal("second", Encoding.UTF8.GetString(out2.ToArray()).Trim());
    }
}
