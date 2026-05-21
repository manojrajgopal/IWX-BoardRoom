FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/youtube-connector/IWX.YouTubeConnector.Api/ platform-connectors/youtube-connector/IWX.YouTubeConnector.Api/
RUN dotnet restore platform-connectors/youtube-connector/IWX.YouTubeConnector.Api/IWX.YouTubeConnector.Api.csproj
RUN dotnet publish platform-connectors/youtube-connector/IWX.YouTubeConnector.Api/IWX.YouTubeConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8201
EXPOSE 9201
ENTRYPOINT ["dotnet", "IWX.YouTubeConnector.Api.dll"]
