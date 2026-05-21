FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/twitter-connector/IWX.TwitterConnector.Api/ platform-connectors/twitter-connector/IWX.TwitterConnector.Api/
RUN dotnet restore platform-connectors/twitter-connector/IWX.TwitterConnector.Api/IWX.TwitterConnector.Api.csproj
RUN dotnet publish platform-connectors/twitter-connector/IWX.TwitterConnector.Api/IWX.TwitterConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8203
EXPOSE 9203
ENTRYPOINT ["dotnet", "IWX.TwitterConnector.Api.dll"]
