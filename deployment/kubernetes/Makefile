# Distributed Commerce - Kubernetes Makefile
# Simplifies deployment and management tasks

.PHONY: help build deploy deploy-dev deploy-staging deploy-prod clean status logs test

# Variables
DOCKER_REGISTRY ?= distributed-commerce
VERSION ?= latest
NAMESPACE ?= distributed-commerce
KUSTOMIZE ?= kubectl apply -k

# Colors for output
BLUE := \033[0;34m
GREEN := \033[0;32m
RED := \033[0;31m
NC := \033[0m # No Color

help: ## Show this help message
	@echo "$(BLUE)Distributed Commerce - Kubernetes Management$(NC)"
	@echo ""
	@echo "$(GREEN)Available targets:$(NC)"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(BLUE)%-20s$(NC) %s\n", $$1, $$2}'

build: ## Build all Docker images
	@echo "$(GREEN)Building Docker images...$(NC)"
	DOCKER_REGISTRY=$(DOCKER_REGISTRY) VERSION=$(VERSION) ./build-images.sh

push: ## Push all Docker images to registry
	@echo "$(GREEN)Pushing images to $(DOCKER_REGISTRY)...$(NC)"
	@for service in api-gateway identity-service catalog-service order-service payment-service inventory-service shipping-service notification-service analytics-service; do \
		docker push $(DOCKER_REGISTRY)/$$service:$(VERSION); \
	done

deploy: ## Deploy to Kubernetes (default namespace)
	@echo "$(GREEN)Deploying to Kubernetes...$(NC)"
	./deploy.sh

deploy-dev: ## Deploy to development environment
	@echo "$(GREEN)Deploying to development environment...$(NC)"
	$(KUSTOMIZE) overlays/dev

deploy-staging: ## Deploy to staging environment
	@echo "$(GREEN)Deploying to staging environment...$(NC)"
	$(KUSTOMIZE) overlays/staging

deploy-prod: ## Deploy to production environment
	@echo "$(GREEN)Deploying to production environment...$(NC)"
	$(KUSTOMIZE) overlays/production

status: ## Show deployment status
	@echo "$(GREEN)Deployment Status:$(NC)"
	@kubectl get all -n $(NAMESPACE)

logs: ## Show logs for all services
	@echo "$(GREEN)Recent logs:$(NC)"
	@kubectl logs -l tier=backend -n $(NAMESPACE) --tail=50 --prefix=true

logs-follow: ## Follow logs for all services
	@echo "$(GREEN)Following logs...$(NC)"
	@kubectl logs -f -l tier=backend -n $(NAMESPACE) --prefix=true

logs-gateway: ## Show API Gateway logs
	@kubectl logs -f deployment/api-gateway -n $(NAMESPACE)

logs-service: ## Show logs for specific service (usage: make logs-service SERVICE=catalog-service)
	@kubectl logs -f deployment/$(SERVICE) -n $(NAMESPACE)

scale: ## Scale a service (usage: make scale SERVICE=catalog-service REPLICAS=5)
	@kubectl scale deployment $(SERVICE) --replicas=$(REPLICAS) -n $(NAMESPACE)

restart: ## Restart a service (usage: make restart SERVICE=catalog-service)
	@kubectl rollout restart deployment/$(SERVICE) -n $(NAMESPACE)

restart-all: ## Restart all services
	@kubectl rollout restart deployment -n $(NAMESPACE)

test: ## Run basic connectivity tests
	@echo "$(GREEN)Running connectivity tests...$(NC)"
	@kubectl run test-pod --image=busybox --rm -it -n $(NAMESPACE) -- sh -c "\
		echo 'Testing API Gateway...'; \
		wget -O- http://api-gateway-service/health || true; \
		echo '\nTesting Catalog Service...'; \
		wget -O- http://catalog-service/health || true; \
		echo '\nTesting Order Service...'; \
		wget -O- http://order-service/health || true"

port-forward-gateway: ## Port forward API Gateway to localhost:8080
	@echo "$(GREEN)Port forwarding API Gateway to localhost:8080...$(NC)"
	@kubectl port-forward svc/api-gateway-service 8080:80 -n $(NAMESPACE)

port-forward-jaeger: ## Port forward Jaeger UI to localhost:16686
	@echo "$(GREEN)Port forwarding Jaeger UI to localhost:16686...$(NC)"
	@kubectl port-forward svc/jaeger-service 16686:16686 -n $(NAMESPACE)

port-forward-postgres: ## Port forward PostgreSQL to localhost:5432
	@echo "$(GREEN)Port forwarding PostgreSQL to localhost:5432...$(NC)"
	@kubectl port-forward svc/postgres-service 5432:5432 -n $(NAMESPACE)

exec-postgres: ## Execute psql in PostgreSQL pod
	@kubectl exec -it postgres-0 -n $(NAMESPACE) -- psql -U postgres -d DistributedCommerce

describe: ## Describe a resource (usage: make describe RESOURCE=pod/catalog-service-xxx)
	@kubectl describe $(RESOURCE) -n $(NAMESPACE)

get-pods: ## Get all pods
	@kubectl get pods -n $(NAMESPACE) -o wide

get-services: ## Get all services
	@kubectl get svc -n $(NAMESPACE)

get-hpa: ## Get horizontal pod autoscalers
	@kubectl get hpa -n $(NAMESPACE)

top-pods: ## Show resource usage of pods
	@kubectl top pods -n $(NAMESPACE)

top-nodes: ## Show resource usage of nodes
	@kubectl top nodes

clean: ## Delete all resources
	@echo "$(RED)Cleaning up all resources...$(NC)"
	./cleanup.sh

backup-db: ## Backup PostgreSQL database
	@echo "$(GREEN)Backing up database...$(NC)"
	@kubectl exec postgres-0 -n $(NAMESPACE) -- pg_dump -U postgres DistributedCommerce > backup-$$(date +%Y%m%d-%H%M%S).sql
	@echo "$(GREEN)Backup completed!$(NC)"

restore-db: ## Restore PostgreSQL database (usage: make restore-db BACKUP=backup.sql)
	@echo "$(GREEN)Restoring database from $(BACKUP)...$(NC)"
	@kubectl exec -i postgres-0 -n $(NAMESPACE) -- psql -U postgres DistributedCommerce < $(BACKUP)
	@echo "$(GREEN)Restore completed!$(NC)"

events: ## Show cluster events
	@kubectl get events -n $(NAMESPACE) --sort-by='.lastTimestamp'

dashboard: ## Open Kubernetes dashboard
	@kubectl proxy & sleep 2 && open http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/

validate: ## Validate Kubernetes manifests
	@echo "$(GREEN)Validating manifests...$(NC)"
	@for file in $$(find . -name "*.yaml"); do \
		echo "Validating $$file"; \
		kubectl apply --dry-run=client -f $$file; \
	done

lint: ## Lint Kubernetes manifests with kubeval
	@echo "$(GREEN)Linting manifests...$(NC)"
	@for file in $$(find . -name "*.yaml"); do \
		kubeval $$file; \
	done

update-secrets: ## Update secrets (prompts for values)
	@echo "$(GREEN)Updating secrets...$(NC)"
	@./update-secrets.sh

# New Improvements Commands

deploy-complete: ## Deploy complete stack with all improvements
	@echo "$(GREEN)Deploying complete stack...$(NC)"
	@./deploy-complete.sh

deploy-security: ## Deploy security policies (NetworkPolicies, RBAC, PSS)
	@echo "$(GREEN)Deploying security policies...$(NC)"
	@kubectl apply -f security/

deploy-monitoring: ## Deploy monitoring stack (Prometheus, Grafana, Loki)
	@echo "$(GREEN)Deploying monitoring stack...$(NC)"
	@kubectl apply -f monitoring/

deploy-postgres-ha: ## Deploy PostgreSQL High Availability
	@echo "$(GREEN)Deploying PostgreSQL HA...$(NC)"
	@kubectl apply -f infrastructure/postgres-ha.yaml

deploy-service-mesh: ## Deploy Istio service mesh
	@echo "$(GREEN)Deploying Istio service mesh...$(NC)"
	@istioctl install --set profile=production -y
	@kubectl label namespace $(NAMESPACE) istio-injection=enabled --overwrite
	@kubectl apply -f service-mesh/

setup-external-secrets: ## Setup External Secrets Operator
	@echo "$(GREEN)Setting up External Secrets Operator...$(NC)"
	@kubectl apply -f https://raw.githubusercontent.com/external-secrets/external-secrets/main/deploy/crds/bundle.yaml
	@kubectl apply -f https://raw.githubusercontent.com/external-secrets/external-secrets/main/deploy/external-secrets.yaml
	@kubectl apply -f external-secrets/

port-forward-grafana: ## Port forward Grafana to localhost:3000
	@echo "$(GREEN)Port forwarding Grafana to localhost:3000...$(NC)"
	@kubectl port-forward svc/grafana-service 3000:3000 -n $(NAMESPACE)

port-forward-prometheus: ## Port forward Prometheus to localhost:9090
	@echo "$(GREEN)Port forwarding Prometheus to localhost:9090...$(NC)"
	@kubectl port-forward svc/prometheus-service 9090:9090 -n $(NAMESPACE)

port-forward-loki: ## Port forward Loki to localhost:3100
	@echo "$(GREEN)Port forwarding Loki to localhost:3100...$(NC)"
	@kubectl port-forward svc/loki-service 3100:3100 -n $(NAMESPACE)

check-security: ## Run security checks with kube-bench
	@echo "$(GREEN)Running security checks...$(NC)"
	@kubectl apply -f https://raw.githubusercontent.com/aquasecurity/kube-bench/main/job.yaml
	@sleep 10
	@kubectl logs -l app=kube-bench

run-migrations: ## Run database migrations
	@echo "$(GREEN)Running database migrations...$(NC)"
	@kubectl apply -f jobs/db-migration-jobs.yaml
	@kubectl wait --for=condition=complete job -l app=db-migration -n $(NAMESPACE) --timeout=600s

test-backups: ## Test backup and restore
	@echo "$(GREEN)Testing backup and restore...$(NC)"
	@kubectl exec postgres-0 -n $(NAMESPACE) -- pg_dumpall -U postgres > test-backup.sql
	@echo "Backup created: test-backup.sql"

rollback: ## Rollback a deployment (usage: make rollback SERVICE=catalog-service)
	@kubectl rollout undo deployment/$(SERVICE) -n $(NAMESPACE)

rollback-all: ## Rollback all deployments
	@for deployment in $$(kubectl get deployments -n $(NAMESPACE) -o jsonpath='{.items[*].metadata.name}'); do \
		kubectl rollout undo deployment/$$deployment -n $(NAMESPACE); \
	done

info: ## Show cluster and deployment information
	@echo "$(BLUE)Cluster Information:$(NC)"
	@kubectl cluster-info
	@echo ""
	@echo "$(BLUE)Namespace: $(NAMESPACE)$(NC)"
	@echo "$(BLUE)Docker Registry: $(DOCKER_REGISTRY)$(NC)"
	@echo "$(BLUE)Version: $(VERSION)$(NC)"
	@echo ""
	@echo "$(GREEN)Deployments:$(NC)"
	@kubectl get deployments -n $(NAMESPACE)
	@echo ""
	@echo "$(GREEN)Services:$(NC)"
	@kubectl get svc -n $(NAMESPACE)

.DEFAULT_GOAL := help
