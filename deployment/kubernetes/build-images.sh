#!/bin/bash

# Distributed Commerce - Docker Build Script
# This script builds all Docker images for the microservices

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Docker registry (change this to your registry)
REGISTRY=${DOCKER_REGISTRY:-"distributed-commerce"}
VERSION=${VERSION:-"latest"}

# Base directory
BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

print_info "Building Docker images..."
print_info "Registry: $REGISTRY"
print_info "Version: $VERSION"
print_info "Base directory: $BASE_DIR"

# Services to build
declare -a services=(
    "ApiGateways/ApiGateway:api-gateway"
    "Services/Identity/Identity.API:identity-service"
    "Services/Catalog/Catalog.API:catalog-service"
    "Services/Order/Order.API:order-service"
    "Services/Payment/Payment.API:payment-service"
    "Services/Inventory/Inventory.API:inventory-service"
    "Services/Shipping/Shipping.API:shipping-service"
    "Services/Notification/Notification.API:notification-service"
    "Services/Analytics/Analytics.API:analytics-service"
)

# Build each service
for service in "${services[@]}"; do
    IFS=':' read -r path name <<< "$service"
    
    print_info "Building $name..."
    
    docker build \
        -f "$BASE_DIR/src/$path/Dockerfile" \
        -t "$REGISTRY/$name:$VERSION" \
        -t "$REGISTRY/$name:latest" \
        "$BASE_DIR" \
        || { print_error "Failed to build $name"; exit 1; }
    
    print_info "Successfully built $name"
done

print_info "All images built successfully!"
print_info ""
print_info "To push images to registry, run:"
print_info "  docker push $REGISTRY/<service-name>:$VERSION"
print_info ""
print_info "Or to push all images:"
for service in "${services[@]}"; do
    IFS=':' read -r path name <<< "$service"
    echo "  docker push $REGISTRY/$name:$VERSION"
done
