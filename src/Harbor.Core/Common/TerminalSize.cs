namespace Harbor.Core.Common;

/// <summary>
/// Dimensions d'un terminal en nombre de colonnes et de lignes de caractères.
/// Utilisé pour dimensionner initialement une session PTY et pour propager
/// les redimensionnements (SIGWINCH) via <c>IInteractiveSession</c>.
/// </summary>
public readonly record struct TerminalSize(int Columns, int Rows)
{
    /// <summary>Dimensions par défaut d'un terminal VT (80 colonnes, 24 lignes).</summary>
    public static TerminalSize Default => new(80, 24);
}
