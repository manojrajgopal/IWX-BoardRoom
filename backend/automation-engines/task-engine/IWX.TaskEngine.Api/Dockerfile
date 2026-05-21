FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/automation-engines/task-engine/IWX.TaskEngine.Api/ automation-engines/task-engine/IWX.TaskEngine.Api/
RUN dotnet restore automation-engines/task-engine/IWX.TaskEngine.Api/IWX.TaskEngine.Api.csproj
RUN dotnet publish automation-engines/task-engine/IWX.TaskEngine.Api/IWX.TaskEngine.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8302
ENTRYPOINT ["dotnet", "IWX.TaskEngine.Api.dll"]
