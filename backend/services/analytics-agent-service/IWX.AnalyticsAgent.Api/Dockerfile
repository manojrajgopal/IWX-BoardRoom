FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/analytics-agent-service/IWX.AnalyticsAgent.Api/ services/analytics-agent-service/IWX.AnalyticsAgent.Api/
RUN dotnet restore services/analytics-agent-service/IWX.AnalyticsAgent.Api/IWX.AnalyticsAgent.Api.csproj
RUN dotnet publish services/analytics-agent-service/IWX.AnalyticsAgent.Api/IWX.AnalyticsAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8091
ENTRYPOINT ["dotnet", "IWX.AnalyticsAgent.Api.dll"]
