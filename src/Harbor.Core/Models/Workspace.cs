namespace Harbor.Core.Models;

/// <summary>
/// Regroupement logique de profils, snippets, notes — typiquement un projet ou un client.
/// Un workspace dispose d'une icône et d'une couleur custom pour faciliter l'identification
/// visuelle dans la sidebar.
/// </summary>
/// <param name="Id">Identifiant stable (PK SQLite).</param>
/// <param name="Name">Nom affiché (recherchable via command palette).</param>
/// <param name="Icon">Nom d'icône ou emoji, ou <c>null</c> pour l'icône par défaut.</param>
/// <param name="Color">Couleur d'accent au format <c>#RRGGBB</c>, ou <c>null</c> pour la couleur par défaut.</param>
/// <param name="ProfileIds">Liste des profils appartenant au workspace (ordre d'affichage préservé).</param>
/// <param name="Notes">Notes Markdown libres.</param>
/// <param name="CreatedAt">Date de création.</param>
/// <param name="UpdatedAt">Date de dernière modification.</param>
public sealed record Workspace(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    IReadOnlyList<Guid> ProfileIds,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
