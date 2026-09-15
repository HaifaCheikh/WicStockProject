# k8s/manage.ps1
# Script PowerShell d'administration local pour Windows

param (
    [Parameter(Position=0)]
    [string]$Command = "help"
)

$ClusterName = "wicstock-cluster"
$Registry = "ghcr.io/haifacheikh/wicstockproject"

function Show-Help {
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host " WicStock Kubernetes Cluster & Namespace Manager (Windows)" -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host " Commandes disponibles :"
    Write-Host "   create-cluster       : Cree le cluster kind 2 noeuds (dev)"
    Write-Host "   create-demo-cluster  : Cree le cluster kind 3 noeuds (demo)"
    Write-Host "   delete-cluster       : Supprime le cluster kind"
    Write-Host "   install-ingress      : Installe NGINX Ingress Controller"
    Write-Host "   start-core           : Deploie wicstock-core (Postgres + API + Front)"
    Write-Host "   stop-core            : Arrete wicstock-core"
    Write-Host "   start-ai             : Deploie wicstock-ai (Service IA + ChromaDB)"
    Write-Host "   stop-ai              : Arrete wicstock-ai"
    Write-Host "   start-obs            : Deploie wicstock-observability (Prometheus + Grafana)"
    Write-Host "   stop-obs             : Arrete wicstock-observability"
    Write-Host "   start-all            : Deploie tous les namespaces"
    Write-Host "   stop-all             : Arrete tous les namespaces"
    Write-Host "   load-images          : Charge les images locales dans kind"
    Write-Host "   push-ghcr            : Pousse les images vers GHCR"
    Write-Host "   status               : Affiche l'etat des Pods, Services et Ingress"
    Write-Host "   top                  : Consommation CPU / RAM en temps reel"
    Write-Host "=================================================================" -ForegroundColor Cyan
}

switch ($Command) {
    "create-cluster" {
        Write-Host "Creation du cluster kind dev (2 noeuds)..." -ForegroundColor Green
        kind create cluster --config k8s/kind-config.yaml
    }
    "create-demo-cluster" {
        Write-Host "Creation du cluster kind demo (3 noeuds)..." -ForegroundColor Green
        kind create cluster --config k8s/kind-config-demo.yaml
    }
    "delete-cluster" {
        Write-Host "Suppression du cluster kind..." -ForegroundColor Yellow
        kind delete cluster --name $ClusterName
    }
    "install-ingress" {
        Write-Host "Installation de NGINX Ingress Controller..." -ForegroundColor Green
        kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
        Write-Host "Attente du demarrage de NGINX Ingress..."
        kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=120s
    }
    "start-core" {
        Write-Host "Deploiement du namespace wicstock-core..." -ForegroundColor Green
        kubectl apply -f k8s/manifests/wicstock-core/
    }
    "stop-core" {
        Write-Host "Arrete du namespace wicstock-core..." -ForegroundColor Yellow
        kubectl delete -f k8s/manifests/wicstock-core/ --ignore-not-found
    }
    "start-ai" {
        Write-Host "Deploiement du namespace wicstock-ai..." -ForegroundColor Green
        kubectl apply -f k8s/manifests/wicstock-ai/
    }
    "stop-ai" {
        Write-Host "Arrete du namespace wicstock-ai..." -ForegroundColor Yellow
        kubectl delete -f k8s/manifests/wicstock-ai/ --ignore-not-found
    }
    "start-obs" {
        Write-Host "Deploiement du namespace wicstock-observability..." -ForegroundColor Green
        kubectl apply -f k8s/manifests/wicstock-observability/
    }
    "stop-obs" {
        Write-Host "Arrete du namespace wicstock-observability..." -ForegroundColor Yellow
        kubectl delete -f k8s/manifests/wicstock-observability/ --ignore-not-found
    }
    "start-all" {
        Write-Host "Deploiement de TOUS les namespaces..." -ForegroundColor Green
        kubectl apply -f k8s/manifests/wicstock-core/
        kubectl apply -f k8s/manifests/wicstock-ai/
        kubectl apply -f k8s/manifests/wicstock-observability/
    }
    "stop-all" {
        Write-Host "Arrete de TOUS les namespaces..." -ForegroundColor Yellow
        kubectl delete -f k8s/manifests/wicstock-observability/ --ignore-not-found
        kubectl delete -f k8s/manifests/wicstock-ai/ --ignore-not-found
        kubectl delete -f k8s/manifests/wicstock-core/ --ignore-not-found
    }
    "load-images" {
        Write-Host "Chargement des images locales dans kind..." -ForegroundColor Green
        kind load docker-image wicstock-api:latest --name $ClusterName
        kind load docker-image wicstock-web:latest --name $ClusterName
        kind load docker-image wicstock-ai:latest --name $ClusterName
    }
    "push-ghcr" {
        Write-Host "Push des images vers GHCR ($Registry)..." -ForegroundColor Green
        docker tag wicstock-api:latest "$Registry/wicstock-api:latest"
        docker tag wicstock-web:latest "$Registry/wicstock-web:latest"
        docker tag wicstock-ai:latest "$Registry/wicstock-ai:latest"
        docker push "$Registry/wicstock-api:latest"
        docker push "$Registry/wicstock-web:latest"
        docker push "$Registry/wicstock-ai:latest"
    }
    "status" {
        Write-Host "Etat global du cluster Kubernetes :" -ForegroundColor Cyan
        kubectl get pods -A
        Write-Host "`nServices :" -ForegroundColor Cyan
        kubectl get services -A
        Write-Host "`nIngress :" -ForegroundColor Cyan
        kubectl get ingress -A
    }
    "top" {
        Write-Host "Consommation CPU / RAM :" -ForegroundColor Cyan
        kubectl top nodes
        kubectl top pods -A
    }
    Default {
        Show-Help
    }
}
