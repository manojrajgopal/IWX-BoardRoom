using Grpc.Core;
using IWX.Contracts.Connectors;
using IWX.Contracts.Connectors.Grpc;
using IWX.Connectors.Worker.Infrastructure;

namespace IWX.Connectors.Worker.Grpc;

/// <summary>
/// Default gRPC implementation for any connector that hasn't shipped its
/// platform integration yet. Returns structured "not_implemented" responses
/// so calling departments can gracefully degrade.
///
/// Real connectors override individual methods and call the upstream API
/// (Meta Graph, YouTube Data API, X v2, LinkedIn, Reddit, SMTP/IMAP, etc.).
/// </summary>
public class StubConnectorService : ConnectorService.ConnectorServiceBase
{
    protected readonly ConnectorDescriptor Descriptor;
    protected readonly CredentialStore Credentials;

    public StubConnectorService(ConnectorDescriptor descriptor, CredentialStore credentials)
    {
        Descriptor = descriptor;
        Credentials = credentials;
    }

    public override async Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
    {
        var configured = (await Credentials.ListAccountsAsync(context.CancellationToken)).Count > 0;
        var resp = new PingResponse
        {
            Connector = Descriptor.Key,
            Version = "0.1.0-stub",
            CredentialsConfigured = configured
        };
        resp.Capabilities.Add(new[] { "publish", "fetch", "search", "profile", "engage" });
        return resp;
    }

    public override Task<PublishResponse> Publish(PublishRequest request, ServerCallContext context) =>
        Task.FromResult(new PublishResponse { Success = false, Error = $"{Descriptor.Key}.publish not implemented yet" });

    public override Task<FetchResponse> Fetch(FetchRequest request, ServerCallContext context) =>
        Task.FromResult(new FetchResponse());

    public override Task<SearchResponse> Search(SearchRequest request, ServerCallContext context) =>
        Task.FromResult(new SearchResponse());

    public override Task<ProfileResponse> Profile(ProfileRequest request, ServerCallContext context) =>
        Task.FromResult(new ProfileResponse { Handle = request.Handle, DisplayName = Descriptor.DisplayName });

    public override Task<EngageResponse> Engage(EngageRequest request, ServerCallContext context) =>
        Task.FromResult(new EngageResponse { Success = false, Error = $"{Descriptor.Key}.engage not implemented yet" });
}
