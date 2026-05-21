# IWX Boardroom — Architecture Roadmap

Legend: ✅ shipped & verified · ⏳ in progress · ⬜ not started · 🚫 intentionally deferred (with note)

---

## Phase 1 — Foundational runtime  ✅ COMPLETE

End-to-end vertical slice proving CEO → AI agent → realtime feedback loop.

| Component | Stack | Status |
|---|---|---|
| API Gateway (`backend/api-gateway/IWX.ApiGateway`) | .NET 10 + YARP | ✅ |
| CEO Agent Service (`backend/services/ceo-agent-service`) | .NET 10 + MediatR + EF Core + MassTransit + SignalR | ✅ |
| Orchestrator AI (`backend/ai-services/orchestrator-ai`) | Python 3.12 + FastAPI + LangChain + Ollama + aio-pika | ✅ |
| Shared Contracts (`backend/shared/contracts/IWX.Contracts`) | .NET 10 class library | ✅ |
| Shared Common (`backend/shared/common/IWX.Common`) | .NET 10, Serilog, OpenTelemetry | ✅ |
| Angular Dashboard (`frontend/angular-dashboard`) | Angular 21 + PrimeNG (Aura) + Tailwind v4 + SignalR client + signals | ✅ |
| Docker Compose (`devops/docker/docker-compose.yml`) | SQL Server 2022, Mongo 7, Redis 7, RabbitMQ 3.13, Kafka 3.7 (KRaft), Ollama | ✅ |
| End-to-end demo flow (submit → approve → AI → SignalR → UI) | RabbitMQ fanout + SignalR hub | ✅ |
| .NET solution builds with 0 errors | `dotnet build IWX.Boardroom.slnx` | ✅ |
| Angular bundles cleanly | `ng build` | ✅ |

---

## Phase 2 — Department fan-out  ✅ COMPLETE (with two items deferred)

All 13 worker departments run as independent .NET 10 microservices powered by the shared `IWX.Departments.Worker` library.

| Capability | Status | Notes |
|---|---|---|
| Shared `IWX.Departments.Worker` library (`backend/shared/department-worker`) | ✅ | One-liner setup via `AddIwxDepartmentService(descriptor)` |
| Authoritative `DepartmentRegistry` (key, port, db, host, icon) | ✅ | `IWX.Contracts/Departments/DepartmentRegistry.cs` |
| 13 dedicated `*-agent-service` projects added to slnx | ✅ | hr, sales, finance, marketing, operations, development, research, legal, social-media, analytics, customer-support, automation, platform-intelligence |
| Each service has its own SQL Server database | ✅ | `IwxHr` … `IwxPlatformIntelligence` |
| Each service has a private RabbitMQ queue (`{dept}.task.approved`) bound to the `iwx.task.approved` fanout | ✅ | Configured by worker library |
| Consumer filters events by `TargetDepartment` | ✅ | `DepartmentTaskApprovedConsumer` |
| Default cognition emits structured `AgentThinkingEvent`s and a final `TaskCompletedEvent` | ✅ | `DefaultDepartmentBrain` (placeholder until Phase 3) |
| Standard REST endpoints exposed: `/health`, `/department`, `/tasks`, `/tasks/{id}`, `/stats` | ✅ | `DepartmentEndpoints` |
| API Gateway routes `/api/{dept}/**` → correct cluster for all 14 departments | ✅ | `api-gateway/appsettings.json` |
| Docker Compose entries for all 13 services with healthcheck-gated `depends_on` | ✅ | `devops/docker/docker-compose.yml` |
| Orchestrator-AI scoped to `ceo`-targeted tasks only (no duplicate processing) | ✅ | `ai-services/orchestrator-ai/app/bus.py` |
| Generator script (`scripts/generate-department-services.sh`) for repeatable scaffold | ✅ | Re-run to regenerate from one template |
| Full solution builds (16 .NET projects) with 0 errors | ✅ | Verified |
| Per-dept MongoDB memory collection | 🚫 | Deferred → centralised in Phase 3 `memory-engine` |
| Per-dept gRPC endpoint published in shared/contracts | 🚫 | Deferred → Phase 4 when platform-connectors need sync calls; nothing consumes it yet |

