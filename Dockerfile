FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 15672  
EXPOSE 5672   

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["VinWallet.API/VinWallet.API.csproj", "VinWallet.API/"]
COPY ["VinWallet.Repository/VinWallet.Repository.csproj", "VinWallet.Repository/"]
COPY ["VinWallet.Domain/VinWallet.Domain.csproj", "VinWallet.Domain/"]
RUN dotnet restore "VinWallet.API/VinWallet.API.csproj"
COPY . .
WORKDIR "/src/VinWallet.API"
RUN dotnet build "VinWallet.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VinWallet.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM rabbitmq:3-management AS final
WORKDIR /app

COPY rabbitmq.conf /etc/rabbitmq/rabbitmq.conf
ENV RABBITMQ_DEFAULT_USER=guest
ENV RABBITMQ_DEFAULT_PASS=guest
ENV RABBITMQ_ERLANG_COOKIE="secretcookie"


COPY --from=publish /app/publish .

CMD ["sh", "-c", "rabbitmq-server & dotnet VinWallet.API.dll"]
