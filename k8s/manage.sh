#!/usr/bin/env bash
# k8s/manage.sh
# Script d'administration local pour la gestion du cluster kind et des namespaces WicStock.
# Permet d'allumer/éteindre sélectivement chaque module pour minimiser la mémoire RAM/CPU.

set -e

CLUSTER_NAME="wicstock-cluster"
REGISTRY="ghcr.io/haifacheikh/wicstockproject"

function show_help() {
  echo "================================================================="
  echo " 🚀 WicStock Kubernetes Cluster & Namespace Manager"
  echo "================================================================="
  echo " Usage: ./k8s/manage.sh <command>"
  echo ""
  echo " Cluster Management:"
  echo "   create-cluster       Crée le cluster kind 2 nœuds (dev)"
  echo "   create-demo-cluster  Crée le cluster kind 3 nœuds (démo/screenshots)"
  echo "   delete-cluster       Supprime le cluster kind"
  echo "   install-ingress      Installe NGINX Ingress Controller"
  echo ""
  echo " Namespace Management (Dev à la demande):"
  echo "   start-core           Déploie wicstock-core (PostgreSQL + API + Frontend)"
  echo "   stop-core            Arrête wicstock-core"
  echo "   start-ai             Déploie wicstock-ai (Service IA + ChromaDB)"
  echo "   stop-ai              Arrête wicstock-ai"
  echo "   start-obs            Déploie wicstock-observability (Prometheus + Grafana)"
  echo "   stop-obs             Arrête wicstock-observability"
  echo "   start-all            Déploie tous les namespaces"
  echo "   stop-all             Arrête tous les namespaces"
  echo ""
  echo " Images & Status:"
  echo "   load-images          Charge les images Docker locales directement dans kind"
  echo "   push-ghcr            Pousse les images vers GHCR (GitHub Container Registry)"
  echo "   status               Affiche l'état des Pods, Services et Ingress"
  echo "   top                  Affiche la consommation RAM/CPU réelle via kubectl top"
  echo "================================================================="
}

case "$1" in
  create-cluster)
    echo "📌 Création du cluster kind dev (2 nœuds)..."
    kind create cluster --config k8s/kind-config.yaml
    ;;
  create-demo-cluster)
    echo "📌 Création du cluster kind démo (3 nœuds)..."
    kind create cluster --config k8s/kind-config-demo.yaml
    ;;
  delete-cluster)
    echo "🗑️ Suppression du cluster kind..."
    kind delete cluster --name $CLUSTER_NAME
    ;;
  install-ingress)
    echo "🌐 Installation de NGINX Ingress Controller..."
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
    echo "⏳ Attente du démarrage de NGINX Ingress..."
    kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=120s
    ;;
  start-core)
    echo "🚀 Déploiement du namespace wicstock-core..."
    kubectl apply -f k8s/manifests/wicstock-core/
    ;;
  stop-core)
    echo "🛑 Arrêt du namespace wicstock-core..."
    kubectl delete -f k8s/manifests/wicstock-core/ --ignore-not-found
    ;;
  start-ai)
    echo "🤖 Déploiement du namespace wicstock-ai..."
    kubectl apply -f k8s/manifests/wicstock-ai/
    ;;
  stop-ai)
    echo "🛑 Arrêt du namespace wicstock-ai..."
    kubectl delete -f k8s/manifests/wicstock-ai/ --ignore-not-found
    ;;
  start-obs)
    echo "🔭 Déploiement du namespace wicstock-observability..."
    kubectl apply -f k8s/manifests/wicstock-observability/
    ;;
  stop-obs)
    echo "🛑 Arrêt du namespace wicstock-observability..."
    kubectl delete -f k8s/manifests/wicstock-observability/ --ignore-not-found
    ;;
  start-all)
    echo "🚀 Déploiement de TOUS les namespaces..."
    kubectl apply -f k8s/manifests/wicstock-core/
    kubectl apply -f k8s/manifests/wicstock-ai/
    kubectl apply -f k8s/manifests/wicstock-observability/
    ;;
  stop-all)
    echo "🛑 Arrêt de TOUS les namespaces..."
    kubectl delete -f k8s/manifests/wicstock-observability/ --ignore-not-found
    kubectl delete -f k8s/manifests/wicstock-ai/ --ignore-not-found
    kubectl delete -f k8s/manifests/wicstock-core/ --ignore-not-found
    ;;
  load-images)
    echo "📦 Chargement des images locales dans kind..."
    kind load docker-image wicstock-api:latest --name $CLUSTER_NAME
    kind load docker-image wicstock-web:latest --name $CLUSTER_NAME
    kind load docker-image wicstock-ai:latest --name $CLUSTER_NAME
    ;;
  push-ghcr)
    echo "📤 Push des images vers GHCR ($REGISTRY)..."
    docker tag wicstock-api:latest $REGISTRY/wicstock-api:latest
    docker tag wicstock-web:latest $REGISTRY/wicstock-web:latest
    docker tag wicstock-ai:latest $REGISTRY/wicstock-ai:latest
    docker push $REGISTRY/wicstock-api:latest
    docker push $REGISTRY/wicstock-web:latest
    docker push $REGISTRY/wicstock-ai:latest
    ;;
  status)
    echo "📊 État global du cluster Kubernetes :"
    kubectl get pods -A
    echo ""
    kubectl get services -A
    echo ""
    kubectl get ingress -A
    ;;
  top)
    echo "💻 Consommation CPU / RAM en temps réel :"
    kubectl top nodes
    echo ""
    kubectl top pods -A
    ;;
  *)
    show_help
    ;;
esac