---

## Phase 3 — AI substrate  ✅ COMPLETE

Centralised cognition stack. Every service is independent — same separation rule as Phases 1–2.

| Service | Stack | Path | Port | Status |
|---|---|---|---|---|
| `memory-engine` | .NET 10 + MongoDB.Driver + StackExchange.Redis | `backend/ai-services/memory-engine/IWX.MemoryEngine.Api` | 8100 | ✅ |
| `llm-router` | Python 3.12 + FastAPI + httpx (Ollama/OpenAI abstraction) | `backend/ai-services/llm-router` | 8101 | ✅ |
| `prompt-engine` | Python 3.12 + FastAPI + Motor (MongoDB) + Jinja2 (versioned templates) | `backend/ai-services/prompt-engine` | 8102 | ✅ |
| `vector-engine` | Python 3.12 + FastAPI + sentence-transformers (CPU MiniLM) | `backend/ai-services/vector-engine` | 8103 | ✅ |
| `rag-engine` | Python 3.12 + FastAPI + ChromaDB (persistent) + httpx → vector-engine | `backend/ai-services/rag-engine` | 8104 | ✅ |
| `reasoning-engine` | Python 3.12 + FastAPI + lightweight multi-agent crew loop → llm-router/prompt-engine/memory-engine/rag-engine | `backend/ai-services/reasoning-engine` | 8105 | ✅ |
| API Gateway routes `/api/ai/{memory,llm,prompts,vectors,rag,reasoning}/**` | YARP | `backend/api-gateway/IWX.ApiGateway/appsettings.json` | — | ✅ |
| Docker Compose entries for all 6 (with chroma-data volume) | — | `devops/docker/docker-compose.yml` | — | ✅ |
| `SubstrateDepartmentBrain` — every department brain delegates to reasoning-engine + memory-engine with graceful fallback to `DefaultDepartmentBrain` | C# | `IWX.Departments.Worker/Brain/SubstrateDepartmentBrain.cs` | — | ✅ |
| Full solution builds (17 .NET projects) with 0 errors | — | — | — | ✅ |

**Note on CrewAI:** the production CrewAI library has heavy transitive deps (LangChain stack ≈ 800 MB). The `reasoning-engine` ships with a hand-written multi-agent crew loop (5 roles: planner → researcher → executor → critic → finalizer) that calls llm-router + memory-engine + rag-engine. Swapping in real CrewAI/AutoGen later is a drop-in replacement at `app/crew.py`.

---

## Phase 4 — Platform connectors  ✅ COMPLETE

Independent worker services per platform. Each connector exposes the **same** unified gRPC contract (`ConnectorService`) plus its own REST admin surface — strict separation preserved.

