namespace Harbor.Core.Enums;

/// <summary>
/// Type d'événement enregistré dans le journal d'audit local.
/// Le journal couvre toutes les actions sensibles (connexions, accès aux
/// secrets, modifications de profils, imports/exports) et sert à la
/// traçabilité côté utilisateur final.
/// </summary>
public enum AuditEventType
{
    /// <summary>Session distante ouverte avec succès.</summary>
    ConnectionOpened,

    /// <summary>Session distante fermée proprement.</summary>
    ConnectionClosed,

    /// <summary>Tentative de connexion ayant échoué (auth, réseau, timeout).</summary>
    ConnectionFailed,

    /// <summary>Un secret du keystore a été lu (mot de passe, clé privée, token).</summary>
    SecretRead,

    /// <summary>Un secret a été créé ou mis à jour dans le keystore.</summary>
    SecretWritten,

    /// <summary>Un secret a été supprimé du keystore.</summary>
    SecretDeleted,

    /// <summary>Un nouveau profil de connexion a été créé.</summary>
    ProfileCreated,

    /// <summary>Un profil de connexion a été modifié.</summary>
    ProfileModified,

    /// <summary>Un profil de connexion a été supprimé.</summary>
    ProfileDeleted,

    /// <summary>Des profils ont été importés depuis un fichier externe.</summary>
    ProfileImported,

    /// <summary>Des profils ont été exportés vers un fichier.</summary>
    ProfileExported,

    /// <summary>Un transfert de fichier a démarré.</summary>
    TransferStarted,

    /// <summary>Un transfert s'est terminé avec succès.</summary>
    TransferCompleted,

    /// <summary>Un transfert a échoué définitivement.</summary>
    TransferFailed,

    /// <summary>Le master password du keystore a été changé.</summary>
    MasterPasswordChanged,

    /// <summary>L'application a été verrouillée automatiquement pour inactivité.</summary>
    LockedByInactivity,

    /// <summary>L'application a été déverrouillée (master password saisi).</summary>
    Unlocked,
}
