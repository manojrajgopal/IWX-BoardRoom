FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/reddit-connector/IWX.RedditConnector.Api/ platform-connectors/reddit-connector/IWX.RedditConnector.Api/
RUN dotnet restore platform-connectors/reddit-connector/IWX.RedditConnector.Api/IWX.RedditConnector.Api.csproj
RUN dotnet publish platform-connectors/reddit-connector/IWX.RedditConnector.Api/IWX.RedditConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8205
EXPOSE 9205
ENTRYPOINT ["dotnet", "IWX.RedditConnector.Api.dll"]
