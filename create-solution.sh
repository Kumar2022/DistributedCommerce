#!/bin/bash
set -e

echo "🏗️  Creating FAANG-Scale Distributed Commerce Solution..."

# Create solution
dotnet new sln -n DistributedCommerce

# Create solution structure
mkdir -p src/BuildingBlocks/{Domain,Application,Infrastructure,EventBus,ApiGateway}
mkdir -p src/Services/{Identity,Order,Payment,Inventory,Shipping,Notification,Analytics,Catalog}
mkdir -p src/ApiGateways
mkdir -p tests/{Unit,Integration,Load,E2E}
mkdir -p deployment/{kubernetes,helm,terraform}
mkdir -p docs

echo "✅ Directory structure created"
