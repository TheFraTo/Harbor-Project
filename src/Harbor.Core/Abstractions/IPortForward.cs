using Harbor.Core.Enums;

namespace Harbor.Core.Abstractions;

/// <summary>
/// Un tunnel SSH actif (local, remote ou dynamic). Produit par les méthodes
/// <c>CreateXxxForwardAsync</c> de <see cref="IRemoteShell"/>.
/// </summary>
public interface IPortForward : IAsyncDisposable
{
    /// <summary>Identifiant stable du tunnel (utile pour le tracking UI).</summary>
    Guid Id { get; }

    /// <summary>Type du forwarding (Local, Remote, Dynamic).</summary>
    PortForwardKind Kind { get; }

    /// <summary>Port local du tunnel.</summary>
    int LocalPort { get; }

    /// <summary>
    /// Host cible du forwarding Local/Remote, ou <c>null</c> pour un forwarding Dynamic
    /// (cible déterminée à la volée par le protocole SOCKS).
    /// </summary>
    string? RemoteHost { get; }

    /// <summary>
    /// Port cible du forwarding Local/Remote, ou <c>null</c> pour un forwarding Dynamic.
    /// </summary>
    int? RemotePort { get; }

    /// <summary><c>true</c> si le tunnel est actuellement actif.</summary>
    bool IsActive { get; }

    /// <summary>Arrête le tunnel proprement.</summary>
    Task StopAsync(CancellationToken ct = default);
}
