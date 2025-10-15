#!/bin/bash

# Distributed Commerce - Kubernetes Cleanup Script
# This script removes all deployed resources from the Kubernetes cluster

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Confirmation prompt
read -p "Are you sure you want to delete all resources in the distributed-commerce namespace? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    print_info "Cleanup cancelled."
    exit 0
fi

print_warning "Deleting all resources in distributed-commerce namespace..."

# Delete microservices
print_info "Deleting microservices..."
kubectl delete -f services/ --ignore-not-found=true

# Delete infrastructure
print_info "Deleting infrastructure services..."
kubectl delete -f infrastructure/ --ignore-not-found=true

# Delete base resources
print_info "Deleting base resources..."
kubectl delete -f base/configmap.yaml --ignore-not-found=true
kubectl delete -f base/secrets.yaml --ignore-not-found=true

# Delete namespace (this will delete everything in it)
print_info "Deleting namespace..."
kubectl delete -f base/namespace.yaml --ignore-not-found=true

print_info "Cleanup completed!"
