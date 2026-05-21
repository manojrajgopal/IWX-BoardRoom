FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/sales-agent-service/IWX.SalesAgent.Api/ services/sales-agent-service/IWX.SalesAgent.Api/
RUN dotnet restore services/sales-agent-service/IWX.SalesAgent.Api/IWX.SalesAgent.Api.csproj
RUN dotnet publish services/sales-agent-service/IWX.SalesAgent.Api/IWX.SalesAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8083
ENTRYPOINT ["dotnet", "IWX.SalesAgent.Api.dll"]
