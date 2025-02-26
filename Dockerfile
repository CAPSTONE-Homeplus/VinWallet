FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VinWallet.API/VinWallet.API.csproj", "VinWallet.API/"]
COPY ["VinWallet.Repository/VinWallet.Repository.csproj", "VinWallet.Repository/"]
COPY ["VinWallet.Domain/VinWallet.Domain.csproj", "VinWallet.Domain/"]
RUN dotnet restore "./VinWallet.API/VinWallet.API.csproj"
COPY . .
WORKDIR "/src/VinWallet.API"
RUN dotnet build "./VinWallet.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VinWallet.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VinWallet.API.dll"]