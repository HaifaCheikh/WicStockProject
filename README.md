# 🧵 WicStock – Smart Textile Inventory Management & Optimization Platform

> 🎓 Academic/internship project — built to demonstrate full-stack architecture, AI-assisted analytics, and real-time systems in a realistic business scenario.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat&logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Entity Framework](https://img.shields.io/badge/EF%20Core-8.0-6C287E?style=flat)](https://learn.microsoft.com/ef/core/)
[![SignalR](https://img.shields.io/badge/SignalR-Realtime-blue?style=flat)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![LemonSqueezy](https://img.shields.io/badge/Payments-LemonSqueezy-yellow?style=flat)](https://www.lemonsqueezy.com/)
[![Cloudflare](https://img.shields.io/badge/Security-Turnstile-F38020?style=flat&logo=cloudflare)](https://www.cloudflare.com/)

---

## 📌 Overview

**WicStock** is an intelligent web-based platform tailored for the textile manufacturing and retail industry. Built with a primary focus on **waste reduction**, **circular economy**, and **stock optimization**, it helps textile businesses minimize financial loss caused by unsold garments and overproduction.

By leveraging **predictive risk analytics and AI recommendations**, WicStock anticipates stock shortages, obsolescence, and overstock scenarios, automatically suggesting mitigation strategies (e.g., flash discounts, B2B redistribution, or fabric recycling).

The application is architected as a **full-stack monorepo** featuring an **ASP.NET Core Web API (.NET 8)** backend and a **Blazor WebAssembly** frontend.

---

## 🚀 Key Features & Modules

### 🧠 1. Smart Stock Analytics & AI Recommendations
* **Surstock & Shortage Risk Scoring**: Automatic detection of slow-moving inventory and critical threshold alerts.
* **Intelligent Action Plans**: AI-assisted recommendations to apply promotional markdowns, trigger recycling workflows, or reallocate excess fabric.
* **Interactive Metric Cards**: Real-time stock turnover, holding costs, and lifecycle indicators.

### 📊 2. Role-Based BI Dashboards
* **Admin & Manager Dashboards**: High-level KPI visualization, interactive SVG/HTML charts (Category Breakdown, Monthly Sales, Stock Health).
* **Role-Based Access Control (RBAC)**: Custom views and permission sets for `Admin`, `Manager`, `Client`, and `Livreur` (Delivery).

### 🛍️ 3. Catalog & Order Lifecycle (Standard & Made-to-Order)
* **Dynamic Product Catalog**: Filtering by category, fabric type, promotion status, and custom stock availability.
* **Made-to-Order Pipeline (*Sur-Commande*)**: End-to-end workflow for pre-orders and personalized client specifications.

### 💳 4. Payment Gateway Integration
* **LemonSqueezy Integration**: Secure checkout sessions, variant-based pricing, and automatic order confirmation webhooks.

### 🚚 5. Logistics & Delivery Tracking
* **Delivery Board**: Dedicated tracking interface for delivery agents and managers.
* **Customer Order Tracker**: Step-by-step progress stepper (*Confirmed → In Preparation → In Transit → Delivered*).

### 🔔 6. Real-Time SignalR Notifications
* **Live Notifications**: Instant push notifications for critical alerts, delivery status changes, and new reviews without page refresh.
* **Interactive Notification Bell**: Unread counters and quick mark-as-read functionality.

### ⭐ 7. Feedback & Dispute Management
* **Customer Reviews & Ratings**: Star-based evaluation system with comments.
* **Claims / Reclamations**: Integrated ticket management allowing administrators to review and resolve customer issues.

---

## 🛠️ Technology Stack

| Layer | Technologies & Tools |
|---|---|
| **Backend** | ASP.NET Core Web API (.NET 8), Entity Framework Core, SQL Server (LocalDB / Azure SQL) |
| **Frontend** | Blazor WebAssembly (.NET 8), MudBlazor, Custom Modern CSS & Glassmorphism UI |
| **Realtime** | ASP.NET Core SignalR WebSockets |
| **Security** | JWT (JSON Web Tokens), Role Authorization, Cloudflare Turnstile Bot Protection |
| **Integrations** | LemonSqueezy Payments API, WhatsApp Service API |

---

## 📁 Repository Structure

```
WicStockProject/
├── backend/              # ASP.NET Core Web API (.NET 8)
├── frontend/             # Blazor WebAssembly client application
├── WicStock.sln          # Unified Visual Studio Solution
├── .gitignore            # Git exclusion rules
└── README.md             # Project documentation
```

---

## ⚙️ Getting Started

### 1. Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [SQL Server](https://www.microsoft.com/sql-server/) (or SQL Server LocalDB with Visual Studio 2022)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) / [VS Code](https://code.visualstudio.com/)

---

### 2. Clone the Repository

```bash
git clone https://github.com/HaifaCheikh/WicStockProject.git
cd WicStockProject
```

---

### 3. Backend Setup

```bash
cd backend

# Configure environment settings
cp appsettings.Example.json appsettings.json
# Edit appsettings.json to add your Database Connection String, JWT Key, and LemonSqueezy credentials

# Restore packages and apply database migrations
dotnet restore
dotnet ef database update

# Run the API
dotnet run
```
> The API will start on `https://localhost:7179` (or `http://localhost:5042`).
>
> Interactive API documentation (Swagger UI) is available at `https://localhost:7179/swagger`.

---

### 4. Frontend Setup

In a new terminal window:

```bash
cd frontend

# Restore packages and run Blazor client
dotnet restore
dotnet run
```
> The web application will be accessible at `https://localhost:7121` (or `http://localhost:5043`).

---

## 🔐 Environment Configuration

For security reasons, actual credentials and API keys are not committed to Git. Refer to `backend/appsettings.Example.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=WicStockDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_JWT_SECRET_KEY_MIN_32_CHARS",
    "Issuer": "WicStock",
    "Audience": "WicStockUsers",
    "ExpireMinutes": "120"
  },
  "Turnstile": {
    "SecretKey": "YOUR_CLOUDFLARE_TURNSTILE_SECRET"
  },
  "LemonSqueezy": {
    "VariantId": "YOUR_VARIANT_ID",
    "ApiKey": "YOUR_LEMON_SQUEEZY_API_KEY",
    "StoreId": "YOUR_STORE_ID"
  }
}
```

---

## 🗺️ Roadmap

- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Automated tests
- [ ] Screenshots / demo

---

## 📜 License

This is an academic/internship project developed for educational and demonstration purposes. No commercial license is granted.
