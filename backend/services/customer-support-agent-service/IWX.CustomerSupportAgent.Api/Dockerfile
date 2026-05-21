FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/shared/department-worker/IWX.Departments.Worker/ shared/department-worker/IWX.Departments.Worker/
COPY backend/services/customer-support-agent-service/IWX.CustomerSupportAgent.Api/ services/customer-support-agent-service/IWX.CustomerSupportAgent.Api/
RUN dotnet restore services/customer-support-agent-service/IWX.CustomerSupportAgent.Api/IWX.CustomerSupportAgent.Api.csproj
RUN dotnet publish services/customer-support-agent-service/IWX.CustomerSupportAgent.Api/IWX.CustomerSupportAgent.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8092
ENTRYPOINT ["dotnet", "IWX.CustomerSupportAgent.Api.dll"]
