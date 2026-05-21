FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/security/auth-service/IWX.AuthService.Api/ security/auth-service/IWX.AuthService.Api/
RUN dotnet restore security/auth-service/IWX.AuthService.Api/IWX.AuthService.Api.csproj
RUN dotnet publish security/auth-service/IWX.AuthService.Api/IWX.AuthService.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8401
ENTRYPOINT ["dotnet", "IWX.AuthService.Api.dll"]
