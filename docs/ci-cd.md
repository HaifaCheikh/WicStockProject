# 🚀 WicStock - Intégration & Déploiement Continus (CI/CD), Qualité du Code & Coverage

Ce document détaille l'architecture et le fonctionnement de la chaîne d'Intégration et de Déploiement Continus (**CI/CD**), la stratégie de **Tests Unitaires**, et l'analyse de la **Qualité du Code** (SAST & Couverture Codecov / SonarCloud).

---

## 🛠️ 1. Pipeline d'Intégration Continue (GitHub Actions)

Le workflow principal `.github/workflows/ci.yml` s'exécute automatiquement à chaque `push` et `pull_request` vers la branche `main` :

1. **Build & Unit Testing (.NET 8)** :
   - Restauration des dépendances et compilation de la solution `WicStock.sln`.
   - Exécution des tests unitaires backend xUnit (`WicStock.Api.Tests`).
   - Collecte de la couverture de code via `coverlet.collector` (format Cobertura).
2. **Validation Frontend (Blazor WebAssembly)** :
   - Vérification de la compilation du client Blazor.
3. **Validation Microservice IA (Python / FastAPI)** :
   - Installation des dépendances et vérification syntaxique du service IA.
4. **Validation des Builds Docker** :
   - Verification multi-stage des conteneurs (`backend`, `frontend`, `ai-service`, `whatsapp-service`).
5. **Analyse de Sécurité DevSecOps (Trivy & CodeQL)** :
   - Scan SAST CodeQL sur le code source C#.
   - Scan des vulnérabilités d'images Docker via Trivy avec génération de rapports SARIF.
6. **Publication d'Images GHCR & GitOps Tag Bump** :
   - Publication des images sur GitHub Container Registry (`ghcr.io/haifacheikh/wicstockproject/...`).
   - Création et auto-merge d'une Pull Request pour mettre à jour `values.yaml` dans Argo CD.

---

## 🧪 2. Stratégie de Tests Unitaires & Couverture de Code

### 🎯 Objectif & Philosophie
- **Couverture visée** : **40% à 60%** ciblée en priorité sur la **logique métier critique**.
- **Focus** : Les algorithmes de diagnostic de surstock, le calcul des risques de péremption/immobilisation et les règles anti-faux-positifs.
- **Exclusions volontaires** : Les contrôleurs REST simples (qui ne font que déléguer), les DTOs, les migrations EF Core, et le code d'authentification JWT standard.

### 🧩 Composants Testés (`backend/WicStock.Api.Tests`)
| Classe de Test | Composant Cible | Logique Métier Validée |
|---|---|---|
| `AnalyseSurstockServiceTests.cs` | `AnalyseSurstockService` | Calcul des surplus, mode dégradé (fallback sans IA), actions recommandées (promotions ciblées, recyclage). |
| `MetriquesStockServiceTests.cs` | `MetriquesStockService` | Seuil de surstock métier (100 u.), règle anti-faux-positif 21 jours, calcul du pourcentage au-dessus du seuil (EF Core In-Memory). |

### 💻 Exécution des Tests en Local
Pour exécuter la suite de tests et générer le rapport de couverture en local :

```powershell
# Exécution des tests unitaires avec collecte de couverture
dotnet test backend/WicStock.Api.Tests/WicStock.Api.Tests.csproj --collect:"XPlat Code Coverage"
```

---

## 📊 3. Qualité du Code (Codecov & SonarCloud)

### 📈 Codecov (Rapport de Couverture)
- **Rôle** : Reçoit les rapports XML de couverture générés par Coverlet lors des builds CI et affiche l'évolution de la couverture par Pull Request.
- **Configuration** : Action officielle `codecov/codecov-action@v4`.
- **Secret requis** : `CODECOV_TOKEN` configuré dans les secrets du dépôt GitHub.

### 🛡️ SonarCloud (Analyse SAST & Quality Gate)
- **Rôle** : Analyse statique du code C# (detection des bugs, doutes sur la sécurité, doutes sur la maintenabilité et duplications).
- **Configuration** : `sonar-project.properties` à la racine du dépôt.
- **Mode actuel** : Informatif (`continue-on-error: true`), pour ne pas bloquer les déploiements pendant la phase de stabilisation initiale.
- **Secret requis** : `SONAR_TOKEN` configuré dans les secrets du dépôt GitHub.

---

## 🚀 4. Guide d'Activation des Badges (Codecov & SonarCloud)

Pour activer l'affichage vert des badges dans le `README.md` :

1. **Activer Codecov** :
   - Connectez-vous sur [codecov.io](https://codecov.io) avec votre compte GitHub.
   - Ajoutez le projet `HaifaCheikh/WicStockProject` et copiez le token d'upload.
   - Dans GitHub : **Settings ➔ Secrets and variables ➔ Actions ➔ New repository secret** ➔ Nom : `CODECOV_TOKEN`.

2. **Activer SonarCloud** :
   - Connectez-vous sur [sonarcloud.io](https://sonarcloud.io) avec votre compte GitHub.
   - Créez votre organisation (`haifacheikh`) et importez `HaifaCheikh/WicStockProject`.
   - Générez un token sous **My Account ➔ Security**.
   - Dans GitHub : **Settings ➔ Secrets and variables ➔ Actions ➔ New repository secret** ➔ Nom : `SONAR_TOKEN`.
