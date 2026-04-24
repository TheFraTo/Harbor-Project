namespace Harbor.Data.Migrations;

/// <summary>
/// Métadonnées d'une migration découverte dans les ressources embarquées.
/// </summary>
/// <param name="Version">Numéro de version monotone (ex : 1 pour <c>0001_Initial.sql</c>).</param>
/// <param name="Name">Nom descriptif extrait du fichier (ex : <c>Initial</c>).</param>
/// <param name="ResourceName">Nom complet de la ressource embarquée dans l'assembly.</param>
internal sealed record MigrationInfo(int Version, string Name, string ResourceName);
