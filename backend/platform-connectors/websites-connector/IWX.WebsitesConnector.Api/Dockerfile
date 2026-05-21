FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/websites-connector/IWX.WebsitesConnector.Api/ platform-connectors/websites-connector/IWX.WebsitesConnector.Api/
RUN dotnet restore platform-connectors/websites-connector/IWX.WebsitesConnector.Api/IWX.WebsitesConnector.Api.csproj
RUN dotnet publish platform-connectors/websites-connector/IWX.WebsitesConnector.Api/IWX.WebsitesConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8208
EXPOSE 9208
ENTRYPOINT ["dotnet", "IWX.WebsitesConnector.Api.dll"]
