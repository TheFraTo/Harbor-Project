namespace Harbor.Security.Crypto;

/// <summary>
/// Paramètres Argon2id utilisés par <see cref="KeyDerivation"/>.
/// </summary>
/// <param name="MemoryKb">Mémoire en kibioctets (KB). Défaut : 65536 (= 64 MiB).</param>
/// <param name="Iterations">Nombre de passes. Défaut : 3.</param>
/// <param name="Parallelism">Nombre de threads. Défaut : 4.</param>
/// <remarks>
/// Ces paramètres correspondent aux recommandations de
/// <c>harbor-architecture.md</c> §13.2 et offrent un bon équilibre
/// résistance / performance sur du matériel récent (≈ 200-400 ms par dérivation).
/// </remarks>
public sealed record KeyDerivationParameters(
    int MemoryKb = 65_536,
    int Iterations = 3,
    int Parallelism = 4)
{
    /// <summary>Paramètres recommandés par défaut.</summary>
    public static KeyDerivationParameters Default => new();
}
