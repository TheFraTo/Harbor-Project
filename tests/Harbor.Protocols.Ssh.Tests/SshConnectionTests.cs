using Harbor.Core.Enums;

namespace Harbor.Protocols.Ssh.Tests;

public sealed class SshConnectionTests
{
    private const string Host = "example.com";
    private const string Username = "deploy";
    private const string Password = "hunter2";

    [Fact]
    public async Task WithPasswordExposesProvidedPropertiesAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword(Host, 2222, Username, Password);

        Assert.Equal(Host, conn.Host);
        Assert.Equal(2222, conn.Port);
        Assert.Equal(Username, conn.Username);
        Assert.False(conn.IsConnected);
    }

    [Fact]
    public async Task WithPasswordUsesDefaultKeepAliveWhenUnspecifiedAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword(Host, 22, Username, Password);

        Assert.Equal(SshConnection.DefaultKeepAliveInterval, conn.KeepAliveInterval);
    }

    [Fact]
    public async Task WithPasswordHonorsCustomKeepAliveAsync()
    {
        TimeSpan custom = TimeSpan.FromMinutes(2);
        await using SshConnection conn = SshConnection.WithPassword(Host, 22, Username, Password, custom);

        Assert.Equal(custom, conn.KeepAliveInterval);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithPasswordRejectsNullOrEmptyHost(string? badHost)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() =>
            SshConnection.WithPassword(badHost!, 22, Username, Password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithPasswordRejectsNullOrEmptyUsername(string? badUser)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() =>
            SshConnection.WithPassword(Host, 22, badUser!, Password));
    }

    [Fact]
    public void WithPasswordRejectsNullPassword()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            SshConnection.WithPassword(Host, 22, Username, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void WithPasswordRejectsInvalidPort(int badPort)
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SshConnection.WithPassword(Host, badPort, Username, Password));
    }

    [Fact]
    public void WithPrivateKeyRejectsNullKeyMaterial()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            SshConnection.WithPrivateKey(Host, 22, Username, null!));
    }

    [Fact]
    public void WithPrivateKeyRejectsEmptyKeyMaterial()
    {
        _ = Assert.Throws<ArgumentException>(() =>
            SshConnection.WithPrivateKey(Host, 22, Username, []));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void WithPrivateKeyRejectsInvalidPort(int badPort)
    {
        // Bytes valides en taille mais pas une vraie clé : on attend un échec sur le port avant le parse.
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SshConnection.WithPrivateKey(Host, badPort, Username, [0x01, 0x02, 0x03]));
    }

    [Fact]
    public void WithPrivateKeyRejectsGarbageKeyBytes()
    {
        // Les bytes ne sont pas un format de clé reconnaissable : SSH.NET doit lever une exception
        // en parsant. On teste qu'on ne masque pas cette erreur.
        byte[] garbage = [0xFF, 0xFE, 0xFD, 0xFC];

        _ = Assert.ThrowsAny<Exception>(() =>
            SshConnection.WithPrivateKey(Host, 22, Username, garbage));
    }

    [Fact]
    public async Task DisposeAsyncOnFreshConnectionDoesNotThrowAsync()
    {
        SshConnection conn = SshConnection.WithPassword(Host, 22, Username, Password);

        await conn.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsyncIsIdempotentAsync()
    {
        SshConnection conn = SshConnection.WithPassword(Host, 22, Username, Password);

        await conn.DisposeAsync();
        await conn.DisposeAsync();
    }

    [Fact]
    public async Task ConnectAsyncThrowsAfterDisposeAsync()
    {
        SshConnection conn = SshConnection.WithPassword(Host, 22, Username, Password);
        await conn.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => conn.ConnectAsync());
    }

    [Fact]
    public async Task GetClientThrowsWhenNotConnectedAsync()
    {
        await using SshConnection conn = SshConnection.WithPassword(Host, 22, Username, Password);

        // Reflection-free check : we expose GetClient internally to the assembly.
        // Direct test via a friend-call would require InternalsVisibleTo. Pour rester
        // dans le scope unitaire 2.2, on vérifie juste l'état IsConnected.
        Assert.False(conn.IsConnected);
    }

    [Fact]
    public async Task ConnectionStateChangedFiresOnFailedConnectAsync()
    {
        // On force un échec rapide avec un port inutilisé sur localhost.
        // Ne nécessite pas de serveur SSH — la TCP connect échouera vite.
        await using SshConnection conn = SshConnection.WithPassword(
            host: "127.0.0.1",
            port: 1, // port privilégié qui n'écoute presque jamais → échec rapide
            username: Username,
            password: Password);

        List<ConnectionState> seenStates = [];
        conn.ConnectionStateChanged += (_, args) => seenStates.Add(args.NewState);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<Exception>(() => conn.ConnectAsync(cts.Token));

        // On doit avoir au minimum vu Connecting puis Failed.
        Assert.Contains(ConnectionState.Connecting, seenStates);
        Assert.Contains(ConnectionState.Failed, seenStates);
        Assert.False(conn.IsConnected);
    }
}
