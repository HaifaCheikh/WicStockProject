# 🛡️ Sécurité & DevSecOps

WicStock intègre une suite de protections **DevSecOps automatisées**, 100% cloud-native et intégrées au workflow GitHub.

## Vue d'ensemble

| Outil | Champ d'application | Fréquence / déclencheur | Canal de remontée |
|---|---|---|---|
| **Dependabot** | Dépendances NuGet, Pip, npm, Docker & GitHub Actions | Hebdomadaire | Pull Requests groupées & Security tab |
| **CodeQL (SAST)** | Analyse statique du code C# (Backend API & Frontend Blazor) | Push, PR & cron (lundi 03:00 UTC) | GitHub Security > Code scanning |
| **Trivy (Container Scan)** | Vulnérabilités (CRITICAL, HIGH) des 4 images Docker | CI Pipeline (`trivy-scan`) | GitHub Security SARIF & logs CI |
| **Branch Protection** | Branche `main` protégée (PR obligatoire, checks CI/CodeQL/Trivy requis) | En continu | Règles GitHub |

## Détails

1. **Dependabot** — mises à jour automatisées par écosystème, avec règles `ignore` sur les sauts de version majeurs des SDK critiques (`.NET SDK 8→10`, `Python 3.11→3.14`, `Node 20→26`) pour éviter les ruptures en production.
2. **CodeQL** — détection des vulnérabilités de code (injection SQL, failles d'autorisation, fuites de données) directement dans l'onglet Security de GitHub.
3. **Trivy** — inspecte le système de fichiers et les packages OS des images Docker construites en CI ; rapports SARIF centralisés.
4. **Politique de protection `main`** — interdiction des push directs, toutes les validations de sécurité doivent passer avant intégration.

## Mises à jour de sécurité appliquées

- **`chromadb`** → mis à jour vers `1.5.9`, résolvant une vulnérabilité RCE (*ChromaToast*, `CVE-2026-45829`)
- **`python-dotenv`** → mis à jour vers `1.2.3`, résolvant une vulnérabilité de traversée de liens symboliques (`CVE-2026-28684`)
- **`sqlparse`** → supprimé de `requirements.txt` (dépendance inutile, 6 vulnérabilités DoS/ReDoS éliminées)

## Risques résiduels connus — ChromaDB

Les vulnérabilités signalées par Dependabot sur ChromaDB (isolation multi-tenant, injection via serveur HTTP distant) concernent le **serveur HTTP indépendant** de ChromaDB lorsqu'il est exposé publiquement.

Mitigation par l'architecture WicStock :
- ChromaDB tourne en **mode embarqué persistant local** (`chromadb.PersistentClient`), enfermé dans le conteneur privé `ai-service`
- Le port ChromaDB n'est **jamais exposé sur Internet**
- L'application est **mono-tenant**, éliminant les risques de fuite inter-tenants

## Gestion des secrets Kubernetes

Aucun secret n'est committé en clair dans le dépôt. Options d'injection :
- `kubectl create secret` (usage local/dev)
- **Bitnami Sealed Secrets** ou **External Secrets Operator** / Vault (production) — voir [`gitops.md`](gitops.md)

---

⬅ [Back to README](../README.md)
