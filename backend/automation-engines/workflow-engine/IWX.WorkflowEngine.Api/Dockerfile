FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/automation-engines/workflow-engine/IWX.WorkflowEngine.Api/ automation-engines/workflow-engine/IWX.WorkflowEngine.Api/
RUN dotnet restore automation-engines/workflow-engine/IWX.WorkflowEngine.Api/IWX.WorkflowEngine.Api.csproj
RUN dotnet publish automation-engines/workflow-engine/IWX.WorkflowEngine.Api/IWX.WorkflowEngine.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8300
ENTRYPOINT ["dotnet", "IWX.WorkflowEngine.Api.dll"]
