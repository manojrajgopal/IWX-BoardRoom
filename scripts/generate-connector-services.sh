#!/usr/bin/env bash
# Phase 4 generator — scaffolds all 9 platform connector services from one template.
# Each service:
#   - Hosts the unified ConnectorService gRPC contract (default: StubConnectorService).
#   - Exposes REST on HttpPort (health, connector, credentials).
#   - Stores credentials in its own Mongo database.
# Re-running this script overwrites the generated files.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONN_DIR="$ROOT/backend/platform-connectors"

# kebab-key:PascalName:RegistryProperty
CONNECTORS=(
  "instagram:Instagram:Instagram"
  "youtube:YouTube:YouTube"
  "linkedin:LinkedIn:LinkedIn"
  "twitter:Twitter:Twitter"
  "facebook:Facebook:Facebook"
  "reddit:Reddit:Reddit"
  "whatsapp:WhatsApp:WhatsApp"
  "email:Email:Email"
  "websites:Websites:Websites"
)

# Ports must match IWX.Contracts.Connectors.ConnectorRegistry
declare -A HTTP_PORTS=(
  [instagram]=8200 [youtube]=8201 [linkedin]=8202 [twitter]=8203 [facebook]=8204
  [reddit]=8205 [whatsapp]=8206 [email]=8207 [websites]=8208
)
declare -A GRPC_PORTS=(
  [instagram]=9200 [youtube]=9201 [linkedin]=9202 [twitter]=9203 [facebook]=9204
  [reddit]=9205 [whatsapp]=9206 [email]=9207 [websites]=9208
)
declare -A MONGO_DBS=(
  [instagram]=iwx_connector_instagram [youtube]=iwx_connector_youtube
  [linkedin]=iwx_connector_linkedin   [twitter]=iwx_connector_twitter
  [facebook]=iwx_connector_facebook   [reddit]=iwx_connector_reddit
  [whatsapp]=iwx_connector_whatsapp   [email]=iwx_connector_email
  [websites]=iwx_connector_websites
)

for entry in "${CONNECTORS[@]}"; do
  IFS=':' read -r KEY PASCAL REG <<<"$entry"
  HTTP="${HTTP_PORTS[$KEY]}"
  GRPC="${GRPC_PORTS[$KEY]}"
  DB="${MONGO_DBS[$KEY]}"
  SVC_DIR="$CONN_DIR/${KEY}-connector/IWX.${PASCAL}Connector.Api"
  mkdir -p "$SVC_DIR"

  cat >"$SVC_DIR/IWX.${PASCAL}Connector.Api.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>IWX.${PASCAL}Connector</RootNamespace>
    <AssemblyName>IWX.${PASCAL}Connector.Api</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\..\\shared\\contracts\\IWX.Contracts\\IWX.Contracts.csproj" />
    <ProjectReference Include="..\\..\\..\\shared\\common\\IWX.Common\\IWX.Common.csproj" />
    <ProjectReference Include="..\\..\\..\\shared\\connector-worker\\IWX.Connectors.Worker\\IWX.Connectors.Worker.csproj" />
  </ItemGroup>
</Project>
EOF

  cat >"$SVC_DIR/Program.cs" <<EOF
using IWX.Connectors.Worker;
using IWX.Connectors.Worker.Grpc;
using IWX.Contracts.Connectors;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxConnectorService(ConnectorRegistry.${REG});

var app = builder.Build();

// Swap StubConnectorService for a platform-specific implementation
// (Meta Graph, YouTube Data, X v2, LinkedIn, Reddit, SMTP/IMAP, scraping, …)
// in a future iteration. Contract stays identical.
app.UseIwxConnectorService<StubConnectorService>(ConnectorRegistry.${REG});

app.Run();
EOF

  cat >"$SVC_DIR/appsettings.json" <<EOF
{
  "ConnectionStrings": {
    "Mongo": "mongodb://iwx:iwx@mongo:27017/${DB}?authSource=admin"
  },
  "RabbitMq": { "Host": "rabbitmq", "User": "iwx", "Pass": "iwx" },
  "Serilog": { "MinimumLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
EOF

  cat >"$SVC_DIR/appsettings.Development.json" <<EOF
{
  "ConnectionStrings": {
    "Mongo": "mongodb://iwx:iwx@localhost:27017/${DB}?authSource=admin"
  },
  "RabbitMq": { "Host": "localhost", "User": "iwx", "Pass": "iwx" }
}
EOF

  cat >"$SVC_DIR/Dockerfile" <<EOF
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/${KEY}-connector/IWX.${PASCAL}Connector.Api/ platform-connectors/${KEY}-connector/IWX.${PASCAL}Connector.Api/
RUN dotnet restore platform-connectors/${KEY}-connector/IWX.${PASCAL}Connector.Api/IWX.${PASCAL}Connector.Api.csproj
RUN dotnet publish platform-connectors/${KEY}-connector/IWX.${PASCAL}Connector.Api/IWX.${PASCAL}Connector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE ${HTTP}
EXPOSE ${GRPC}
ENTRYPOINT ["dotnet", "IWX.${PASCAL}Connector.Api.dll"]
EOF

  echo "scaffolded ${KEY}-connector (http ${HTTP}, grpc ${GRPC}, db ${DB})"
done
