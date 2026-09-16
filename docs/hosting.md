# ☁️ Hébergement & Déploiement Cloud

| Composant | Plateforme | Détails |
|---|---|---|
| **Frontend Blazor WebAssembly** | **Vercel** (`vercel.json`) | Routage Single Page Application (SPA) |
| **Frontend (miroir)** | **GitHub Pages** | Synchronisé automatiquement via la CD pipeline |
| **Backend API** | **Render.com** (`render.yaml`) | Déploiement déclaratif (Infrastructure as Code) |
| **Base de données** | **PostgreSQL managé** | Hébergé sur Render.com |

## Infrastructure as Code

Le fichier `render.yaml` décrit l'infrastructure backend en code plutôt qu'en configuration manuelle — c'est la brique d'Infrastructure as Code (IaC) actuellement en place pour l'hébergement de production.

> 📌 Le cluster Kubernetes local (kind) et Argo CD servent de démonstration de compétences DevOps avancées et sont indépendants de ces hébergements de production — voir [`kubernetes.md`](kubernetes.md) et [`gitops.md`](gitops.md).

---

⬅ [Back to README](../README.md)
