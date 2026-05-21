using IWX.Common.Observability;
using IWX.Connectors.Worker.Endpoints;
using IWX.Connectors.Worker.Grpc;
using IWX.Connectors.Worker.Infrastructure;
using IWX.Contracts.Connectors;
using IWX.Contracts.Connectors.Grpc;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace IWX.Connectors.Worker;

/// <summary>
/// One-liner setup for any platform connector service.
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.AddIwxConnectorService(ConnectorRegistry.Instagram);
/// var app = builder.Build();
/// app.UseIwxConnectorService&lt;StubConnectorService&gt;(ConnectorRegistry.Instagram);
/// app.Run();
/// </code>
/// </summary>
public static class ConnectorWorkerExtensions
{
    public static WebApplicationBuilder AddIwxConnectorService(
        this WebApplicationBuilder builder, ConnectorDescriptor descriptor)
    {
        builder.AddIwxObservability($"iwx.{descriptor.Key}-connector");

        builder.Services.AddSingleton(descriptor);

        // Mongo — credentials store + per-connector cache.
        var mongoUri = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://iwx:iwx@mongo:27017";
        builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUri));
        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(descriptor.MongoDb));
        builder.Services.AddSingleton<CredentialStore>();

        // RabbitMQ outbound — connectors publish inbound platform events
        // (mentions, replies, DMs) onto the iwx.connector.event fanout.
        builder.Services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                var rmq = builder.Configuration.GetSection("RabbitMq");
                cfg.Host(rmq["Host"] ?? "localhost", h =>
                {
                    h.Username(rmq["User"] ?? "guest");
                    h.Password(rmq["Pass"] ?? "guest");
                });
            });
        });

        // Kestrel: HTTP/1.1 on HttpPort for REST, HTTP/2 (h2c) on GrpcPort for gRPC.
        builder.Services.Configure<KestrelServerOptions>(o =>
        {
            o.ListenAnyIP(descriptor.HttpPort, l => l.Protocols = HttpProtocols.Http1);
            o.ListenAnyIP(descriptor.GrpcPort, l => l.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddGrpc();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
            .WithOrigins("http://localhost:4200", "http://localhost:8080")
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        return builder;
    }

    public static WebApplication UseIwxConnectorService<TGrpcImpl>(
        this WebApplication app, ConnectorDescriptor descriptor)
        where TGrpcImpl : ConnectorService.ConnectorServiceBase
    {
        app.UseCors();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapGrpcService<TGrpcImpl>();
        app.MapConnectorEndpoints(descriptor);

        return app;
    }
}
