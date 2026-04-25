using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Harbor.Protocols.Ssh.Tests.Integration;

/// <summary>
/// Démarre un conteneur Docker <c>linuxserver/openssh-server</c> pour les
/// tests d'intégration. Configure un utilisateur <c>tester</c> avec un mot
/// de passe connu, expose le port SSH 22 sur un port local aléatoire.
/// </summary>
/// <remarks>
/// <para>
/// Si Docker n'est pas disponible (machine de dev sans Docker Desktop ou
/// runner CI sans Docker), <see cref="InitializeAsync"/> propage l'exception ;
/// les tests qui dépendent de cette fixture sont alors marqués Failed dans
/// xunit. La CI filtre déjà la catégorie <c>Integration</c> hors de Linux.
/// </para>
/// <para>
/// Une seule instance de conteneur est partagée par classe de test (pattern
/// xunit <c>IClassFixture</c>) pour éviter de payer 2-3 secondes de démarrage
/// par test.
/// </para>
/// </remarks>
public sealed class SshContainerFixture : IAsyncLifetime
{
    /// <summary>Nom de l'utilisateur configuré dans le conteneur.</summary>
    public const string TestUsername = "tester";

    /// <summary>Mot de passe de l'utilisateur de test.</summary>
    public const string TestPassword = "testpass-harbor-2026";

    private const int InternalSshPort = 2222;
    private IContainer? _container;

    /// <summary>Hôte joignable pour le SSH (typiquement <c>localhost</c>).</summary>
    public string Host => _container?.Hostname
        ?? throw new InvalidOperationException("Le conteneur n'est pas démarré.");

    /// <summary>Port local mappé vers le port SSH du conteneur.</summary>
    public int Port => _container?.GetMappedPublicPort(InternalSshPort)
        ?? throw new InvalidOperationException("Le conteneur n'est pas démarré.");

    /// <summary>Crée et démarre le conteneur. Bloque jusqu'à ce que SSH soit prêt.</summary>
    public async Task InitializeAsync()
    {
        // L'image linuxserver/openssh-server log "[ls.io-init] done." une fois
        // le bootstrap terminé et SSH listening sur le port 2222 en interne.
        _container = new ContainerBuilder("lscr.io/linuxserver/openssh-server:latest")
            .WithEnvironment("PUID", "1000")
            .WithEnvironment("PGID", "1000")
            .WithEnvironment("TZ", "Etc/UTC")
            .WithEnvironment("USER_NAME", TestUsername)
            .WithEnvironment("USER_PASSWORD", TestPassword)
            .WithEnvironment("PASSWORD_ACCESS", "true")
            .WithEnvironment("SUDO_ACCESS", "false")
            .WithPortBinding(InternalSshPort, assignRandomHostPort: true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("\\[ls\\.io-init\\] done\\."))
            .Build();

        await _container.StartAsync().ConfigureAwait(false);
    }

    /// <summary>Stoppe et supprime le conteneur.</summary>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
