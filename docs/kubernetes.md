# ☸️ Architecture Kubernetes & Helm

WicStock dispose d'une infrastructure **Kubernetes native**, organisée par **namespaces modulaires à la demande** et gérée par **Helm 3**. Cette architecture minimise l'empreinte RAM/CPU en développement local tout en gardant des profils de production scalables.

## 1. Découpage par namespaces modulaires

| Namespace | Composants | Stratégie |
|---|---|---|
| **`wicstock-core`** | PostgreSQL + Backend API + Frontend Blazor + Ingress | **Socle quotidien** : toujours allumé (~288 Mi RAM) |
| **`wicstock-ai`** | Microservice IA FastAPI + Vectorstore ChromaDB | **À la demande** : allumé uniquement pour tester les fonctions IA (~256 Mi RAM) |
| **`wicstock-observability`** | Prometheus + Grafana | **À la demande** : allumé pour démo/métriques (~192 Mi RAM) |

> 🔒 **Sécurité des secrets** : les secrets ne sont jamais committés en clair. En local, ils sont injectés via `kubectl create secret` ; en production via un gestionnaire externe (*Sealed Secrets*, *External Secrets Operator* / Vault). Voir [`security.md`](security.md).

## 2. Profils de ressources & limites

| Composant | Profil Dev (Requests / Limits) | Profil Prod (Requests / Limits) | Health Probes |
|---|---|---|---|
| **PostgreSQL** | `50m / 250m` CPU · `128Mi / 256Mi` RAM | `250m / 1000m` CPU · `512Mi / 2Gi` RAM | `pg_isready -U wicstock_user -d wicstock_db` |
| **Backend API** | `50m / 300m` CPU · `128Mi / 256Mi` RAM | `200m / 1000m` CPU · `512Mi / 1Gi` RAM | `GET /health/live` & `GET /health/ready` (port 8080) |
| **Frontend Blazor** | `20m / 100m` CPU · `32Mi / 64Mi` RAM | `50m / 250m` CPU · `128Mi / 256Mi` RAM | `GET /` (port 80) |
| **AI Service (FastAPI)** | `100m / 500m` CPU · `256Mi / 768Mi` RAM | `500m / 2000m` CPU · `1Gi / 4Gi` RAM | `GET /health` (port 8000) |
| **Prometheus** | `50m / 200m` CPU · `128Mi / 256Mi` RAM | `200m / 1000m` CPU · `512Mi / 2Gi` RAM | `emptyDir` (dev) vs `PVC` + 15j (prod) |
| **Grafana** | `20m / 100m` CPU · `64Mi / 128Mi` RAM | `100m / 500m` CPU · `256Mi / 512Mi` RAM | `GET /api/health` (port 3000) |

## 3. Empreinte réelle mesurée (`kubectl top`)

Sur un cluster kind à 2 nœuds (1 control-plane + 1 worker), les 3 namespaces déployés ensemble consomment environ **7% de la RAM totale des nœuds** — mesuré avec `metrics-server` sur une machine de 24 Go de RAM.

## 4. Guide de démarrage rapide (kind)

```bash
# 1. Créer le cluster kind (2 nœuds, ports 80/443 mappés)
kind create cluster --config k8s/kind-config.yaml

# (Optionnel : cluster 3 nœuds pour démo multi-nœuds / captures d'écran)
# kind create cluster --config k8s/kind-config-demo.yaml

# 2. Installer NGINX Ingress Controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=120s

# 3. Lancer le socle quotidien wicstock-core
kubectl apply -f k8s/manifests/wicstock-core/

# 4. (Optionnel) Lancer le module IA et/ou Observabilité à la demande
kubectl apply -f k8s/manifests/wicstock-ai/
kubectl apply -f k8s/manifests/wicstock-observability/

# 5. Script d'administration interactif
./k8s/manage.sh status     # PowerShell: .\k8s\manage.ps1 status
./k8s/manage.sh top        # Consommation CPU / RAM en temps réel
```

> 💡 **Note d'accès sous Windows / kind** : selon la configuration réseau Docker Desktop / WSL2, l'accès local aux services Ingress, Grafana ou Prometheus peut nécessiter `kubectl port-forward` plutôt que le `hostPort` mappé par kind (limitation connue de WSL2). Exemple :
> ```bash
> kubectl port-forward svc/grafana-service 3000:3000 -n wicstock-observability
> ```

## 5. Déploiement via Helm 3 (`helm/wicstock`)

```bash
# Profil Dev (ultra-léger)
helm install wicstock ./helm/wicstock --set resources.profile=dev

# Profil Prod (ressources élevées + persistence PVC)
helm install wicstock-prod ./helm/wicstock --set resources.profile=prod --set environment=prod
```

---

⬅ [Back to README](../README.md)
