using System.Text;
using Harbor.Core.Models;

namespace Harbor.Protocols.Ssh.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("SSH integration")]
public sealed class SftpProviderIntegrationTests : IClassFixture<SshContainerFixture>
{
    private readonly SshContainerFixture _container;

    public SftpProviderIntegrationTests(SshContainerFixture container)
    {
        _container = container;
    }

    private SshConnection NewConnection() => SshConnection.WithPassword(
        _container.Host,
        _container.Port,
        SshContainerFixture.TestUsername,
        SshContainerFixture.TestPassword);

    [Fact]
    public async Task ConnectsAndListsHomeDirectoryAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider sftp = new(conn);
        await sftp.ConnectAsync();

        IReadOnlyList<RemoteFile> entries = await sftp.ListAsync("/config");

        Assert.True(sftp.IsConnected);
        Assert.NotNull(entries);
    }

    [Fact]
    public async Task UploadDownloadRoundTripsBytesAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider sftp = new(conn);
        await sftp.ConnectAsync();

        const string remotePath = "/config/harbor-roundtrip.txt";
        byte[] payload = Encoding.UTF8.GetBytes("Bonjour Harbor 🌍");

        // Upload
        await using (Stream upload = await sftp.OpenWriteAsync(remotePath))
        {
            await upload.WriteAsync(payload);
        }

        // Download
        byte[] downloaded;
        await using (Stream download = await sftp.OpenReadAsync(remotePath))
        await using (MemoryStream buffer = new())
        {
            await download.CopyToAsync(buffer);
            downloaded = buffer.ToArray();
        }

        Assert.Equal(payload, downloaded);

        // Cleanup
        await sftp.DeleteAsync(remotePath);
    }

    [Fact]
    public async Task StatReturnsAccurateMetadataAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider sftp = new(conn);
        await sftp.ConnectAsync();

        const string path = "/config/harbor-stat.txt";
        byte[] payload = "12345"u8.ToArray();

        await using (Stream upload = await sftp.OpenWriteAsync(path))
        {
            await upload.WriteAsync(payload);
        }

        RemoteFile info = await sftp.StatAsync(path);

        Assert.Equal(5, info.Size);
        Assert.False(info.IsDirectory);
        Assert.NotNull(info.LastModified);

        await sftp.DeleteAsync(path);
    }

    [Fact]
    public async Task CreateDirectoryThenDeleteAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider sftp = new(conn);
        await sftp.ConnectAsync();

        const string dir = "/config/harbor-test-dir";
        await sftp.CreateDirectoryAsync(dir);

        RemoteFile info = await sftp.StatAsync(dir);
        Assert.True(info.IsDirectory);

        await sftp.DeleteAsync(dir);
    }

    [Fact]
    public async Task RenameMovesFileAtomicallyAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider sftp = new(conn);
        await sftp.ConnectAsync();

        const string src = "/config/harbor-src.txt";
        const string dst = "/config/harbor-dst.txt";

        await using (Stream upload = await sftp.OpenWriteAsync(src))
        {
            await upload.WriteAsync("hello"u8.ToArray());
        }

        await sftp.RenameAsync(src, dst);

        // Confirme que la cible existe et que la source a disparu
        RemoteFile info = await sftp.StatAsync(dst);
        Assert.False(info.IsDirectory);
        _ = await Assert.ThrowsAnyAsync<Exception>(() => sftp.StatAsync(src));

        await sftp.DeleteAsync(dst);
    }

    [Fact]
    public async Task SetPermissionsModifiesUnixModeAsync()
    {
        await using SshConnection conn = NewConnection();
        await using SftpProvider sftp = new(conn);
        await sftp.ConnectAsync();

        const string path = "/config/harbor-perms.txt";
        await using (Stream upload = await sftp.OpenWriteAsync(path))
        {
            await upload.WriteAsync("data"u8.ToArray());
        }

        // 0o600 = UserRead | UserWrite seulement
        UnixFileMode targetMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        await sftp.SetPermissionsAsync(path, targetMode);

        RemoteFile info = await sftp.StatAsync(path);
        Assert.NotNull(info.Permissions);

        // Vérifie que le bit UserRead est présent et qu'OtherRead n'est PAS présent.
        Assert.True(info.Permissions.Value.HasFlag(UnixFileMode.UserRead));
        Assert.False(info.Permissions.Value.HasFlag(UnixFileMode.OtherRead));

        await sftp.DeleteAsync(path);
    }
}
