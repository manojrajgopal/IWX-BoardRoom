FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/email-connector/IWX.EmailConnector.Api/ platform-connectors/email-connector/IWX.EmailConnector.Api/
RUN dotnet restore platform-connectors/email-connector/IWX.EmailConnector.Api/IWX.EmailConnector.Api.csproj
RUN dotnet publish platform-connectors/email-connector/IWX.EmailConnector.Api/IWX.EmailConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8207
EXPOSE 9207
ENTRYPOINT ["dotnet", "IWX.EmailConnector.Api.dll"]
