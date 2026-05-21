FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/automation-engines/approval-engine/IWX.ApprovalEngine.Api/ automation-engines/approval-engine/IWX.ApprovalEngine.Api/
RUN dotnet restore automation-engines/approval-engine/IWX.ApprovalEngine.Api/IWX.ApprovalEngine.Api.csproj
RUN dotnet publish automation-engines/approval-engine/IWX.ApprovalEngine.Api/IWX.ApprovalEngine.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8303
ENTRYPOINT ["dotnet", "IWX.ApprovalEngine.Api.dll"]
