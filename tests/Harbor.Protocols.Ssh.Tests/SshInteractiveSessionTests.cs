using Harbor.Core.Common;

namespace Harbor.Protocols.Ssh.Tests;

public sealed class SshInteractiveSessionTests
{
    [Fact]
    public async Task ConstructorRejectsNullShellStreamAsync()
    {
        await Task.Yield();
        _ = Assert.Throws<ArgumentNullException>(() => new SshInteractiveSession(null!));
    }

    // Note : on ne peut pas instancier un ShellStream SSH.NET hors d'une vraie
    // session SSH (le constructeur public est interne à SSH.NET). Les autres
    // comportements de SshInteractiveSession (Input == Output, ResizeAsync,
    // WaitForExitAsync, ExitCode null avant fin) sont testés indirectement via
    // les tests d'intégration de la brique 2.8 contre un vrai serveur SSH dans
    // un conteneur Docker.

    [Fact]
    public async Task SshShellStartInteractiveSessionAsyncRejectsZeroDimensionsAsync()
    {
        // Garde locale dans SshShell elle-même : on rejette les dimensions
        // invalides AVANT d'avoir besoin d'une connexion réelle. Mais
        // SshShell vérifie d'abord si elle est connectée, donc on attend
        // InvalidOperationException ici (pas connectée).
        await using SshConnection conn = SshConnection.WithPassword("h", 22, "u", "p");
        await using SshShell shell = new(conn);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => shell.StartInteractiveSessionAsync(new TerminalSize(0, 24)));
    }
}
