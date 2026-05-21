FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/hr-agent-service/IWX.HrAgent.Api/ services/hr-agent-service/IWX.HrAgent.Api/
RUN dotnet restore services/hr-agent-service/IWX.HrAgent.Api/IWX.HrAgent.Api.csproj
RUN dotnet publish services/hr-agent-service/IWX.HrAgent.Api/IWX.HrAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8082
ENTRYPOINT ["dotnet", "IWX.HrAgent.Api.dll"]
