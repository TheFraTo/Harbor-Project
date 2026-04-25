using Harbor.Core.Common;
using Harbor.Core.Enums;
using Harbor.Core.Models;
using Harbor.Data.Repositories;
using Harbor.Data.Tests.Fixtures;

namespace Harbor.Data.Tests.Repositories;

public sealed class ProfileRepositoryTests
{
    [Fact]
    public async Task InsertThenGetByIdRoundTripsSshProfileWithJumpHostAndKeyAuth()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        ProfileRepository repo = new(fixture.Context);

        Guid keyId = Guid.NewGuid();
        SshConnectionDetails ssh = new(
            Host: "target.internal",
            Port: 22,
            Username: "deploy",
            Jump: new JumpHost("bastion.example.com", 2222, "jumper", null, null));

        KeyAuth auth = new(keyId, Passphrase: null);

        Profile original = new(
            Id: Guid.NewGuid(),
            Name: "Prod web 1",
            Protocol: ProtocolKind.Ssh,
            Connection: ssh,
            Auth: auth,
            Tags: ["prod", "web"],
            ParentFolderId: null,
            EnvVars: new Dictionary<string, string> { ["LANG"] = "fr_FR.UTF-8" },
            PostConnectScript: "tmux a || tmux",
            Notes: "VPS principal",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            LastUsedAt: null);

        await repo.InsertAsync(original);
        Profile? retrieved = await repo.GetByIdAsync(original.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(ProtocolKind.Ssh, retrieved.Protocol);

        SshConnectionDetails retrievedSsh = Assert.IsType<SshConnectionDetails>(retrieved.Connection);
        Assert.Equal("target.internal", retrievedSsh.Host);
        Assert.Equal(22, retrievedSsh.Port);
        Assert.Equal("deploy", retrievedSsh.Username);
        Assert.NotNull(retrievedSsh.Jump);
        Assert.Equal("bastion.example.com", retrievedSsh.Jump.Host);
        Assert.Equal(2222, retrievedSsh.Jump.Port);

        KeyAuth retrievedAuth = Assert.IsType<KeyAuth>(retrieved.Auth);
        Assert.Equal(keyId, retrievedAuth.KeyId);

        Assert.Equal(["prod", "web"], retrieved.Tags);
        Assert.Equal("fr_FR.UTF-8", retrieved.EnvVars["LANG"]);
        Assert.Equal("tmux a || tmux", retrieved.PostConnectScript);
    }

    [Fact]
    public async Task InsertThenGetByIdRoundTripsS3ProfileWithAccessKeyAuth()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        ProfileRepository repo = new(fixture.Context);

        S3ConnectionDetails s3 = new(
            Endpoint: "https://s3.fr-par.scw.cloud",
            Region: "fr-par",
            BucketName: "backups",
            UsePathStyle: true);

        AccessKeyAuth auth = new(
            AccessKeyId: "SCW123",
            SecretAccessKey: new EncryptedString([1, 2, 3], [4, 5, 6], [7, 8, 9]));

        Profile original = new(
            Guid.NewGuid(), "Backups Scaleway", ProtocolKind.S3, s3, auth,
            [], null,
            new Dictionary<string, string>(),
            null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);

        await repo.InsertAsync(original);
        Profile? retrieved = await repo.GetByIdAsync(original.Id);

        Assert.NotNull(retrieved);
        S3ConnectionDetails retS3 = Assert.IsType<S3ConnectionDetails>(retrieved.Connection);
        Assert.Equal("fr-par", retS3.Region);
        Assert.True(retS3.UsePathStyle);

