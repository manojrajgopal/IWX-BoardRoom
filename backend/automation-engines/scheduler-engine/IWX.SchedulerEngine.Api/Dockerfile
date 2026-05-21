FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/automation-engines/scheduler-engine/IWX.SchedulerEngine.Api/ automation-engines/scheduler-engine/IWX.SchedulerEngine.Api/
RUN dotnet restore automation-engines/scheduler-engine/IWX.SchedulerEngine.Api/IWX.SchedulerEngine.Api.csproj
RUN dotnet publish automation-engines/scheduler-engine/IWX.SchedulerEngine.Api/IWX.SchedulerEngine.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8301
ENTRYPOINT ["dotnet", "IWX.SchedulerEngine.Api.dll"]
