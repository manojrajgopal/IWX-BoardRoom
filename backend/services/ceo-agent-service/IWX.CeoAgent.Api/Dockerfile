FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/services/ceo-agent-service/IWX.CeoAgent.Api/ services/ceo-agent-service/IWX.CeoAgent.Api/
RUN dotnet restore services/ceo-agent-service/IWX.CeoAgent.Api/IWX.CeoAgent.Api.csproj
RUN dotnet publish services/ceo-agent-service/IWX.CeoAgent.Api/IWX.CeoAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8081
ENTRYPOINT ["dotnet", "IWX.CeoAgent.Api.dll"]
