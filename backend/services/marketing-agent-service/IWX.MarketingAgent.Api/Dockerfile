FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/marketing-agent-service/IWX.MarketingAgent.Api/ services/marketing-agent-service/IWX.MarketingAgent.Api/
RUN dotnet restore services/marketing-agent-service/IWX.MarketingAgent.Api/IWX.MarketingAgent.Api.csproj
RUN dotnet publish services/marketing-agent-service/IWX.MarketingAgent.Api/IWX.MarketingAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8085
ENTRYPOINT ["dotnet", "IWX.MarketingAgent.Api.dll"]
