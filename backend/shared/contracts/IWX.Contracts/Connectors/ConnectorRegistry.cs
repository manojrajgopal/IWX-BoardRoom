namespace IWX.Contracts.Connectors;

/// <summary>
/// Authoritative registry for every platform connector in the IWX Boardroom.
/// Each connector is an independent worker service that exposes:
///   - REST on HttpPort  (health, credentials, admin)
///   - gRPC on GrpcPort  (the unified ConnectorService contract)
/// </summary>
public sealed record ConnectorDescriptor(
    string Key,           // routing key (e.g. "instagram")
    string DisplayName,
    string MongoDb,       // Mongo database for credentials / cache
    int HttpPort,
    int GrpcPort,
    string ServiceHost,   // docker-compose service hostname
    string Icon);

public static class ConnectorRegistry
{
    public static readonly ConnectorDescriptor Instagram = new(
        Connectors.Instagram, "Instagram", "iwx_connector_instagram", 8200, 9200, "instagram-connector", "pi-instagram");

    public static readonly ConnectorDescriptor YouTube = new(
        Connectors.YouTube, "YouTube", "iwx_connector_youtube", 8201, 9201, "youtube-connector", "pi-youtube");

    public static readonly ConnectorDescriptor LinkedIn = new(
        Connectors.LinkedIn, "LinkedIn", "iwx_connector_linkedin", 8202, 9202, "linkedin-connector", "pi-linkedin");

    public static readonly ConnectorDescriptor Twitter = new(
        Connectors.Twitter, "X / Twitter", "iwx_connector_twitter", 8203, 9203, "twitter-connector", "pi-twitter");

    public static readonly ConnectorDescriptor Facebook = new(
        Connectors.Facebook, "Facebook", "iwx_connector_facebook", 8204, 9204, "facebook-connector", "pi-facebook");

    public static readonly ConnectorDescriptor Reddit = new(
        Connectors.Reddit, "Reddit", "iwx_connector_reddit", 8205, 9205, "reddit-connector", "pi-reddit");

    public static readonly ConnectorDescriptor WhatsApp = new(
        Connectors.WhatsApp, "WhatsApp", "iwx_connector_whatsapp", 8206, 9206, "whatsapp-connector", "pi-whatsapp");

    public static readonly ConnectorDescriptor Email = new(
        Connectors.Email, "Email (SMTP/IMAP)", "iwx_connector_email", 8207, 9207, "email-connector", "pi-envelope");

    public static readonly ConnectorDescriptor Websites = new(
        Connectors.Websites, "Websites (HTTP/Scrape)", "iwx_connector_websites", 8208, 9208, "websites-connector", "pi-globe");

    public static readonly IReadOnlyList<ConnectorDescriptor> All = new[]
    {
        Instagram, YouTube, LinkedIn, Twitter, Facebook,
        Reddit, WhatsApp, Email, Websites
    };

    public static ConnectorDescriptor ByKey(string key) =>
        All.FirstOrDefault(c => c.Key == key)
        ?? throw new ArgumentException($"Unknown connector key '{key}'", nameof(key));
}
