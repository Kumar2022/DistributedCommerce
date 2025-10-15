#!/bin/bash

# Deploy Complete Stack with All Improvements
# This script deploys the full production-ready stack

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }
print_header() { echo -e "${BLUE}=== $1 ===${NC}"; }

# Check prerequisites
check_prerequisites() {
    print_header "Checking Prerequisites"
    
    for cmd in kubectl helm istioctl; do
        if ! command -v $cmd &> /dev/null; then
            print_warning "$cmd not found (optional for some features)"
        else
            print_info "$cmd found"
        fi
    done
}

# Deploy External Secrets Operator
deploy_external_secrets() {
    print_header "Deploying External Secrets Operator"
    
    read -p "Deploy External Secrets Operator? (yes/no): " confirm
    if [ "$confirm" = "yes" ]; then
        print_info "Installing External Secrets Operator..."
        kubectl apply -f https://raw.githubusercontent.com/external-secrets/external-secrets/main/deploy/crds/bundle.yaml
        kubectl apply -f https://raw.githubusercontent.com/external-secrets/external-secrets/main/deploy/external-secrets.yaml
        
        print_info "Waiting for operator to be ready..."
        kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=external-secrets -n external-secrets-system --timeout=300s
        
        print_info "Applying external secrets configuration..."
        kubectl apply -f external-secrets/external-secrets-config.yaml
    else
        print_warning "Skipping External Secrets Operator"
    fi
}

# Deploy Sealed Secrets (alternative)
deploy_sealed_secrets() {
    print_header "Deploying Sealed Secrets"
    
    read -p "Deploy Sealed Secrets? (yes/no): " confirm
    if [ "$confirm" = "yes" ]; then
        print_info "Installing Sealed Secrets..."
        kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.24.0/controller.yaml
        
        print_info "Waiting for controller to be ready..."
        kubectl wait --for=condition=ready pod -l name=sealed-secrets-controller -n kube-system --timeout=300s
    else
        print_warning "Skipping Sealed Secrets"
    fi
}

# Deploy Security Policies
deploy_security() {
    print_header "Deploying Security Policies"
    
    print_info "Applying Network Policies..."
    kubectl apply -f security/network-policies.yaml
    
    print_info "Applying RBAC configurations..."
    kubectl apply -f security/rbac.yaml
    
    print_info "Applying Pod Security Standards..."
    kubectl apply -f security/pod-security-standards.yaml
    
    print_info "Security policies deployed successfully"
}

# Deploy Reliability Features
deploy_reliability() {
    print_header "Deploying Reliability Features"
    
    print_info "Applying Pod Disruption Budgets..."
    kubectl apply -f reliability/pod-disruption-budgets.yaml
    
    print_info "Applying Resource Quotas..."
    kubectl apply -f reliability/resource-quotas.yaml
    
    print_info "Reliability features deployed successfully"
}

# Deploy Monitoring Stack
deploy_monitoring() {
    print_header "Deploying Monitoring Stack"
    
    print_info "Deploying Prometheus..."
    kubectl apply -f monitoring/prometheus-stack.yaml
    
    print_info "Waiting for Prometheus to be ready..."
    kubectl wait --for=condition=ready pod -l app=prometheus -n distributed-commerce --timeout=300s
    
    print_info "Deploying Grafana..."
    kubectl apply -f monitoring/grafana.yaml
    
    print_info "Waiting for Grafana to be ready..."
    kubectl wait --for=condition=ready pod -l app=grafana -n distributed-commerce --timeout=300s
    
    print_info "Deploying Loki..."
    kubectl apply -f monitoring/loki-stack.yaml
    
    print_info "Monitoring stack deployed successfully"
    
    # Get Grafana password
    GRAFANA_PASSWORD=$(kubectl get secret grafana-admin -n distributed-commerce -o jsonpath='{.data.password}' | base64 -d)
    print_info "Grafana admin password: $GRAFANA_PASSWORD"
}

# Deploy PostgreSQL HA
deploy_postgres_ha() {
    print_header "Deploying PostgreSQL High Availability"
    
    read -p "Deploy PostgreSQL HA (3 replicas)? This will replace single-node PostgreSQL. (yes/no): " confirm
    if [ "$confirm" = "yes" ]; then
        print_warning "Backing up existing PostgreSQL data..."
        kubectl exec postgres-0 -n distributed-commerce -- pg_dumpall -U postgres > postgres-backup-$(date +%Y%m%d-%H%M%S).sql
        
        print_info "Deploying PostgreSQL HA with Patroni..."
        kubectl apply -f infrastructure/postgres-ha.yaml
        
        print_info "Waiting for PostgreSQL HA to be ready..."
        kubectl wait --for=condition=ready pod -l app=postgres-ha -n distributed-commerce --timeout=600s
        
        print_info "PostgreSQL HA deployed successfully"
    else
        print_warning "Skipping PostgreSQL HA deployment"
    fi
}

