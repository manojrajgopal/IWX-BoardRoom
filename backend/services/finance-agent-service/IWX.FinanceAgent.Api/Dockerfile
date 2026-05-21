FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/finance-agent-service/IWX.FinanceAgent.Api/ services/finance-agent-service/IWX.FinanceAgent.Api/
RUN dotnet restore services/finance-agent-service/IWX.FinanceAgent.Api/IWX.FinanceAgent.Api.csproj
RUN dotnet publish services/finance-agent-service/IWX.FinanceAgent.Api/IWX.FinanceAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8084
ENTRYPOINT ["dotnet", "IWX.FinanceAgent.Api.dll"]
