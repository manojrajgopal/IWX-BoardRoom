FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/development-agent-service/IWX.DevelopmentAgent.Api/ services/development-agent-service/IWX.DevelopmentAgent.Api/
RUN dotnet restore services/development-agent-service/IWX.DevelopmentAgent.Api/IWX.DevelopmentAgent.Api.csproj
RUN dotnet publish services/development-agent-service/IWX.DevelopmentAgent.Api/IWX.DevelopmentAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8087
ENTRYPOINT ["dotnet", "IWX.DevelopmentAgent.Api.dll"]
