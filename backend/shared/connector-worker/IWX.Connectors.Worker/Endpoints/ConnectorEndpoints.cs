using IWX.Connectors.Worker.Infrastructure;
using IWX.Contracts.Connectors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace IWX.Connectors.Worker.Endpoints;

public static class ConnectorEndpoints
{
    public static IEndpointRouteBuilder MapConnectorEndpoints(
        this IEndpointRouteBuilder app, ConnectorDescriptor descriptor)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", connector = descriptor.Key }));

        app.MapGet("/connector", () => Results.Ok(new
        {
            descriptor.Key,
            descriptor.DisplayName,
            descriptor.HttpPort,
            descriptor.GrpcPort,
            descriptor.ServiceHost,
            descriptor.Icon
        }));

        app.MapGet("/credentials", async (CredentialStore store, CancellationToken ct) =>
            Results.Ok(await store.ListAccountsAsync(ct)));

        app.MapGet("/credentials/{account}", async (string account, CredentialStore store, CancellationToken ct) =>
        {
            var c = await store.GetAsync(account, ct);
            return c is null ? Results.NotFound() : Results.Ok(new { c.Account, c.UpdatedAtUtc, Keys = c.Values.Keys });
        });

        app.MapPut("/credentials/{account}", async (
            string account, Dictionary<string, string> values, CredentialStore store, CancellationToken ct) =>
        {
            await store.UpsertAsync(new ConnectorCredential { Account = account, Values = values }, ct);
            return Results.NoContent();
        });

        app.MapDelete("/credentials/{account}", async (string account, CredentialStore store, CancellationToken ct) =>
            await store.DeleteAsync(account, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
