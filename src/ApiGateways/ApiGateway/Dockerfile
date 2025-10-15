# ===========================================
# Build Stage
# ===========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DistributedCommerce.sln", "./"]
COPY ["src/BuildingBlocks/Domain/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj", "src/BuildingBlocks/Domain/BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/Application/BuildingBlocks.Application/BuildingBlocks.Application.csproj", "src/BuildingBlocks/Application/BuildingBlocks.Application/"]
COPY ["src/BuildingBlocks/Infrastructure/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj", "src/BuildingBlocks/Infrastructure/BuildingBlocks.Infrastructure/"]
COPY ["src/BuildingBlocks/EventBus/BuildingBlocks.EventBus/BuildingBlocks.EventBus.csproj", "src/BuildingBlocks/EventBus/BuildingBlocks.EventBus/"]
COPY ["src/BuildingBlocks/EventBus/BuildingBlocks.EventBus.Kafka/BuildingBlocks.EventBus.Kafka.csproj", "src/BuildingBlocks/EventBus/BuildingBlocks.EventBus.Kafka/"]
COPY ["src/ApiGateways/ApiGateway/ApiGateway.csproj", "src/ApiGateways/ApiGateway/"]

# Restore dependencies
RUN dotnet restore "src/ApiGateways/ApiGateway/ApiGateway.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/src/ApiGateways/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ===========================================
# Runtime Stage
# ===========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose ports
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
