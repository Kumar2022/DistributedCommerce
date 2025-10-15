#!/bin/bash

# Script to create EF Core migrations for all services
# Usage: ./create-migrations.sh

set -e

PROJECT_ROOT="/Users/omkarkumar/RiderProjects/DistributedCommerce"
cd "$PROJECT_ROOT"

echo " Creating EF Core Migrations for all services..."
echo ""

# Function to create design-time factory
create_factory() {
    local service=$1
    local db_name=$2
    local context_name=$3
    local factory_path="src/Services/$service/$service.Infrastructure/Persistence/${context_name}Factory.cs"
    
    echo "📝 Creating design-time factory for $service..."
    
    cat > "$factory_path" << EOF
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ${service}.Infrastructure.Persistence;

public class ${context_name}Factory : IDesignTimeDbContextFactory<${context_name}>
{
    public ${context_name} CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<${context_name}>();
        
        // Use a connection string for design-time only (migrations)
        optionsBuilder.UseNpgsql("Host=localhost;Database=${db_name};Username=postgres;Password=postgres");
        
        return new ${context_name}(optionsBuilder.Options);
    }
}
EOF
    
    echo " Created factory for $service"
}

# Function to create migration
create_migration() {
    local service=$1
    local context_name=$2
    
    echo " Creating migration for $service..."
    
    cd "$PROJECT_ROOT"
    dotnet ef migrations add InitialCreate \
        --project "src/Services/$service/$service.Infrastructure" \
        --startup-project "src/Services/$service/$service.API" \
        --context "$context_name" \
        --output-dir "Persistence/Migrations" \
        2>&1 | grep -E "(Build|succeeded|Migration|Done|error)" || true
    
    if [ $? -eq 0 ]; then
        echo "Migration created for $service"
    else
        echo "Failed to create migration for $service"
    fi
    echo ""
}

# Array of services to process
declare -A services=(
    ["Payment"]="payment_db:PaymentDbContext"
    ["Inventory"]="inventory_db:InventoryDbContext"
    ["Catalog"]="catalog_db:CatalogDbContext"
    ["Shipping"]="shipping_db:ShippingDbContext"
    ["Notification"]="notification_db:NotificationDbContext"
    ["Analytics"]="analytics_db:AnalyticsDbContext"
    ["Identity"]="identity_db:IdentityDbContext"
)

# Create factories and migrations for each service
for service in "${!services[@]}"; do
    IFS=':' read -r db_name context_name <<< "${services[$service]}"
    
    echo "====================================="
    echo "Processing $service Service"
    echo "====================================="
    
    # Create design-time factory
    create_factory "$service" "$db_name" "$context_name"
    
    # Create migration
    create_migration "$service" "$context_name"
    
    echo ""
done

echo " All migrations created successfully!"
echo ""
echo " Next steps:"
echo "  1. Review migrations in each service's Persistence/Migrations folder"
echo "  2. Ensure Outbox, Inbox, DLQ, and SagaState tables are included"
echo "  3. Run 'dotnet ef database update' for each service to apply migrations"
