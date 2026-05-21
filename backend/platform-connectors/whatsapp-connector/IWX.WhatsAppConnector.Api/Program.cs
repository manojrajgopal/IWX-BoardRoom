using IWX.Connectors.Worker;
using IWX.Connectors.Worker.Grpc;
using IWX.Contracts.Connectors;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxConnectorService(ConnectorRegistry.WhatsApp);

var app = builder.Build();

// Swap StubConnectorService for a platform-specific implementation
// (Meta Graph, YouTube Data, X v2, LinkedIn, Reddit, SMTP/IMAP, scraping, …)
// in a future iteration. Contract stays identical.
app.UseIwxConnectorService<StubConnectorService>(ConnectorRegistry.WhatsApp);

app.Run();
