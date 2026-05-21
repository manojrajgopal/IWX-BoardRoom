FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/connector-worker/IWX.Connectors.Worker/ shared/connector-worker/IWX.Connectors.Worker/
COPY backend/platform-connectors/whatsapp-connector/IWX.WhatsAppConnector.Api/ platform-connectors/whatsapp-connector/IWX.WhatsAppConnector.Api/
RUN dotnet restore platform-connectors/whatsapp-connector/IWX.WhatsAppConnector.Api/IWX.WhatsAppConnector.Api.csproj
RUN dotnet publish platform-connectors/whatsapp-connector/IWX.WhatsAppConnector.Api/IWX.WhatsAppConnector.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8206
EXPOSE 9206
ENTRYPOINT ["dotnet", "IWX.WhatsAppConnector.Api.dll"]
