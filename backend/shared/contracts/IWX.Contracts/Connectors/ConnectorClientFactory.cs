using Grpc.Net.Client;
using IWX.Contracts.Connectors.Grpc;

namespace IWX.Contracts.Connectors;

/// <summary>
/// Lightweight factory that produces gRPC clients for any connector by key.
/// Lives in IWX.Contracts so any service (departments, ai-services, …) can
/// consume connectors without depending on the worker library.
/// </summary>
public sealed class ConnectorClientFactory
{
    private readonly Dictionary<string, GrpcChannel> _channels = new();
    private readonly Func<ConnectorDescriptor, string> _addressResolver;

    public ConnectorClientFactory(Func<ConnectorDescriptor, string>? addressResolver = null)
    {
        // Default: build "http://{host}:{grpcPort}" — matches docker-compose hostnames.
        _addressResolver = addressResolver ?? (d => $"http://{d.ServiceHost}:{d.GrpcPort}");
    }

    public ConnectorService.ConnectorServiceClient Get(string connectorKey)
    {
        var desc = ConnectorRegistry.ByKey(connectorKey);
        if (!_channels.TryGetValue(desc.Key, out var channel))
        {
            channel = GrpcChannel.ForAddress(_addressResolver(desc));
            _channels[desc.Key] = channel;
        }
        return new ConnectorService.ConnectorServiceClient(channel);
    }
}
