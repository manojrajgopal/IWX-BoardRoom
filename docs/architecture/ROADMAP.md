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

## Phase 4 — Platform connectors  ⬜
Independent worker services per platform (Instagram, YouTube, LinkedIn, etc.). Each connector exposes a gRPC contract consumed by department services.

## Phase 5 — Automation engines  ⬜
- `workflow-engine` — durable workflow DSL (Elsa or custom)
- `scheduler-engine` — Quartz.NET
- `task-engine` — task graph executor
- `approval-engine` — CEO approval gate

## Phase 6 — Security  ⬜
- `java-security-engine` (Spring Boot) — prompt-injection detection, behavioral analysis
- `auth-service` (.NET 10) — JWT + OAuth2 + RBAC
- `audit-service` — append-only audit log to Mongo + Kafka

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
