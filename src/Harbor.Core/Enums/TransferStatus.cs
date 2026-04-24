namespace Harbor.Core.Enums;

/// <summary>
/// État courant d'un transfert dans la file d'attente persistante.
/// Les transitions possibles sont gérées par le <c>TransferEngine</c>.
/// </summary>
public enum TransferStatus
{
    /// <summary>En file d'attente, pas encore démarré.</summary>
    Queued,

    /// <summary>Transfert en cours d'exécution par un worker.</summary>
    InProgress,

    /// <summary>Transfert temporairement suspendu par l'utilisateur.</summary>
    Paused,

    /// <summary>Transfert terminé avec succès.</summary>
    Completed,

    /// <summary>
    /// Transfert échoué après épuisement des tentatives de retry.
    /// Reste dans la queue pour permettre un retry manuel.
    /// </summary>
    Failed,

    /// <summary>
    /// Transfert interrompu de manière inattendue (crash de l'application,
    /// perte de connexion brutale). Candidat à la reprise automatique.
    /// </summary>
    Interrupted,

    /// <summary>Transfert annulé explicitement par l'utilisateur.</summary>
    Cancelled,
}
