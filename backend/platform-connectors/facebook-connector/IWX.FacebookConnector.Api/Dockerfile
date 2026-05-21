FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/facebook-connector/IWX.FacebookConnector.Api/ platform-connectors/facebook-connector/IWX.FacebookConnector.Api/
RUN dotnet restore platform-connectors/facebook-connector/IWX.FacebookConnector.Api/IWX.FacebookConnector.Api.csproj
RUN dotnet publish platform-connectors/facebook-connector/IWX.FacebookConnector.Api/IWX.FacebookConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8204
EXPOSE 9204
ENTRYPOINT ["dotnet", "IWX.FacebookConnector.Api.dll"]
