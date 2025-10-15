#!/bin/bash

# Distributed Commerce - Kubernetes Deployment Script
# This script deploys all microservices to a Kubernetes cluster

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

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    print_error "kubectl is not installed. Please install kubectl first."
    exit 1
fi

# Check if cluster is accessible
if ! kubectl cluster-info &> /dev/null; then
    print_error "Cannot connect to Kubernetes cluster. Please check your kubeconfig."
    exit 1
fi

print_info "Starting deployment to Kubernetes cluster..."

# Deploy base resources
print_info "Creating namespace and base resources..."
kubectl apply -f base/namespace.yaml
kubectl apply -f base/configmap.yaml
kubectl apply -f base/secrets.yaml

# Deploy infrastructure services
print_info "Deploying infrastructure services..."
kubectl apply -f infrastructure/postgres.yaml
kubectl apply -f infrastructure/redis.yaml
kubectl apply -f infrastructure/kafka.yaml
kubectl apply -f infrastructure/elasticsearch.yaml
kubectl apply -f infrastructure/jaeger.yaml

# Wait for infrastructure to be ready
print_info "Waiting for infrastructure services to be ready..."
kubectl wait --for=condition=ready pod -l app=postgres -n distributed-commerce --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n distributed-commerce --timeout=300s
kubectl wait --for=condition=ready pod -l app=kafka -n distributed-commerce --timeout=300s

# Deploy microservices
print_info "Deploying microservices..."
kubectl apply -f services/identity-service.yaml
kubectl apply -f services/catalog-service.yaml
kubectl apply -f services/order-service.yaml
kubectl apply -f services/payment-service.yaml
kubectl apply -f services/inventory-service.yaml
kubectl apply -f services/shipping-service.yaml
kubectl apply -f services/notification-service.yaml
kubectl apply -f services/analytics-service.yaml

# Deploy API Gateway
print_info "Deploying API Gateway..."
kubectl apply -f services/api-gateway.yaml

# Wait for services to be ready
print_info "Waiting for services to be ready..."
sleep 10

# Get deployment status
print_info "Deployment Status:"
kubectl get deployments -n distributed-commerce
kubectl get statefulsets -n distributed-commerce
kubectl get services -n distributed-commerce

# Get API Gateway external IP
print_info "Getting API Gateway endpoint..."
API_GATEWAY_IP=$(kubectl get svc api-gateway-service -n distributed-commerce -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

if [ -z "$API_GATEWAY_IP" ]; then
    print_warning "API Gateway LoadBalancer IP is not yet assigned. Check status with:"
    print_warning "kubectl get svc api-gateway-service -n distributed-commerce"
else
    print_info "API Gateway is accessible at: http://${API_GATEWAY_IP}"
fi

print_info "Deployment completed successfully!"
print_info "To check the status of your deployment, run:"
print_info "  kubectl get all -n distributed-commerce"
print_info "To view logs, run:"
print_info "  kubectl logs -f deployment/<service-name> -n distributed-commerce"
