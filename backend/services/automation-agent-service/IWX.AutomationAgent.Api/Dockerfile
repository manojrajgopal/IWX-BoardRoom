FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/automation-agent-service/IWX.AutomationAgent.Api/ services/automation-agent-service/IWX.AutomationAgent.Api/
RUN dotnet restore services/automation-agent-service/IWX.AutomationAgent.Api/IWX.AutomationAgent.Api.csproj
RUN dotnet publish services/automation-agent-service/IWX.AutomationAgent.Api/IWX.AutomationAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8093
ENTRYPOINT ["dotnet", "IWX.AutomationAgent.Api.dll"]
