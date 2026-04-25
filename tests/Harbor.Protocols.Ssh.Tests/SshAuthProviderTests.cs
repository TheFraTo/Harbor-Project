namespace Harbor.Protocols.Ssh.Tests;

public sealed class SshAuthProviderTests
{
    [Fact]
    public void PasswordAuthExposesProvidedPassword()
    {
        SshPasswordAuth auth = new("hunter2");

        Assert.Equal("hunter2", auth.Password);
    }

    [Fact]
    public void PasswordAuthEqualityIsByValue()
    {
        SshPasswordAuth a = new("p1");
        SshPasswordAuth b = new("p1");
        SshPasswordAuth c = new("p2");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void KeyAuthExposesMaterialAndPassphrase()
    {
        byte[] material = [1, 2, 3, 4];
        SshKeyAuth auth = new(material, "secret");

        Assert.Same(material, auth.KeyMaterial);
        Assert.Equal("secret", auth.Passphrase);
    }

    [Fact]
    public void KeyAuthDefaultsPassphraseToNull()
    {
        SshKeyAuth auth = new([1, 2, 3]);

        Assert.Null(auth.Passphrase);
    }

    [Fact]
    public void PatternMatchingDiscriminatesProviders()
    {
        SshAuthProvider[] all =
        [
            new SshPasswordAuth("p"),
            new SshKeyAuth([1]),
        ];

        string[] tags = all.Select(p => p switch
        {
            SshPasswordAuth => "password",
            SshKeyAuth => "key",
            _ => "unknown",
        }).ToArray();

        Assert.Equal(["password", "key"], tags);
    }

    [Fact]
    public void EndpointEqualityIsByValue()
    {
        SshEndpoint a = new("host", 22, "user");
        SshEndpoint b = new("host", 22, "user");
        SshEndpoint c = new("host", 22, "other");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
