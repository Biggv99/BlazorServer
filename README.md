# Blazor Server

### 📌 Overview
**Optimus Prime** is an **ASP.NET Core Blazor Server** template built around **Minimal APIs**, designed as a secure and observable foundation for internal admin tooling. 

It ships with a built-in **Admin Dashboard**, JWT access/refresh authentication, advanced cryptographic device binding, audit logging, rate limiting, and background services for health monitoring and refresh‑token cleanup.

At its core, Optimus Prime provides a dashboard to monitor and manage internal resources, backed by a clean pipeline and a clear separation between endpoints, handlers, and repositories.

### ✨ Key Features

### Security & Authentication
* **JWT Authentication:** Access + refresh tokens with EF Core persistence.
* **Cryptographic Device Binding:** Implements **Diffie-Hellman key exchange** to establish secure, device-specific private keys, ensuring strict user-to-device binding and preventing credential replay attacks.
* **Secure Identity Recovery:** Features a built-in QR Code backup and restore mechanism, allowing users to seamlessly export and recover their cryptographic device credentials across sessions.
* **Role-based access:** Roles (Admin/Editor/Viewer) with policy enforcement where required.
* **Rate Limiting:** Built-in throttling for public endpoints.
* **Audit Logging:** Full traceability of user actions and system requests.

### Monitoring & Observability
* **Service Health Checks:** `/api/health` with periodic background checks.
* **Real-Time Mobile Alerts:** Firebase FCM notifications on health status changes.
* **Metrics & Telemetry:** `/metrics` via **Prometheus**, visualized in **Grafana**.

### 🐳 Infrastructure & Containerization
The entire environment is orchestrated using Docker, ensuring a seamless, reproducible, and hassle-free setup process. 

**Integrated External Services:**
* **PostgreSQL** - Primary relational database (users, refresh tokens, audit logs)
* **Redis** - Caching and health checks
* **Prometheus** - Metrics collection
* **Grafana** - Monitoring dashboards
* **Firebase Realtime DB** - Instruction dispatch
* **Firebase FCM** - Mobile health alerts
