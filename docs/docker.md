# 🐳 Containerisation

WicStock est entièrement containerisé avec des builds multi-stages optimisés pour la production.

## Images & Services

| Service | Fichier | Détails |
|---|---|---|
| **Backend API** | `backend/Dockerfile` | Multi-stage `.NET 8` (`mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/aspnet:8.0`), port `8080` |
| **Frontend Blazor** | `frontend/Dockerfile` | Build Blazor WebAssembly servi par **Nginx Alpine**, routage SPA, port `80` |
| **Microservice IA** | `ai-service/Dockerfile` | `python:3.11-slim`, Uvicorn, FastAPI, ChromaDB (1.5.9), client Ollama |
| **Service WhatsApp** | `whatsapp-service/Dockerfile` | `node:20-alpine` pour les notifications |

## Orchestration locale

`docker-compose.yml` orchestre simultanément :
- API Backend, Frontend Web, service IA, service WhatsApp
- PostgreSQL
- Prometheus + Grafana
- Isolation réseau via un bridge Docker dédié

```bash
cp .env.example .env   # personnaliser les identifiants Grafana
docker-compose up --build
```

---

⬅ [Back to README](../README.md)
