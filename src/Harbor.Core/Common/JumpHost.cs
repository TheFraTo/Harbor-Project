namespace Harbor.Core.Common;

/// <summary>
/// Décrit un hôte intermédiaire (bastion) utilisé pour atteindre un serveur
/// non exposé directement. Les <see cref="JumpHost"/> peuvent être chaînés via
/// <see cref="NextJump"/> pour des architectures multi-niveaux (SSH → bastion 1 → bastion 2 → cible).
/// </summary>
/// <param name="Host">Nom DNS ou adresse IP du bastion.</param>
/// <param name="Port">Port SSH du bastion (22 par défaut).</param>
/// <param name="Username">Identifiant utilisé sur le bastion.</param>
/// <param name="AuthenticationKeyId">
/// Identifiant optionnel d'une clé SSH spécifique du keystore à utiliser sur le bastion.
/// Si <c>null</c>, la méthode d'authentification du profil parent est tentée.
/// </param>
/// <param name="NextJump">Maillon suivant de la chaîne, ou <c>null</c> si ce bastion atteint la cible.</param>
public sealed record JumpHost(
    string Host,
    int Port,
    string Username,
    Guid? AuthenticationKeyId,
    JumpHost? NextJump);
