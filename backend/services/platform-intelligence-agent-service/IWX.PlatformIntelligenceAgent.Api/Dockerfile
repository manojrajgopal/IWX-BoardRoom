FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/platform-intelligence-agent-service/IWX.PlatformIntelligenceAgent.Api/ services/platform-intelligence-agent-service/IWX.PlatformIntelligenceAgent.Api/
RUN dotnet restore services/platform-intelligence-agent-service/IWX.PlatformIntelligenceAgent.Api/IWX.PlatformIntelligenceAgent.Api.csproj
RUN dotnet publish services/platform-intelligence-agent-service/IWX.PlatformIntelligenceAgent.Api/IWX.PlatformIntelligenceAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8094
ENTRYPOINT ["dotnet", "IWX.PlatformIntelligenceAgent.Api.dll"]
