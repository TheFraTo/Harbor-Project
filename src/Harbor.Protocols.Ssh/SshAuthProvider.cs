namespace Harbor.Protocols.Ssh;

/// <summary>
/// Mode d'authentification fourni à <see cref="SshConnection"/>, déjà déchiffré
/// (la couche services s'occupe de demander au keystore avant d'instancier).
/// </summary>
public abstract record SshAuthProvider;

/// <summary>Authentification par mot de passe en clair.</summary>
/// <param name="Password">Mot de passe (à manipuler en mémoire le moins longtemps possible).</param>
public sealed record SshPasswordAuth(string Password) : SshAuthProvider;

/// <summary>Authentification par clé privée Ed25519, RSA ou ECDSA.</summary>
/// <param name="KeyMaterial">Bytes de la clé privée au format OpenSSH ou PEM.</param>
/// <param name="Passphrase">Passphrase qui protège la clé, ou <c>null</c> si la clé n'en a pas.</param>
public sealed record SshKeyAuth(byte[] KeyMaterial, string? Passphrase = null) : SshAuthProvider;

/// <summary>
/// Triplet (host, port, username) identifiant un endpoint SSH. Utilisé par
/// <see cref="SshConnection.WithJumpHost"/> pour décrire bastion et cible
/// de manière concise.
/// </summary>
/// <param name="Host">Nom DNS ou adresse IP.</param>
/// <param name="Port">Port SSH (22 par défaut côté usage).</param>
/// <param name="Username">Identifiant utilisateur sur cet endpoint.</param>
public sealed record SshEndpoint(string Host, int Port, string Username);