# Deploy Service Mesh
deploy_service_mesh() {
    print_header "Deploying Istio Service Mesh"
    
    read -p "Deploy Istio Service Mesh? (yes/no): " confirm
    if [ "$confirm" = "yes" ]; then
        if ! command -v istioctl &> /dev/null; then
            print_error "istioctl not found. Please install Istio first."
            print_info "Visit: https://istio.io/latest/docs/setup/getting-started/"
            return
        fi
        
        print_info "Installing Istio with production profile..."
        istioctl install --set profile=production -y
        
        print_info "Enabling Istio injection for namespace..."
        kubectl label namespace distributed-commerce istio-injection=enabled --overwrite
        
        print_info "Applying Istio configurations..."
        kubectl apply -f service-mesh/istio-config.yaml
        
        print_info "Restarting pods to inject Istio sidecars..."
        kubectl rollout restart deployment -n distributed-commerce
        
        print_info "Istio service mesh deployed successfully"
    else
        print_warning "Skipping Istio deployment"
    fi
}

# Deploy Infrastructure
deploy_infrastructure() {
    print_header "Deploying Infrastructure Services"
    
    print_info "Deploying infrastructure..."
    kubectl apply -f infrastructure/
    
    print_info "Waiting for infrastructure to be ready..."
    sleep 30
}

# Deploy Services
deploy_services() {
    print_header "Deploying Microservices"
    
    print_info "Deploying services..."
    kubectl apply -f services/
    
    print_info "Waiting for services to be ready..."
    sleep 20
}

# Run Database Migrations
run_migrations() {
    print_header "Running Database Migrations"
    
    read -p "Run database migrations? (yes/no): " confirm
    if [ "$confirm" = "yes" ]; then
        print_info "Running migrations..."
        kubectl apply -f jobs/db-migration-jobs.yaml
        
        print_info "Waiting for migrations to complete..."
        kubectl wait --for=condition=complete job -l app=db-migration -n distributed-commerce --timeout=600s
        
        print_info "Database migrations completed"
    else
        print_warning "Skipping database migrations"
    fi
}

# Setup Backups
setup_backups() {
    print_header "Setting Up Backup Strategy"
    
    print_info "Deploying backup CronJobs..."
    kubectl apply -f backup/backup-schedule.yaml
    
    print_info "Backup strategy configured"
}

# Display Access Information
display_info() {
    print_header "Access Information"
    
    echo ""
    print_info "Service Endpoints:"
    kubectl get svc -n distributed-commerce
    
    echo ""
    print_info "To access Grafana:"
    echo "  kubectl port-forward svc/grafana-service 3000:3000 -n distributed-commerce"
    echo "  URL: http://localhost:3000"
    echo "  Username: admin"
    echo "  Password: Run 'kubectl get secret grafana-admin -n distributed-commerce -o jsonpath=\"{.data.password}\" | base64 -d'"
    
    echo ""
    print_info "To access Prometheus:"
    echo "  kubectl port-forward svc/prometheus-service 9090:9090 -n distributed-commerce"
    echo "  URL: http://localhost:9090"
    
    echo ""
    print_info "To access API Gateway:"
    API_GATEWAY_IP=$(kubectl get svc api-gateway-service -n distributed-commerce -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
    if [ -z "$API_GATEWAY_IP" ]; then
        echo "  kubectl port-forward svc/api-gateway-service 8080:80 -n distributed-commerce"
        echo "  URL: http://localhost:8080"
    else
        echo "  URL: http://$API_GATEWAY_IP"
    fi
}

# Main execution
main() {
    echo ""
    echo "╔════════════════════════════════════════════════════════════╗"
    echo "║                                                            ║"
    echo "║  Distributed Commerce - Complete Stack Deployment         ║"
    echo "║                                                            ║"
    echo "╚════════════════════════════════════════════════════════════╝"
    echo ""
    
    check_prerequisites
    
    # Create namespace
    kubectl apply -f base/namespace.yaml
    kubectl apply -f base/configmap.yaml
    
    # Deploy in order
    deploy_external_secrets
    # deploy_sealed_secrets  # Alternative to External Secrets
    deploy_security
    deploy_reliability
    deploy_infrastructure
    deploy_postgres_ha
    deploy_monitoring
    deploy_services
    run_migrations
    setup_backups
    deploy_service_mesh
    
    echo ""
    print_header "Deployment Summary"
    kubectl get all -n distributed-commerce
    
    echo ""
    display_info
    
    echo ""
    print_header "Deployment Complete!"
    print_info "Your production-ready microservices platform is deployed! 🚀"
    echo ""
}

# Run main function
main
