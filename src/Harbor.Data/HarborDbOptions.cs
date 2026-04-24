namespace Harbor.Data;

/// <summary>
/// Options de configuration pour <see cref="HarborDbContext"/>.
/// </summary>
/// <param name="DatabasePath">
/// Chemin absolu vers le fichier SQLite. Créé s'il n'existe pas.
/// Le fichier doit être dans un dossier avec droits d'écriture pour l'utilisateur courant.
/// </param>
/// <param name="EncryptionKey">
/// Clé de chiffrement SQLCipher de 32 octets (AES-256). Typiquement dérivée
/// du master password de l'utilisateur via Argon2id (cf. <c>Harbor.Security</c>).
/// </param>
/// <param name="EnableWal">
/// Active le journal WAL (Write-Ahead Logging) pour supporter des lectures
/// concurrentes pendant une écriture. Par défaut <c>true</c>.
/// </param>
/// <param name="BusyTimeout">
/// Délai pendant lequel SQLite attend si la base est verrouillée par un autre
/// writer avant de renvoyer <c>SQLITE_BUSY</c>. 5 secondes par défaut.
/// </param>
public sealed record HarborDbOptions(
    string DatabasePath,
    byte[] EncryptionKey,
    bool EnableWal = true,
    TimeSpan? BusyTimeout = null)
{
    /// <summary>Délai par défaut si aucun n'est fourni : 5 secondes.</summary>
    public TimeSpan EffectiveBusyTimeout => BusyTimeout ?? TimeSpan.FromSeconds(5);
}
