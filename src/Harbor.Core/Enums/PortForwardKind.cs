namespace Harbor.Core.Enums;

/// <summary>
/// Type de port forwarding SSH.
/// </summary>
public enum PortForwardKind
{
    /// <summary>
    /// Local forward (<c>-L</c>). Un port local écoute et relaie le trafic vers
    /// un host/port accessible depuis la machine distante.
    /// </summary>
    Local,

    /// <summary>
    /// Remote forward (<c>-R</c>). Un port distant écoute et relaie le trafic
    /// vers un host/port accessible depuis la machine locale.
    /// </summary>
    Remote,

    /// <summary>
    /// Dynamic forward / SOCKS (<c>-D</c>). Un port local agit comme proxy SOCKS
    /// transférant dynamiquement les connexions vers les cibles demandées.
    /// </summary>
    Dynamic,
}
