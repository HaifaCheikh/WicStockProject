# 🧵 WicStock — Smart Textile Inventory Management & Optimization Platform

> Academic / internship project focused on intelligent textile inventory management, AI-assisted analytics, multi-agent systems, and real-time operations.

[![CI Pipeline](https://github.com/HaifaCheikh/WicStockProject/actions/workflows/ci.yml/badge.svg)](https://github.com/HaifaCheikh/WicStockProject/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat&logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![FastAPI](https://img.shields.io/badge/FastAPI-009688?style=flat&logo=fastapi)](https://fastapi.tiangolo.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-Kind-326CE5?style=flat&logo=kubernetes)](https://kubernetes.io/)
[![Argo CD](https://img.shields.io/badge/GitOps-Argo%20CD-EF6C00?style=flat&logo=argo)](https://argoproj.github.io/cd/)
[![Helm 3](https://img.shields.io/badge/Helm-v3-0F1689?style=flat&logo=helm)](https://helm.sh/)

---

## 📊 Overview

**WicStock** is an intelligent web platform for the textile manufacturing and retail industry, focused on **waste reduction**, **circular economy**, and **stock optimization**. It helps businesses minimize losses from unsold garments and overproduction through **predictive risk analytics and AI-assisted recommendations** — anticipating shortages, obsolescence, and overstock, and suggesting mitigation strategies (flash discounts, B2B redistribution, fabric recycling).

The platform is a **full-stack monorepo**:
- **ASP.NET Core Web API (.NET 8)** — backend
- **Blazor WebAssembly** — frontend
- **FastAPI Multi-Agent AI Service (Python 3.10+)** — NL2SQL, RAG, interactive analytics

---

## 🧩 Key Features

| Module | Highlights |
|---|---|
| **AI Assistant** | Natural-language to SQL, automatic chart generation, overstock/shortage risk scoring, AI-assisted action plans |
| **BI Dashboards** | Role-based (Admin, Manager, Client, Delivery), KPI visualization, stock health charts |
| **Catalog & Orders** | Dynamic catalog, made-to-order (*sur-commande*) pipeline |
| **Payments** | LemonSqueezy checkout, webhooks, variant-based pricing |
| **Logistics** | Delivery board, customer order tracker |
| **Realtime** | SignalR live notifications |

Full feature details → [`docs/features.md`](docs/features.md)

---

## 🤖 AI Architecture (at a glance)

A 4-agent decision layer, coordinated by a central orchestrator, backed by a SQL guard for RBAC/SELECT-only enforcement:

```
User Query → OrchestratorAgent → [NL2SQLAgent | SurstockAgent | PreferenceAgent] → SQLGuardAgent → SQL Server
```

Full agent diagram and responsibilities → [`docs/architecture.md`](docs/architecture.md)

---

## 🛠️ Technology Stack

| Layer | Technologies |
|---|---|
| **Backend** | ASP.NET Core Web API (.NET 8), EF Core, SQL Server / PostgreSQL |
| **Frontend** | Blazor WebAssembly, MudBlazor |
| **AI Microservice** | Python 3.10+, FastAPI, Ollama (Qwen3), ChromaDB (RAG) |
| **Realtime** | SignalR |
| **Security** | JWT, RBAC, Cloudflare Turnstile |
| **Infra** | Docker, GitHub Actions, Kubernetes (kind), Helm, Argo CD |

---

## 📁 Repository Structure

```
WicStockProject/
├── backend/              # ASP.NET Core Web API (.NET 8)
├── frontend/             # Blazor WebAssembly client
├── ai-service/           # FastAPI AI microservice (4 agents + ChromaDB + Ollama)
├── k8s/                  # Kubernetes manifests, kind configs, Helm chart
├── gitops/               # Argo CD application manifests
├── docs/                 # Detailed documentation (architecture, k8s, gitops, security...)
├── WicStock.sln
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python 3.10+](https://www.python.org/downloads/) & [Ollama](https://ollama.com/) (`qwen3:1.7b`, `nomic-embed-text`)
- SQL Server / LocalDB
- [ODBC Driver 17 for SQL Server](https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server)

### Run locally

```bash
git clone https://github.com/HaifaCheikh/WicStockProject.git
cd WicStockProject

# Backend
cd backend && cp appsettings.Example.json appsettings.json
dotnet restore && dotnet ef database update && dotnet run
# → https://localhost:7179

# AI service (new terminal)
cd ai-service
python -m venv venv && .\venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8001
# → http://localhost:8001/docs

# Frontend (new terminal)
cd frontend && dotnet restore && dotnet run
# → https://localhost:7121
```

### Run with Docker Compose

```bash
cp .env.example .env   # set your own Grafana credentials
docker-compose up --build
```

---

## 🏗️ Infrastructure & DevOps

WicStock ships with a full cloud-native toolchain, kept lightweight enough to run entirely on a laptop:

| Area | Summary | Details |
|---|---|---|
| **Containerization** | 4 multi-stage Docker images (API, frontend/Nginx, AI service, WhatsApp service) | [`docs/docker.md`](docs/docker.md) |
| **CI/CD** | GitHub Actions: build, Trivy scan, GHCR push, deploy | [`docs/ci-cd.md`](docs/ci-cd.md) |
| **Kubernetes** | 3 modular namespaces (`core`, `ai`, `observability`) on a local kind cluster, ~7% node RAM footprint end-to-end, Helm chart with dev/prod profiles | [`docs/kubernetes.md`](docs/kubernetes.md) |
| **GitOps** | Argo CD pull-based deployment + Bitnami Sealed Secrets | [`docs/gitops.md`](docs/gitops.md) |
| **Observability** | Prometheus + auto-provisioned Grafana dashboard, structured Serilog logging with correlation IDs | [`docs/observability.md`](docs/observability.md) |
| **Security (DevSecOps)** | Dependabot, CodeQL (SAST), Trivy (container scan), branch protection | [`docs/security.md`](docs/security.md) |
| **Cloud hosting** | Frontend on Vercel, API + PostgreSQL on Render.com | [`docs/hosting.md`](docs/hosting.md) |

**Quick start — local Kubernetes:**

```bash
kind create cluster --config k8s/kind-config.yaml
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
kubectl apply -f k8s/manifests/wicstock-core/          # daily baseline
kubectl apply -f k8s/manifests/wicstock-ai/             # on demand
kubectl apply -f k8s/manifests/wicstock-observability/  # on demand

./k8s/manage.sh status   # or .\k8s\manage.ps1 status
```

> 🔒 No secret is committed in clear text. Local secrets are set via `.env` / `kubectl create secret`; production secrets go through Sealed Secrets or an external secret manager — see [`docs/security.md`](docs/security.md).

---

## 📄 License

Academic / internship project, developed for educational and demonstration purposes. No commercial license is granted.
