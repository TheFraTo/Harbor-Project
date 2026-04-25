using Harbor.Core.Enums;

namespace Harbor.Core.Models;

/// <summary>
/// Un profil de connexion Harbor : configuration nommée et réutilisable
/// ciblant un serveur, un bucket, un conteneur ou une autre ressource distante.
/// </summary>
/// <param name="Id">Identifiant stable (PK SQLite).</param>
/// <param name="Name">Nom affiché (recherchable via command palette).</param>
/// <param name="Protocol">Type de protocole — doit cohabiter avec le type concret de <paramref name="Connection"/>.</param>
/// <param name="Connection">Détails spécifiques au protocole.</param>
/// <param name="Auth">Méthode d'authentification.</param>
/// <param name="Tags">Étiquettes libres utilisées pour le filtrage et la recherche.</param>
/// <param name="ParentFolderId">
/// Référence optionnelle à un dossier logique parent dans l'arborescence des profils
/// (à l'intérieur d'un workspace). <c>null</c> si profil à la racine.
/// </param>
/// <param name="EnvVars">Variables d'environnement à exporter sur la session distante.</param>
/// <param name="PostConnectScript">Script shell à exécuter automatiquement après connexion SSH, ou <c>null</c>.</param>
/// <param name="Notes">Notes Markdown libres (recherchables).</param>
/// <param name="CreatedAt">Date de création.</param>
/// <param name="UpdatedAt">Date de dernière modification.</param>
/// <param name="LastUsedAt">Date de dernière utilisation, ou <c>null</c> si jamais utilisé.</param>
/// <param name="WorkspaceId">
/// Workspace auquel le profil est rattaché, ou <c>null</c> si non rattaché.
/// FK vers <see cref="Workspace.Id"/> ; <c>ON DELETE SET NULL</c> côté base.
/// </param>
public sealed record Profile(
    Guid Id,
    string Name,
    ProtocolKind Protocol,
    ConnectionDetails Connection,
    AuthenticationMethod Auth,
    IReadOnlyList<string> Tags,
    Guid? ParentFolderId,
    IReadOnlyDictionary<string, string> EnvVars,
    string? PostConnectScript,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastUsedAt,
    Guid? WorkspaceId = null);
