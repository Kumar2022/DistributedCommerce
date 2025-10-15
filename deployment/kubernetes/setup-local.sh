#!/bin/bash

# Local Kubernetes Development Setup
# This script sets up a local Kubernetes cluster for development

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}================================${NC}"
}

# Check prerequisites
check_prerequisites() {
    print_header "Checking Prerequisites"
    
    local missing_tools=()
    
    if ! command -v docker &> /dev/null; then
        missing_tools+=("docker")
    fi
    
    if ! command -v kubectl &> /dev/null; then
        missing_tools+=("kubectl")
    fi
    
    if ! command -v kind &> /dev/null && ! command -v minikube &> /dev/null; then
        print_warning "Neither 'kind' nor 'minikube' found. Installing kind..."
        if [[ "$OSTYPE" == "darwin"* ]]; then
            brew install kind
        else
            curl -Lo ./kind https://kind.sigs.k8s.io/dl/latest/kind-linux-amd64
            chmod +x ./kind
            sudo mv ./kind /usr/local/bin/kind
        fi
    fi
    
    if [ ${#missing_tools[@]} -ne 0 ]; then
        print_error "Missing required tools: ${missing_tools[*]}"
        print_info "Please install the missing tools and try again."
        exit 1
    fi
    
    print_info "All prerequisites met!"
}

# Create local cluster
create_cluster() {
    print_header "Creating Local Kubernetes Cluster"
    
    if command -v kind &> /dev/null; then
        print_info "Using kind..."
        
        # Check if cluster exists
        if kind get clusters | grep -q "distributed-commerce"; then
            print_warning "Cluster 'distributed-commerce' already exists"
            read -p "Delete and recreate? (yes/no): " confirm
            if [ "$confirm" = "yes" ]; then
                kind delete cluster --name distributed-commerce
            else
                print_info "Using existing cluster"
                return
            fi
        fi
        
        # Create kind cluster with custom config
        cat <<EOF | kind create cluster --name distributed-commerce --config=-
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
  kubeadmConfigPatches:
  - |
    kind: InitConfiguration
    nodeRegistration:
      kubeletExtraArgs:
        node-labels: "ingress-ready=true"
  extraPortMappings:
  - containerPort: 80
    hostPort: 80
    protocol: TCP
  - containerPort: 443
    hostPort: 443
    protocol: TCP
- role: worker
- role: worker
EOF
        
        print_info "Kind cluster created successfully!"
        
    elif command -v minikube &> /dev/null; then
        print_info "Using minikube..."
        
        if minikube status &> /dev/null; then
            print_warning "Minikube is already running"
            read -p "Delete and recreate? (yes/no): " confirm
            if [ "$confirm" = "yes" ]; then
                minikube delete
            else
                print_info "Using existing minikube cluster"
                return
            fi
        fi
        
        minikube start \
            --cpus=4 \
            --memory=8192 \
            --disk-size=50g \
            --driver=docker \
            --kubernetes-version=v1.28.0
        
        print_info "Minikube cluster created successfully!"
    fi
    
    # Wait for cluster to be ready
    print_info "Waiting for cluster to be ready..."
    kubectl wait --for=condition=Ready nodes --all --timeout=300s
}

# Install NGINX Ingress Controller
install_ingress() {
    print_header "Installing NGINX Ingress Controller"
    
    if command -v kind &> /dev/null; then
        kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
    else
        kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/cloud/deploy.yaml
    fi
    
    print_info "Waiting for ingress controller to be ready..."
    kubectl wait --namespace ingress-nginx \
        --for=condition=ready pod \
        --selector=app.kubernetes.io/component=controller \
        --timeout=300s
    
    print_info "Ingress controller installed!"
}

# Load images into cluster
load_images() {
    print_header "Building and Loading Docker Images"
    
    # Build images
    print_info "Building images..."
    ./build-images.sh
    
    # Load into kind (if using kind)
    if command -v kind &> /dev/null && kind get clusters | grep -q "distributed-commerce"; then
        print_info "Loading images into kind cluster..."
        
        for service in api-gateway identity-service catalog-service order-service payment-service inventory-service shipping-service notification-service analytics-service; do
            print_info "Loading $service..."
            kind load docker-image distributed-commerce/$service:latest --name distributed-commerce
        done
    fi
    
    print_info "Images loaded successfully!"
}

# Deploy to cluster
deploy_services() {
    print_header "Deploying Services"
    
    print_info "Deploying to development environment..."
    kubectl apply -k ../overlays/dev
    
    print_info "Waiting for services to be ready..."
    sleep 10
    
    print_info "Deployment status:"
    kubectl get all -n distributed-commerce-dev
}

# Display access information
display_info() {
    print_header "Access Information"
    
    if command -v kind &> /dev/null; then
        print_info "Using kind cluster - services accessible via:"
        echo "  API Gateway: http://localhost:80"
        echo "  Jaeger UI: kubectl port-forward svc/dev-jaeger-service 16686:16686 -n distributed-commerce-dev"
        echo "  PostgreSQL: kubectl port-forward svc/dev-postgres-service 5432:5432 -n distributed-commerce-dev"
    else
        MINIKUBE_IP=$(minikube ip)
        print_info "Using minikube - services accessible via:"
        echo "  API Gateway: http://$MINIKUBE_IP"
        echo "  Or use: minikube service dev-api-gateway-service -n distributed-commerce-dev"
    fi
    
    echo ""
    print_info "Useful commands:"
    echo "  View logs: kubectl logs -f deployment/dev-api-gateway -n distributed-commerce-dev"
    echo "  Get pods: kubectl get pods -n distributed-commerce-dev"
    echo "  Get services: kubectl get svc -n distributed-commerce-dev"
    echo "  Delete cluster: kind delete cluster --name distributed-commerce (or minikube delete)"
}

# Main execution
main() {
    print_header "Distributed Commerce - Local Development Setup"
    
    check_prerequisites
    create_cluster
    install_ingress
    load_images
    deploy_services
    display_info
    
    print_header "Setup Complete!"
    print_info "Your local Kubernetes development environment is ready! 🚀"
}

# Run main function
main
