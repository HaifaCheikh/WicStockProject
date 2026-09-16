# 🔄 Pipelines CI/CD (GitHub Actions)

```
[ Git Push / PR ] ──> [ CI: Build .NET + Python + Docker ] ──> [ Trivy Scan ] ──> [ CD: Deployment ]
```

## CI Pipeline (`.github/workflows/ci.yml`)

| Job | Description |
|---|---|
| `backend` | Restauration et compilation Release de `backend/WicStock.Api.csproj` |
| `frontend` | Restauration et compilation Release de `frontend/WicStock.Web.csproj` |
| `ai-service` | Validation de l'environnement Python 3.11 et installation de `requirements.txt` |
| `docker-build` | Construction des 4 images Docker (`wicstock-api`, `wicstock-web`, `wicstock-ai`, `wicstock-whatsapp`) |
| `trivy-scan` | Scan de vulnérabilités (CRITICAL, HIGH) avec génération de rapports SARIF |

Déclenchement : à chaque `push` ou `pull_request` sur `main`.

## CD Pipeline (`.github/workflows/deploy.yml`)

- Déclenchement automatique post-succès de la CI sur `main`
- Publication automatique du frontend Blazor WebAssembly sur **GitHub Pages** (gestion du fichier `.nojekyll`)

---

⬅ [Back to README](../README.md)
