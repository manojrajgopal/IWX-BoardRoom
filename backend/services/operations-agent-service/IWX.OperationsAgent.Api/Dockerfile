FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/operations-agent-service/IWX.OperationsAgent.Api/ services/operations-agent-service/IWX.OperationsAgent.Api/
RUN dotnet restore services/operations-agent-service/IWX.OperationsAgent.Api/IWX.OperationsAgent.Api.csproj
RUN dotnet publish services/operations-agent-service/IWX.OperationsAgent.Api/IWX.OperationsAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8086
ENTRYPOINT ["dotnet", "IWX.OperationsAgent.Api.dll"]