| Component | Stack | Path / Port | Status |
|---|---|---|---|
| Unified gRPC contract `ConnectorService` (Ping/Publish/Fetch/Search/Profile/Engage) | Protobuf 3 + Grpc.Tools | `IWX.Contracts/Protos/connector.proto` | ✅ |
| `ConnectorRegistry` (9 connectors with HTTP + gRPC ports + Mongo DB + host + icon) | C# | `IWX.Contracts/Connectors/ConnectorRegistry.cs` | ✅ |
| `ConnectorClientFactory` — any department/service builds gRPC clients by key | C# | `IWX.Contracts/Connectors/ConnectorClientFactory.cs` | ✅ |
| Shared `IWX.Connectors.Worker` library (REST + gRPC + Mongo credential store + MassTransit outbound) | .NET 10 | `backend/shared/connector-worker/IWX.Connectors.Worker` | ✅ |
| `StubConnectorService` default implementation (graceful "not_implemented" responses; real platforms swap in later) | C# | `IWX.Connectors.Worker/Grpc/StubConnectorService.cs` | ✅ |
| `instagram-connector` | .NET 10 (Stub) | `backend/platform-connectors/instagram-connector` (HTTP 8200, gRPC 9200) | ✅ |
| `youtube-connector` | .NET 10 (Stub) | `backend/platform-connectors/youtube-connector` (HTTP 8201, gRPC 9201) | ✅ |
| `linkedin-connector` | .NET 10 (Stub) | `backend/platform-connectors/linkedin-connector` (HTTP 8202, gRPC 9202) | ✅ |
| `twitter-connector` | .NET 10 (Stub) | `backend/platform-connectors/twitter-connector` (HTTP 8203, gRPC 9203) | ✅ |
| `facebook-connector` | .NET 10 (Stub) | `backend/platform-connectors/facebook-connector` (HTTP 8204, gRPC 9204) | ✅ |
| `reddit-connector` | .NET 10 (Stub) | `backend/platform-connectors/reddit-connector` (HTTP 8205, gRPC 9205) | ✅ |
| `whatsapp-connector` | .NET 10 (Stub) | `backend/platform-connectors/whatsapp-connector` (HTTP 8206, gRPC 9206) | ✅ |
| `email-connector` | .NET 10 (Stub) | `backend/platform-connectors/email-connector` (HTTP 8207, gRPC 9207) | ✅ |
| `websites-connector` | .NET 10 (Stub) | `backend/platform-connectors/websites-connector` (HTTP 8208, gRPC 9208) | ✅ |
| Generator script `scripts/generate-connector-services.sh` | bash | — | ✅ |
| API Gateway routes `/api/connectors/{key}/**` for all 9 | YARP | `api-gateway/appsettings.json` | ✅ |
| Docker Compose entries for all 9 (REST + gRPC ports + Mongo dep) | — | `devops/docker/docker-compose.yml` | ✅ |
| Department services auto-receive `ConnectorClientFactory` (any dept can call any connector by key) | C# DI | `IWX.Departments.Worker/DepartmentWorkerExtensions.cs` | ✅ |
| Phase 2 gap closed — 13 dept services + worker library now registered in slnx | — | `backend/IWX.Boardroom.slnx` | ✅ |
| Full solution builds (29 .NET projects) with 0 errors | — | — | ✅ |

**Note on stubs:** every connector ships with `StubConnectorService` returning structured `not_implemented` responses. Swapping in real Meta Graph / YouTube Data v3 / X v2 / LinkedIn / PRAW / Twilio WhatsApp / SMTP+IMAP / Playwright scraping clients is a per-connector drop-in (`app.UseIwxConnectorService<MyRealService>(…)`). The contract, plumbing, gateway routes, compose, and department wiring are already in place.

---

## Phase 5 — Automation engines  ✅ COMPLETE

Four independent durable runtimes the CEO and departments use to orchestrate work — strict separation preserved.

| Engine | Stack | Path | Port | DB | Status |
|---|---|---|---|---|---|
| `workflow-engine` — JSON DAG durable workflow runtime; auto-dispatches steps as deps complete; signal-driven progress | .NET 10 + MongoDB + MassTransit | `backend/automation-engines/workflow-engine/IWX.WorkflowEngine.Api` | 8300 | Mongo `iwx_workflows` | ✅ |
| `scheduler-engine` — Quartz.NET cron scheduler with persistent EF Core definitions; reloads jobs on startup | .NET 10 + Quartz 3.13 + SQL Server | `backend/automation-engines/scheduler-engine/IWX.SchedulerEngine.Api` | 8301 | SQL `IwxScheduler` | ✅ |
| `task-engine` — task graph DAG executor; tracks node status and emits `iwx.task.node.ready` events | .NET 10 + MongoDB + MassTransit | `backend/automation-engines/task-engine/IWX.TaskEngine.Api` | 8302 | Mongo `iwx_tasks` | ✅ |
| `approval-engine` — CEO approval gate (queue + decide endpoints); publishes `iwx.approval.requested` / `iwx.approval.decided` | .NET 10 + EF Core + SQL Server | `backend/automation-engines/approval-engine/IWX.ApprovalEngine.Api` | 8303 | SQL `IwxApprovals` | ✅ |
| `EngineRegistry` + `AutomationEvents` (5 event types: workflow step / scheduler tick / task node / approval requested / approval decided) | C# | `IWX.Contracts/Automation/AutomationEvents.cs` | — | — | ✅ |
| API Gateway routes `/api/engines/{workflow,scheduler,task,approval}/**` | YARP | `backend/api-gateway/IWX.ApiGateway/appsettings.json` | — | — | ✅ |
| Docker Compose entries for all 4 (with proper SQL/Mongo + RabbitMQ depends_on healthchecks) | — | `devops/docker/docker-compose.yml` | — | — | ✅ |
| Full solution builds (33 .NET projects) with 0 errors | — | — | — | — | ✅ |

