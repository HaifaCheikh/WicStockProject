# 🔭 Observabilité & Monitoring

Le backend API inclut une suite d'observabilité conforme aux standards cloud-native.

## 1. Health Checks

| Endpoint | Rôle |
|---|---|
| `GET /health/live` | Liveness — vérifie que le processus API répond (requis pour Render.com et Kubernetes) |
| `GET /health/ready` (ou `/health`) | Readiness — vérifie l'API **et** la connectivité effective à la base de données (`AppDbContext`) |

Réponses au format JSON, avec horodatage et durées d'exécution.

## 2. Logging structuré (Serilog & Correlation ID)

- **Format JSON structuré** : logs produits au format `CompactJsonFormatter` en console
- **Correlation ID** : chaque requête génère ou propage l'en-tête `X-Correlation-Id`, permettant le suivi bout en bout des requêtes distribuées
- **Contextualisation automatique** : route, status code, latence (ms), IP client

## 3. Métriques Prometheus & Dashboard Grafana

- **Endpoint `/metrics`** : expose les métriques système .NET (CPU, RAM, GC) et les métriques HTTP au format Prometheus (`http_request_duration_seconds`)
- **Métriques custom** : `wicstock_products_total`, `wicstock_ai_requests_total`
- **Dashboard Grafana** : auto-provisionné au démarrage, panneau de latence HTTP en secondes avec mise à l'échelle automatique

```
+------------------+     Scrape /metrics      +-------------------+
|  WicStock API    | <----------------------- |    Prometheus      |
| (ASP.NET Core 8) |                          |   (Port 9090)      |
+------------------+                          +-------------------+
         |                                              |
         | Health checks                                v
         v                                    +-------------------+
/health/live & /health/ready                  |      Grafana       |
  (Render.com / K8s)                          |   (Port 3000)      |
                                               +-------------------+
```

## 4. Guide de test local

```bash
cp .env.example .env
# éditer .env pour choisir vos identifiants Grafana

docker-compose up --build
```

| Endpoint | URL |
|---|---|
| Liveness API | `http://localhost:8080/health/live` |
| Readiness DB | `http://localhost:8080/health/ready` |
| Métriques Prometheus | `http://localhost:8080/metrics` |
| Prometheus UI | `http://localhost:9090` |
| Grafana Dashboard | `http://localhost:3000` (identifiants dans `.env`) |

> 🔒 Aucun identifiant Grafana n'est en dur dans le code — voir [`security.md`](security.md).

---

⬅ [Back to README](../README.md)
