namespace Harbor.Core.Models;

/// <summary>
/// Un snippet de commande shell réutilisable, éventuellement paramétré.
/// Les variables sont référencées via la syntaxe <c>${nom}</c> dans <see cref="Command"/>
/// et peuvent être saisies par l'utilisateur ou prises sur les valeurs par défaut.
/// </summary>
/// <param name="Id">Identifiant stable (PK SQLite).</param>
/// <param name="Name">Nom affiché.</param>
/// <param name="Description">Description libre, ou <c>null</c>.</param>
/// <param name="Command">Commande(s) shell avec placeholders <c>${var}</c>.</param>
/// <param name="Variables">Définition des variables attendues.</param>
/// <param name="Tags">Étiquettes pour filtrer et rechercher.</param>
/// <param name="CreatedAt">Date de création.</param>
public sealed record Snippet(
    Guid Id,
    string Name,
    string? Description,
    string Command,
    IReadOnlyList<SnippetVariable> Variables,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt);

/// <summary>
/// Définition d'une variable utilisable dans un <see cref="Snippet"/>.
/// </summary>
/// <param name="Name">Nom de la variable (sans <c>${...}</c>).</param>
/// <param name="DefaultValue">Valeur par défaut proposée à l'utilisateur, ou <c>null</c>.</param>
/// <param name="Description">Aide contextuelle, ou <c>null</c>.</param>
public sealed record SnippetVariable(
    string Name,
    string? DefaultValue,
    string? Description);
