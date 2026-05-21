FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/security/audit-service/IWX.AuditService.Api/ security/audit-service/IWX.AuditService.Api/
RUN dotnet restore security/audit-service/IWX.AuditService.Api/IWX.AuditService.Api.csproj
RUN dotnet publish security/audit-service/IWX.AuditService.Api/IWX.AuditService.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8402
ENTRYPOINT ["dotnet", "IWX.AuditService.Api.dll"]
