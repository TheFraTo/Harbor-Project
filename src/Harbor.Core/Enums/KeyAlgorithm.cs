namespace Harbor.Core.Enums;

/// <summary>
/// Algorithme cryptographique d'une clé SSH gérée par Harbor.
/// </summary>
public enum KeyAlgorithm
{
    /// <summary>
    /// Ed25519 — algorithme moderne à courbe elliptique. Défaut recommandé
    /// pour toute nouvelle clé générée via Harbor (clés courtes, performantes,
    /// largement supporté depuis OpenSSH 6.5).
    /// </summary>
    Ed25519,

    /// <summary>
    /// RSA — algorithme historique. Harbor génère par défaut des clés RSA
    /// 4096 bits lorsque cette option est choisie explicitement, typiquement
    /// pour la compatibilité avec du matériel ou des serveurs anciens.
    /// </summary>
    Rsa,

    /// <summary>
    /// ECDSA (courbes NIST P-256, P-384, P-521). Moins recommandé qu'Ed25519
    /// mais parfois imposé par des politiques de sécurité existantes.
    /// </summary>
    Ecdsa,
}