---

## Phase 6 — Security  ✅ COMPLETE

Three independent zero-trust security services. All other services treat them as the single source of truth for identity, threat detection, and tamper-evident audit.

| Component | Stack | Path | Port | Storage | Status |
|---|---|---|---|---|---|
| `java-security-engine` — prompt-injection heuristics (12 patterns) + behavioral rate analyzer; publishes `iwx.security.threat.detected` | Spring Boot 3 + Java 21 + Spring AMQP | `backend/security/java-security-engine` | 8400 | stateless | ✅ |
| `auth-service` — JWT (HS256) issuance, RBAC (`ceo`/`admin`/`director`/`agent`/`user`), PBKDF2 password hashing, bootstrap CEO seed; publishes `iwx.security.auth.issued` / `iwx.security.access.denied` | .NET 10 + EF Core + JwtBearer | `backend/security/auth-service/IWX.AuthService.Api` | 8401 | SQL `IwxAuth` | ✅ |
| `audit-service` — append-only **hash-chained** audit log with chain verification; consumes Threat/Auth/Denied/Audit events from Rabbit, persists to Mongo, mirrors to Kafka topic `iwx.audit` | .NET 10 + MongoDB + Confluent.Kafka + MassTransit | `backend/security/audit-service/IWX.AuditService.Api` | 8402 | Mongo `iwx_audit` + Kafka | ✅ |
| `SecurityRegistry` + `SecurityEvents` (4 event types: `ThreatDetectedEvent`, `AuthIssuedEvent`, `AccessDeniedEvent`, `AuditRecordedEvent`) | C# | `IWX.Contracts/Security/SecurityEvents.cs` | — | — | ✅ |
| API Gateway routes `/api/security/{auth,audit,scan}/**` | YARP | `backend/api-gateway/IWX.ApiGateway/appsettings.json` | — | — | ✅ |
| Docker Compose entries for all 3 (with proper SQL/Mongo/Kafka/RabbitMQ healthcheck-gated `depends_on`) | — | `devops/docker/docker-compose.yml` | — | — | ✅ |
| Full solution builds (35 .NET projects + 1 Spring Boot) with 0 errors | — | — | — | — | ✅ |

---

## Phase 7 — Frontend expansion  ⬜
- `admin-panel` (Angular) — system admin
- `realtime-monitor` (Angular) — live workflow visualizer
- `analytics-dashboard` (Angular) — BI views

## Phase 8 — DevOps / Production  ⬜
- Kubernetes manifests (`devops/kubernetes`)
- Helm charts per service
- GitHub Actions CI/CD pipelines
- NGINX / Traefik ingress with mTLS

---

## Design constants
- **Communication:** RabbitMQ fanout for events, gRPC for sync queries (Phase 4+), SignalR for client realtime, Kafka for high-volume analytics streams.
- **Event contracts** live in `backend/shared/contracts/IWX.Contracts/Events`.
- **Department registry** in `backend/shared/contracts/IWX.Contracts/Departments`.
- **Plugin model:** each new department/connector is added by (1) creating its folder under the right root, (2) referencing `IWX.Contracts`, (3) binding a queue to the relevant fanout, (4) registering in API Gateway routes, (5) adding compose entry.
