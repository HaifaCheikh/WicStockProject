# k8s/manage.ps1
# Script PowerShell d'administration local pour Windows (Kind, Argo CD & Sealed Secrets)

param (
    [Parameter(Position=0)]
    [string]$Command = "help"
)

$ClusterName = "wicstock-cluster"
$Registry = "ghcr.io/haifacheikh/wicstockproject"

function Show-Help {
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host " WicStock Kubernetes, GitOps Argo CD & Sealed Secrets Manager" -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host " Commandes de gestion Cluster & GitOps :"
    Write-Host "   create-cluster       : Cree le cluster kind 2 noeuds (dev)"
    Write-Host "   create-demo-cluster  : Cree le cluster kind 3 noeuds (demo)"
    Write-Host "   delete-cluster       : Supprime le cluster kind"
    Write-Host "   install-ingress      : Installe NGINX Ingress Controller"
    Write-Host "   install-sealed-secrets: Installe Bitnami Sealed Secrets Controller"
    Write-Host "   backup-sealed-key   : Sauvegarde la cle maitre Sealed Secrets (hors Git)"
    Write-Host "   install-argocd       : Installe Argo CD dans le namespace argocd"
    Write-Host "   start-argocd-ui      : Lance le port-forward Argo CD sur https://localhost:8443"
    Write-Host "   get-argocd-pass      : Affiche le mot de passe admin initial d'Argo CD"
    Write-Host "   apply-root-app       : Applique le CRD Root App-of-Apps GitOps"
    Write-Host "   start-core           : [Bootstrap/Debug] Deploie wicstock-core"
    Write-Host "   stop-core            : [Bootstrap/Debug] Arrete wicstock-core"
    Write-Host "   start-ai             : [Bootstrap/Debug] Deploie wicstock-ai"
    Write-Host "   stop-ai              : [Bootstrap/Debug] Arrete wicstock-ai"
    Write-Host "   start-obs            : [Bootstrap/Debug] Deploie wicstock-observability"
    Write-Host "   stop-obs             : [Bootstrap/Debug] Arrete wicstock-observability"
    Write-Host "   status               : Affiche l'etat des Pods, Services, Ingress et Argo CD"
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
    "install-sealed-secrets" {
        Write-Host "Installation de Bitnami Sealed Secrets Controller dans kube-system..." -ForegroundColor Green
        kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.26.0/controller.yaml
        Write-Host "Attente du demarrage du controleur Sealed Secrets..."
        kubectl wait --namespace kube-system --for=condition=ready pod -l app.kubernetes.io/name=sealed-secrets --timeout=120s
    }
    "backup-sealed-key" {
        Write-Host "Sauvegarde de la cle privee maitre Sealed Secrets dans sealed-secrets-master.key..." -ForegroundColor Green
        kubectl get secret -n kube-system -l sealedsecrets.bitnami.com/sealed-secrets-key -o yaml > sealed-secrets-master.key
        Write-Host " Cle maitre sauvegardee dans sealed-secrets-master.key (exclue de Git via .gitignore)" -ForegroundColor Cyan
    }
    "install-argocd" {
        Write-Host "Installation d'Argo CD dans le namespace argocd..." -ForegroundColor Green
        kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -
        kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
        Write-Host "Attente du demarrage des composants Argo CD..."
        kubectl wait --namespace argocd --for=condition=ready pod -l app.kubernetes.io/name=argocd-server --timeout=180s
    }
    "start-argocd-ui" {
        Write-Host "Ouverture du canal port-forward vers l'UI Argo CD sur https://localhost:8443..." -ForegroundColor Green
        kubectl port-forward svc/argocd-server 8443:443 -n argocd
    }
    "get-argocd-pass" {
        Write-Host "Recuperation du mot de passe admin Argo CD..." -ForegroundColor Cyan
        $encodedPass = kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}"
        if ($encodedPass) {
            $rawPass = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($encodedPass))
            Write-Host "Mot de passe Admin Argo CD : $rawPass" -ForegroundColor Green
        } else {
            Write-Host "Secret argocd-initial-admin-secret non trouve." -ForegroundColor Yellow
        }
    }
    "apply-root-app" {
        Write-Host "Deploiement du CRD Root App-of-Apps GitOps dans Argo CD..." -ForegroundColor Green
        kubectl apply -f gitops/apps/root-app.yaml
    }
    "start-core" {
        Write-Host "[Bootstrap Mode] Deploiement du namespace wicstock-core..." -ForegroundColor Green
        kubectl apply -f k8s/manifests/wicstock-core/
    }
    "stop-core" {
        Write-Host "[Bootstrap Mode] Arrete du namespace wicstock-core..." -ForegroundColor Yellow
        kubectl delete -f k8s/manifests/wicstock-core/ --ignore-not-found
    }
    "start-ai" {
        Write-Host "[Bootstrap Mode] Deploiement du namespace wicstock-ai..." -ForegroundColor Green
        kubectl apply -f k8s/manifests/wicstock-ai/
    }
    "stop-ai" {
        Write-Host "[Bootstrap Mode] Arrete du namespace wicstock-ai..." -ForegroundColor Yellow
        kubectl delete -f k8s/manifests/wicstock-ai/ --ignore-not-found
    }
    "start-obs" {
        Write-Host "[Bootstrap Mode] Deploiement du namespace wicstock-observability..." -ForegroundColor Green
        kubectl apply -f k8s/manifests/wicstock-observability/
    }
    "stop-obs" {
        Write-Host "[Bootstrap Mode] Arrete du namespace wicstock-observability..." -ForegroundColor Yellow
        kubectl delete -f k8s/manifests/wicstock-observability/ --ignore-not-found
    }
    "status" {
        Write-Host "Etat global du cluster Kubernetes :" -ForegroundColor Cyan
        kubectl get pods -A
        Write-Host "`nApplications Argo CD :" -ForegroundColor Cyan
        kubectl get applications -n argocd
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
