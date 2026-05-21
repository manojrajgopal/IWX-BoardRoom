namespace IWX.Contracts.Connectors;

/// <summary>
/// Stable keys for every platform connector. Used for routing, queue names,
/// docker hostnames, and gRPC client lookup.
/// </summary>
public static class Connectors
{
    public const string Instagram = "instagram";
    public const string YouTube = "youtube";
    public const string LinkedIn = "linkedin";
    public const string Twitter = "twitter";
    public const string Facebook = "facebook";
    public const string Reddit = "reddit";
    public const string WhatsApp = "whatsapp";
    public const string Email = "email";
    public const string Websites = "websites";
}

public static class ConnectorQueues
{
    /// <summary>Inbound event fanout — connectors publish detected events here (mentions, DMs, comments, replies).</summary>
    public const string ConnectorEvent = "iwx.connector.event";
}
