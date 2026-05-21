FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/legal-agent-service/IWX.LegalAgent.Api/ services/legal-agent-service/IWX.LegalAgent.Api/
RUN dotnet restore services/legal-agent-service/IWX.LegalAgent.Api/IWX.LegalAgent.Api.csproj
RUN dotnet publish services/legal-agent-service/IWX.LegalAgent.Api/IWX.LegalAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8089
ENTRYPOINT ["dotnet", "IWX.LegalAgent.Api.dll"]
