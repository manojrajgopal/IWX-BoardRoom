FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/linkedin-connector/IWX.LinkedInConnector.Api/ platform-connectors/linkedin-connector/IWX.LinkedInConnector.Api/
RUN dotnet restore platform-connectors/linkedin-connector/IWX.LinkedInConnector.Api/IWX.LinkedInConnector.Api.csproj
RUN dotnet publish platform-connectors/linkedin-connector/IWX.LinkedInConnector.Api/IWX.LinkedInConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8202
EXPOSE 9202
ENTRYPOINT ["dotnet", "IWX.LinkedInConnector.Api.dll"]
