FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/shared/contracts/IWX.Contracts/ shared/contracts/IWX.Contracts/
COPY backend/shared/common/IWX.Common/ shared/common/IWX.Common/
COPY backend/ai-services/memory-engine/IWX.MemoryEngine.Api/ ai-services/memory-engine/IWX.MemoryEngine.Api/
RUN dotnet restore ai-services/memory-engine/IWX.MemoryEngine.Api/IWX.MemoryEngine.Api.csproj
RUN dotnet publish ai-services/memory-engine/IWX.MemoryEngine.Api/IWX.MemoryEngine.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8100
ENTRYPOINT ["dotnet", "IWX.MemoryEngine.Api.dll"]
