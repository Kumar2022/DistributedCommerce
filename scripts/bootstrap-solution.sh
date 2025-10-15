#!/bin/bash
set -e

echo "Bootstrapping Distributed Commerce Solution..."
echo "=================================================="

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Navigate to solution root
cd "$(dirname "$0")/.."
ROOT_DIR=$(pwd)

echo -e "${BLUE}📁 Solution root: $ROOT_DIR${NC}"

# Function to create project
create_project() {
    local project_path=$1
    local project_name=$2
    local framework=${3:-net9.0}
    
    echo -e "${GREEN}Creating project: $project_name${NC}"
    
    if [ ! -f "$project_path/$project_name.csproj" ]; then
        cd "$(dirname $project_path)"
        dotnet new classlib -n "$project_name" -f "$framework" --no-restore
        cd "$ROOT_DIR"
        dotnet sln add "$project_path/$project_name.csproj"
    else
        echo -e "${YELLOW}   Project already exists, skipping...${NC}"
    fi
}

# ============================================================================
# PHASE 1: BUILDING BLOCKS
# ============================================================================

echo -e "\n${BLUE}Phase 1: Creating Building Blocks Projects${NC}"
echo "============================================"

# Domain (already created)
echo -e "${GREEN}BuildingBlocks.Domain - Already created${NC}"

# Create Infrastructure project
create_project \
    "$ROOT_DIR/src/BuildingBlocks/Infrastructure/BuildingBlocks.Infrastructure" \
    "BuildingBlocks.Infrastructure"

# Create EventBus project  
mkdir -p "$ROOT_DIR/src/BuildingBlocks/EventBus"
create_project \
    "$ROOT_DIR/src/BuildingBlocks/EventBus/BuildingBlocks.EventBus.Kafka" \
    "BuildingBlocks.EventBus.Kafka"

# ============================================================================
# PHASE 2: IDENTITY SERVICE
# ============================================================================

echo -e "\n${BLUE}Phase 2: Creating Identity Service${NC}"
echo "===================================="

mkdir -p "$ROOT_DIR/src/Services/Identity"

create_project \
    "$ROOT_DIR/src/Services/Identity/Identity.Domain" \
    "Identity.Domain"

create_project \
    "$ROOT_DIR/src/Services/Identity/Identity.Application" \
    "Identity.Application"

create_project \
    "$ROOT_DIR/src/Services/Identity/Identity.Infrastructure" \
    "Identity.Infrastructure"

# Create API project (Web API)
if [ ! -f "$ROOT_DIR/src/Services/Identity/Identity.API/Identity.API.csproj" ]; then
    cd "$ROOT_DIR/src/Services/Identity"
    dotnet new webapi -n "Identity.API" -f net9.0 --no-restore --use-controllers false
    cd "$ROOT_DIR"
    dotnet sln add "$ROOT_DIR/src/Services/Identity/Identity.API/Identity.API.csproj"
fi

# ============================================================================
# PHASE 3: ORDER SERVICE
# ============================================================================

echo -e "\n${BLUE}Phase 3: Creating Order Service${NC}"
echo "================================"

mkdir -p "$ROOT_DIR/src/Services/Order"

create_project \
    "$ROOT_DIR/src/Services/Order/Order.Domain" \
    "Order.Domain"

create_project \
    "$ROOT_DIR/src/Services/Order/Order.Application" \
    "Order.Application"

create_project \
    "$ROOT_DIR/src/Services/Order/Order.Infrastructure" \
    "Order.Infrastructure"

if [ ! -f "$ROOT_DIR/src/Services/Order/Order.API/Order.API.csproj" ]; then
    cd "$ROOT_DIR/src/Services/Order"
    dotnet new webapi -n "Order.API" -f net9.0 --no-restore --use-controllers false
    cd "$ROOT_DIR"
    dotnet sln add "$ROOT_DIR/src/Services/Order/Order.API/Order.API.csproj"
fi

# ============================================================================
# REMAINING SERVICES
# ============================================================================

create_service() {
    local service_name=$1
    
    echo -e "\n${GREEN}Creating $service_name Service${NC}"
    
    mkdir -p "$ROOT_DIR/src/Services/$service_name"
    
    create_project \
        "$ROOT_DIR/src/Services/$service_name/$service_name.Domain" \
        "$service_name.Domain"
    
    create_project \
        "$ROOT_DIR/src/Services/$service_name/$service_name.Application" \
        "$service_name.Application"
    
    create_project \
        "$ROOT_DIR/src/Services/$service_name/$service_name.Infrastructure" \
        "$service_name.Infrastructure"
    
    if [ ! -f "$ROOT_DIR/src/Services/$service_name/$service_name.API/$service_name.API.csproj" ]; then
        cd "$ROOT_DIR/src/Services/$service_name"
        dotnet new webapi -n "$service_name.API" -f net9.0 --no-restore --use-controllers false
        cd "$ROOT_DIR"
        dotnet sln add "$ROOT_DIR/src/Services/$service_name/$service_name.API/$service_name.API.csproj"
    fi
}

create_service "Payment"
create_service "Inventory"
create_service "Shipping"
create_service "Notification"
create_service "Analytics"
create_service "Catalog"

# ============================================================================
# API GATEWAY
# ============================================================================

echo -e "\n${BLUE}Creating API Gateway${NC}"
echo "===================="

if [ ! -f "$ROOT_DIR/src/ApiGateways/ApiGateway/ApiGateway.csproj" ]; then
    cd "$ROOT_DIR/src/ApiGateways"
    dotnet new webapi -n "ApiGateway" -f net9.0 --no-restore --use-controllers false
    cd "$ROOT_DIR"
    dotnet sln add "$ROOT_DIR/src/ApiGateways/ApiGateway/ApiGateway.csproj"
fi

# ============================================================================
# RESTORE ALL PACKAGES
# ============================================================================

echo -e "\n${BLUE}Restoring NuGet packages for entire solution${NC}"
echo "=============================================="

dotnet restore

# ============================================================================
# BUILD VERIFICATION
# ============================================================================

echo -e "\n${BLUE}Building entire solution to verify${NC}"
echo "===================================="

dotnet build --no-restore

# ============================================================================
# SUMMARY
# ============================================================================

echo -e "\n${GREEN}================================================${NC}"
echo -e "${GREEN} Solution bootstrap completed successfully!${NC}"
echo -e "${GREEN}================================================${NC}"

echo -e "\n${BLUE} Summary:${NC}"
echo "  Building Blocks: 3 projects"
echo "  Services: 8 microservices (~28 projects)"
echo "  API Gateway: 1 project"
echo "  Total: ~32 projects"

echo -e "\n${BLUE} Next Steps:${NC}"
echo "  1. Implement Application building blocks (CQRS, Behaviors)"
echo "  2. Implement Infrastructure building blocks (Repository, UnitOfWork)"
echo "  3. Implement EventBus with Kafka"
echo "  4. Start Identity Service implementation"

echo -e "\n${GREEN}Happy coding! ${NC}"
