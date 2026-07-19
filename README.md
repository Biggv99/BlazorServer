# 🤖 Blazor Server: Optimus Prime 

---
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

--- 
# 🌳 Folder Structure
<!-- INIZIO ALBERO CARTELLE -->
```text
OptmiusPrime/
├── Config
│   ├── grafana.yml
│   └── prometheus.yml
├── Core
│   ├── Extensions
│   │   ├── DatabaseExtension.cs
│   │   ├── EndpointExtension.cs
│   │   ├── FeatureExtension.cs
│   │   ├── HealthCheckExtension.cs
│   │   ├── PresentationExtension.cs
│   │   ├── RateLimiterExtension.cs
│   │   └── SecurityExtension.cs
│   ├── Network
│   │   ├── ApiRoutes.cs
│   │   ├── HttpContextExtensions.cs
│   │   └── HttpResult.cs
│   ├── Security
│   │   ├── Crypto
│   │   │   ├── EcdhProvider.cs
│   │   │   ├── EkeProvider.cs
│   │   │   └── ProviderService.cs
│   │   ├── HashProvider.cs
│   │   └── JwtProvider.cs
│   ├── Settings
│   │   ├── AppSettings.cs
│   │   ├── EkeSettings.cs
│   │   ├── EmailSettings.cs
│   │   ├── FirebaseSettings.cs
│   │   ├── JwtSettings.cs
│   │   └── SecuritySettings.cs
│   └── Utils
│       └── Result.cs
├── Docs
│   ├── Utility
│   │   ├── copilot-instructions.md
│   │   ├── Roadmap.md
│   │   └── Shell.md
│   └── Diffie-Hellman.md
├── Features
│   ├── Contracts
│   │   ├── Commands
│   │   │   ├── Shared
│   │   │   │   └── BaseTrackerCommand.cs
│   │   │   ├── AuditCommand.cs
│   │   │   ├── AuthCommand.cs
│   │   │   ├── InstructionCommand.cs
│   │   │   └── SystemCommand.cs
│   │   └── Models
│   │       ├── AppVersionModel.cs
│   │       ├── AuditModel.cs
│   │       ├── HealthModel.cs
│   │       ├── InstructionModel.cs
│   │       ├── JwtTokenModel.cs
│   │       ├── PokemonModel.cs
│   │       ├── RecoveryModel.cs
│   │       ├── RefreshTokenModel.cs
│   │       └── UserModel.cs
│   ├── Endpoints
│   │   ├── Shared
│   │   │   ├── HeaderExtractor.cs
│   │   │   └── ValidationFilter.cs
│   │   ├── AuthEndpoint.cs
│   │   ├── InstructionEndpoint.cs
│   │   ├── ProfileEndpoint.cs
│   │   └── SystemEndpoint.cs
│   ├── Handlers
│   │   ├── Shared
│   │   │   └── CheckHandler.cs
│   │   ├── AuditHandler.cs
│   │   ├── AuthHandler.cs
│   │   ├── InstructionHandler.cs
│   │   ├── ProfileHandler.cs
│   │   └── SystemHandler.cs
│   ├── Interfaces
│   │   ├── IAuditRepository.cs
│   │   ├── IAuthRepository.cs
│   │   └── IFirebaseRepository.cs
│   └── Validators
│       ├── AuthValidator.cs
│       └── InstructionValidator.cs
├── Infrastructure
│   ├── BackgroundJobs
│   │   ├── HealthBackground.cs
│   │   └── RefreshBackground.cs
│   ├── Database
│   │   ├── AppDbContext.cs
│   │   └── AppDbContextFactory.cs
│   ├── Health
│   │   ├── FirebaseHealthCheck.cs
│   │   ├── GrafanaHealthCheck.cs
│   │   ├── PostgresHealthCheck.cs
│   │   ├── PrometheusHealthCheck.cs
│   │   └── RedisHealthCheck.cs
│   ├── Notifications
│   │   ├── EmailNotification.cs
│   │   └── HealthNotification.cs
│   └── Repository
│       ├── DatabaseAuditRepository.cs
│       ├── DatabaseAuthRepository.cs
│       └── DatabaseFirebaseRepository.cs
├── Presentation
│   ├── Components
│   │   ├── Cards
│   │   │   ├── Auth
│   │   │   │   ├── Password
│   │   │   │   │   ├── ForgotPasswordCard.razor
│   │   │   │   │   └── ResetPasswordCard.razor
│   │   │   │   ├── EmailCard.razor
│   │   │   │   ├── LoginCard.razor
│   │   │   │   ├── LoginCard.razor.css
│   │   │   │   ├── RefreshCard.razor
│   │   │   │   ├── RefreshCard.razor.css
│   │   │   │   ├── RegisterCard.razor
│   │   │   │   └── RegisterCard.razor.css
│   │   │   ├── AppVersionCard.razor
│   │   │   ├── AuditCard.razor
│   │   │   ├── AuditCard.razor.css
│   │   │   ├── InstructionCard.razor
│   │   │   └── InstructionCard.razor.css
│   │   ├── Elements
│   │   │   ├── Pokemon
│   │   │   │   ├── PokemonCard.razor
│   │   │   │   ├── PokemonCard.razor.css
│   │   │   │   ├── PokemonCheers.razor
│   │   │   │   └── PokemonCheers.razor.css
│   │   │   ├── Appbar.razor
│   │   │   ├── Appbar.razor.css
│   │   │   ├── DashboardSwitch.razor
│   │   │   ├── DashboardSwitch.razor.css
│   │   │   ├── Drawer.razor
│   │   │   ├── Drawer.razor.css
│   │   │   ├── HealthBanner.razor
│   │   │   ├── HealthBanner.razor.css
│   │   │   ├── JwtBanner.razor
│   │   │   └── JwtBanner.razor.css
│   │   ├── Layouts
│   │   │   ├── MainLayout.razor
│   │   │   └── MainLayout.razor.css
│   │   └── Pages
│   │       ├── HomePage.razor
│   │       ├── HomePage.razor.css
│   │       ├── NotFound.razor
│   │       ├── RecoveryPage.razor
│   │       ├── RecoveryPage.razor.css
│   │       ├── ServicePage.razor
│   │       └── ServicePage.razor.css
│   ├── Services
│   │   ├── Shared
│   │   │   └── Interceptor.cs
│   │   ├── AuditService.cs
│   │   ├── AuthService.cs
│   │   ├── InstructionService.cs
│   │   ├── PokemonService.cs
│   │   ├── ProfileService.cs
│   │   └── SystemService.cs
│   ├── Utils
│   │   ├── States
│   │   │   ├── DashboardState.cs
│   │   │   ├── HealthState.cs
│   │   │   └── JwtTimerState.cs
│   │   └── Storage
│   │       ├── LocalStorage.cs
│   │       └── SessionStorage.cs
│   ├── _Imports.razor
│   ├── App.razor
│   └── Routes.razor
├── wwwroot
│   ├── Images
│   │   ├── Profile
│   │   │   ├── no-profile.webp
│   │   │   └── profile.webp
│   │   ├── Services
│   │   │   ├── firebase-icon.webp
│   │   │   ├── grafana-icon.webp
│   │   │   ├── postgres-icon.webp
│   │   │   ├── prometheus-icon.webp
│   │   │   └── redis-icon.webp
│   │   ├── favicon-pokeball.webp
│   │   └── grass.webp
│   └── app.css
├── .env
├── appsettings.json
├── docker-compose.yml
├── Dockerfile
├── firebase-adminsdk.json
└── Program.cs
```
<!-- FINE ALBERO CARTELLE -->