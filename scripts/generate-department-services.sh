#!/usr/bin/env bash
# Phase 2 generator — scaffolds all 13 department agent services from one template.
# Idempotent: re-running overwrites the generated files.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SERVICES_DIR="$ROOT/backend/services"

# kebab-key:PascalName:RegistryProperty
DEPTS=(
  "hr:Hr:Hr"
  "sales:Sales:Sales"
  "finance:Finance:Finance"
  "marketing:Marketing:Marketing"
  "operations:Operations:Operations"
  "development:Development:Development"
  "research:Research:Research"
  "legal:Legal:Legal"
  "social-media:SocialMedia:SocialMedia"
  "analytics:Analytics:Analytics"
  "customer-support:CustomerSupport:CustomerSupport"
  "automation:Automation:Automation"
  "platform-intelligence:PlatformIntelligence:PlatformIntelligence"
)

# Port table must match IWX.Contracts.Departments.DepartmentRegistry
declare -A PORTS=(
  [hr]=8082 [sales]=8083 [finance]=8084 [marketing]=8085 [operations]=8086
  [development]=8087 [research]=8088 [legal]=8089 [social-media]=8090
  [analytics]=8091 [customer-support]=8092 [automation]=8093 [platform-intelligence]=8094
)

declare -A DBS=(
  [hr]=IwxHr [sales]=IwxSales [finance]=IwxFinance [marketing]=IwxMarketing
  [operations]=IwxOperations [development]=IwxDevelopment [research]=IwxResearch
  [legal]=IwxLegal [social-media]=IwxSocialMedia [analytics]=IwxAnalytics
  [customer-support]=IwxCustomerSupport [automation]=IwxAutomation
  [platform-intelligence]=IwxPlatformIntelligence
)

for entry in "${DEPTS[@]}"; do
  IFS=':' read -r KEY PASCAL REG <<<"$entry"
  PORT="${PORTS[$KEY]}"
  DB="${DBS[$KEY]}"
  SVC_DIR="$SERVICES_DIR/${KEY}-agent-service/IWX.${PASCAL}Agent.Api"
  mkdir -p "$SVC_DIR"

  cat >"$SVC_DIR/IWX.${PASCAL}Agent.Api.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>IWX.${PASCAL}Agent</RootNamespace>
    <AssemblyName>IWX.${PASCAL}Agent.Api</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\..\\shared\\contracts\\IWX.Contracts\\IWX.Contracts.csproj" />
    <ProjectReference Include="..\\..\\..\\shared\\common\\IWX.Common\\IWX.Common.csproj" />
    <ProjectReference Include="..\\..\\..\\shared\\department-worker\\IWX.Departments.Worker\\IWX.Departments.Worker.csproj" />
  </ItemGroup>
</Project>
EOF

  cat >"$SVC_DIR/Program.cs" <<EOF
using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.${REG});

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.${REG});

app.Run();
EOF

  cat >"$SVC_DIR/appsettings.json" <<EOF
{
  "ConnectionStrings": {
    "Sql": "Server=sqlserver,1433;Database=${DB};User Id=sa;Password=IwxStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False"
  },
  "RabbitMq": { "Host": "rabbitmq", "User": "iwx", "Pass": "iwx" },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:${PORT}" } } },
  "Serilog": { "MinimumLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
EOF

  cat >"$SVC_DIR/appsettings.Development.json" <<EOF
{
  "ConnectionStrings": {
    "Sql": "Server=localhost,1433;Database=${DB};User Id=sa;Password=IwxStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False"
  },
  "RabbitMq": { "Host": "localhost", "User": "iwx", "Pass": "iwx" }
}
EOF

  cat >"$SVC_DIR/Dockerfile" <<EOF
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/${KEY}-agent-service/IWX.${PASCAL}Agent.Api/ services/${KEY}-agent-service/IWX.${PASCAL}Agent.Api/
RUN dotnet restore services/${KEY}-agent-service/IWX.${PASCAL}Agent.Api/IWX.${PASCAL}Agent.Api.csproj
RUN dotnet publish services/${KEY}-agent-service/IWX.${PASCAL}Agent.Api/IWX.${PASCAL}Agent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE ${PORT}
ENTRYPOINT ["dotnet", "IWX.${PASCAL}Agent.Api.dll"]
EOF

  echo "scaffolded ${KEY}-agent-service (port ${PORT}, db ${DB})"
done
