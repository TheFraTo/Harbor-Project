using Harbor.Core.Common;
using Harbor.Core.Models;

namespace Harbor.Core.Tests.Models;

public sealed class ConnectionDetailsTests
{
    [Fact]
    public void SshVariantExposesHostPortAndUsername()
    {
        ConnectionDetails conn = new SshConnectionDetails("prod.example.com", 22, "deploy", null);

        Assert.IsType<SshConnectionDetails>(conn);
        SshConnectionDetails ssh = (SshConnectionDetails)conn;
        Assert.Equal("prod.example.com", ssh.Host);
        Assert.Equal(22, ssh.Port);
        Assert.Equal("deploy", ssh.Username);
        Assert.Null(ssh.Jump);
    }

    [Fact]
    public void S3VariantSupportsCompatibleEndpoint()
    {
        ConnectionDetails conn = new S3ConnectionDetails(
            Endpoint: "https://s3.fr-par.scw.cloud",
            Region: "fr-par",
            BucketName: "backups-harbor",
            UsePathStyle: true);

        S3ConnectionDetails s3 = Assert.IsType<S3ConnectionDetails>(conn);
        Assert.Equal("fr-par", s3.Region);
        Assert.True(s3.UsePathStyle);
    }

    [Fact]
    public void JumpHostsCanBeChainedMultipleLevels()
    {
        JumpHost innermost = new("bastion-inner", 22, "jumper", null, null);
        JumpHost outer = new("bastion-outer", 22, "jumper", null, innermost);
        SshConnectionDetails ssh = new("target.internal", 22, "root", outer);

        Assert.NotNull(ssh.Jump);
        Assert.Equal("bastion-outer", ssh.Jump.Host);
        Assert.NotNull(ssh.Jump.NextJump);
        Assert.Equal("bastion-inner", ssh.Jump.NextJump.Host);
        Assert.Null(ssh.Jump.NextJump.NextJump);
    }

    [Theory]
    [InlineData(typeof(SshConnectionDetails))]
    [InlineData(typeof(FtpConnectionDetails))]
    [InlineData(typeof(S3ConnectionDetails))]
    [InlineData(typeof(AzureBlobConnectionDetails))]
    [InlineData(typeof(GoogleCloudStorageConnectionDetails))]
    [InlineData(typeof(WebDavConnectionDetails))]
    [InlineData(typeof(DockerConnectionDetails))]
    [InlineData(typeof(KubernetesConnectionDetails))]
    [InlineData(typeof(TelnetConnectionDetails))]
    [InlineData(typeof(SerialPortConnectionDetails))]
    [InlineData(typeof(MoshConnectionDetails))]
    public void AllVariantsDeriveFromConnectionDetails(Type variant)
    {
        Assert.True(typeof(ConnectionDetails).IsAssignableFrom(variant));
    }

    [Fact]
    public void PatternMatchingDiscriminatesVariants()
    {
        ConnectionDetails[] variants =
        [
            new SshConnectionDetails("h", 22, "u", null),
            new S3ConnectionDetails(null, "us-east-1", "b", false),
            new DockerConnectionDetails("unix:///var/run/docker.sock"),
        ];

        string[] labels = variants.Select(v => v switch
        {
            SshConnectionDetails => "ssh",
            S3ConnectionDetails => "s3",
            DockerConnectionDetails => "docker",
            _ => "unknown",
        }).ToArray();

        Assert.Equal(["ssh", "s3", "docker"], labels);
    }
}
