using IWX.Common.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.api-gateway");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "api-gateway" }));
app.MapReverseProxy();

app.Run();
