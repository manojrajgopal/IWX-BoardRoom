FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/api-gateway/IWX.ApiGateway/ api-gateway/IWX.ApiGateway/
RUN dotnet restore api-gateway/IWX.ApiGateway/IWX.ApiGateway.csproj
RUN dotnet publish api-gateway/IWX.ApiGateway/IWX.ApiGateway.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "IWX.ApiGateway.dll"]
