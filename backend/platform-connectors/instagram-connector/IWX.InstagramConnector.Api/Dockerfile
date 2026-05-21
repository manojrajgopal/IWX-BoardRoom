FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/instagram-connector/IWX.InstagramConnector.Api/ platform-connectors/instagram-connector/IWX.InstagramConnector.Api/
RUN dotnet restore platform-connectors/instagram-connector/IWX.InstagramConnector.Api/IWX.InstagramConnector.Api.csproj
RUN dotnet publish platform-connectors/instagram-connector/IWX.InstagramConnector.Api/IWX.InstagramConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8200
EXPOSE 9200
ENTRYPOINT ["dotnet", "IWX.InstagramConnector.Api.dll"]