        AccessKeyAuth retAuth = Assert.IsType<AccessKeyAuth>(retrieved.Auth);
        Assert.Equal("SCW123", retAuth.AccessKeyId);
        Assert.Equal(new byte[] { 1, 2, 3 }, retAuth.SecretAccessKey.Nonce);
        Assert.Equal(new byte[] { 4, 5, 6 }, retAuth.SecretAccessKey.Ciphertext);
        Assert.Equal(new byte[] { 7, 8, 9 }, retAuth.SecretAccessKey.Tag);
    }

    [Fact]
    public async Task UpdateChangesPolymorphicConnectionAndAuth()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        ProfileRepository repo = new(fixture.Context);
        Guid id = Guid.NewGuid();

        Profile sshProfile = new(
            id, "Initial", ProtocolKind.Ssh,
            new SshConnectionDetails("h1", 22, "u", null),
            new PasswordAuth(new EncryptedString([1], [2], [3])),
            [], null, new Dictionary<string, string>(), null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
        await repo.InsertAsync(sshProfile);

        Profile docker = sshProfile with
        {
            Name = "Docker local",
            Protocol = ProtocolKind.Docker,
            Connection = new DockerConnectionDetails("unix:///var/run/docker.sock"),
            Auth = new AnonymousAuth(),
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        bool ok = await repo.UpdateAsync(docker);
        Assert.True(ok);

        Profile? r = await repo.GetByIdAsync(id);
        Assert.NotNull(r);
        Assert.Equal(ProtocolKind.Docker, r.Protocol);
        DockerConnectionDetails dc = Assert.IsType<DockerConnectionDetails>(r.Connection);
        Assert.Equal("unix:///var/run/docker.sock", dc.Endpoint);
        _ = Assert.IsType<AnonymousAuth>(r.Auth);
    }

    [Fact]
    public async Task WorkspaceIdRoundTripsAndGetByWorkspaceFiltersProperly()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository workspaceRepo = new(fixture.Context);
        ProfileRepository profileRepo = new(fixture.Context);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Workspace ws = new(Guid.NewGuid(), "Client X", null, null, [], null, now, now);
        await workspaceRepo.InsertAsync(ws);

        Profile inWs = BuildSshProfile() with { WorkspaceId = ws.Id };
        Profile orphan = BuildSshProfile();
        await profileRepo.InsertAsync(inWs);
        await profileRepo.InsertAsync(orphan);

        Profile? retrieved = await profileRepo.GetByIdAsync(inWs.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(ws.Id, retrieved.WorkspaceId);

        IReadOnlyList<Profile> inWorkspace = await profileRepo.GetByWorkspaceAsync(ws.Id);
        Assert.Single(inWorkspace);
        Assert.Equal(inWs.Id, inWorkspace[0].Id);

        IReadOnlyList<Guid> ids = await profileRepo.GetIdsByWorkspaceAsync(ws.Id);
        Assert.Equal([inWs.Id], ids);
    }

    [Fact]
    public async Task DeletingWorkspaceNullifiesProfileWorkspaceId()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        WorkspaceRepository workspaceRepo = new(fixture.Context);
        ProfileRepository profileRepo = new(fixture.Context);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Workspace ws = new(Guid.NewGuid(), "Temp", null, null, [], null, now, now);
        await workspaceRepo.InsertAsync(ws);

        Profile p = BuildSshProfile() with { WorkspaceId = ws.Id };
        await profileRepo.InsertAsync(p);

        _ = await workspaceRepo.DeleteAsync(ws.Id);

        Profile? retrieved = await profileRepo.GetByIdAsync(p.Id);
        Assert.NotNull(retrieved);
        Assert.Null(retrieved.WorkspaceId);
    }

    private static Profile BuildSshProfile() => new(
        Id: Guid.NewGuid(),
        Name: "Probe",
        Protocol: ProtocolKind.Ssh,
        Connection: new SshConnectionDetails("h", 22, "u", null),
        Auth: new AgentAuth(),
        Tags: [],
        ParentFolderId: null,
        EnvVars: new Dictionary<string, string>(),
        PostConnectScript: null,
        Notes: null,
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow,
        LastUsedAt: null);

    [Fact]
    public async Task DeleteRemovesProfile()
    {
        await using TempDatabaseFixture fixture = await TempDatabaseFixture.CreateAsync();
        ProfileRepository repo = new(fixture.Context);
        Guid id = Guid.NewGuid();

        Profile p = new(
            id, "À jeter", ProtocolKind.Ssh,
            new SshConnectionDetails("h", 22, "u", null),
            new AgentAuth(),
            [], null, new Dictionary<string, string>(), null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
        await repo.InsertAsync(p);

        Assert.True(await repo.DeleteAsync(id));
        Assert.Null(await repo.GetByIdAsync(id));
    }
}
