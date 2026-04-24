using Harbor.Core.Enums;

namespace Harbor.Core.Tests.Enums;

public sealed class EnumDistinctValuesTests
{
    [Theory]
    [InlineData(typeof(ProtocolKind))]
    [InlineData(typeof(KeyAlgorithm))]
    [InlineData(typeof(TransferDirection))]
    [InlineData(typeof(TransferStatus))]
    [InlineData(typeof(AuditEventType))]
    [InlineData(typeof(ConnectionState))]
    [InlineData(typeof(PortForwardKind))]
    public void EnumValuesAreDistinct(Type enumType)
    {
        Array values = Enum.GetValues(enumType);
        HashSet<int> seen = [];

        foreach (object? value in values)
        {
            int numeric = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(seen.Add(numeric), $"Valeur dupliquée {numeric} dans {enumType.Name}");
        }
    }

    [Fact]
    public void ProtocolKindCoversAllDocumentedProtocols()
    {
        HashSet<string> names = [.. Enum.GetNames<ProtocolKind>()];

        string[] expected =
        [
            nameof(ProtocolKind.Ssh),
            nameof(ProtocolKind.Sftp),
            nameof(ProtocolKind.Ftp),
            nameof(ProtocolKind.Ftps),
            nameof(ProtocolKind.WebDav),
            nameof(ProtocolKind.S3),
            nameof(ProtocolKind.AzureBlob),
            nameof(ProtocolKind.GoogleCloudStorage),
            nameof(ProtocolKind.Docker),
            nameof(ProtocolKind.Kubernetes),
        ];

        foreach (string name in expected)
        {
            Assert.Contains(name, names);
        }
    }
}
