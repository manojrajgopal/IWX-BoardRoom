# IWX Boardroom — Architecture Roadmap

## Phase 1 — Foundational runtime ✅
End-to-end vertical slice proving CEO → AI agent → realtime feedback loop.

| Component | Stack | Status |
|---|---|---|
| API Gateway | .NET 10 + YARP | ✅ |
| CEO Agent Service | .NET 10 + MediatR + EF Core + MassTransit + SignalR | ✅ |
| Orchestrator AI | Python 3.12 + FastAPI + LangChain + Ollama + aio-pika | ✅ |
| Shared Contracts | .NET 10 class library (`IWX.Contracts`) | ✅ |
| Shared Common | .NET 10 (`IWX.Common`, Serilog, OpenTelemetry) | ✅ |
| Angular Dashboard | Angular 21 + PrimeNG + Tailwind + SignalR client | ✅ |
| Docker Compose | SQL Server, Mongo, Redis, RabbitMQ, Kafka (KRaft), Ollama | ✅ |

## Phase 2 — Department fan-out ✅ (current)
All 13 worker departments now run as independent .NET 10 microservices powered
by the shared `IWX.Departments.Worker` library. Each service:
- Has its own SQL Server database (`IwxHr`, `IwxSales`, …)
- Binds a private queue (`{dept}.task.approved`) to the `iwx.task.approved` fanout
- Filters events by `TargetDepartment` (only acts on its own work)
- Emits structured `AgentThinkingEvent`s + a final `TaskCompletedEvent`
- Exposes `/health`, `/department`, `/tasks`, `/tasks/{id}`, `/stats`
- Is routed through the API Gateway at `/api/{department}/…`

| Service | Port | Database |
|---|---|---|
| ceo-agent-service                 | 8081 | IwxCeo |
| hr-agent-service                  | 8082 | IwxHr |
| sales-agent-service               | 8083 | IwxSales |
| finance-agent-service             | 8084 | IwxFinance |
| marketing-agent-service           | 8085 | IwxMarketing |
| operations-agent-service          | 8086 | IwxOperations |
| development-agent-service         | 8087 | IwxDevelopment |
| research-agent-service            | 8088 | IwxResearch |
| legal-agent-service               | 8089 | IwxLegal |
| social-media-agent-service        | 8090 | IwxSocialMedia |
| analytics-agent-service           | 8091 | IwxAnalytics |
| customer-support-agent-service    | 8092 | IwxCustomerSupport |
| automation-agent-service          | 8093 | IwxAutomation |
| platform-intelligence-agent-service | 8094 | IwxPlatformIntelligence |

The `orchestrator-ai` now only processes `ceo`-targeted tasks (cross-departmental
LLM planning), avoiding double-processing.

## Phase 3 — AI substrate
- `memory-engine` (.NET) — short/long-term memory façade over Mongo + Redis
- `rag-engine` (Python) — ChromaDB + FAISS, document ingestion pipeline
- `llm-router` (Python) — provider abstraction (Ollama/OpenAI/Gemini/HF)
- `prompt-engine` (Python) — versioned prompt registry
- `reasoning-engine` (Python) — CrewAI / AutoGen multi-agent orchestration
- `vector-engine` (Python) — embedding service

## Phase 4 — Platform connectors
Independent worker services per platform (Instagram, YouTube, LinkedIn, etc.).
Each connector exposes a gRPC contract consumed by department services.

## Phase 5 — Automation engines
- `workflow-engine` — durable workflow DSL (Elsa or custom)
- `scheduler-engine` — Quartz.NET
- `task-engine` — task graph executor
- `approval-engine` — CEO approval gate

## Phase 6 — Security
- `java-security-engine` (Spring Boot) — prompt-injection detection, behavioral analysis
- `auth-service` (.NET 10) — JWT + OAuth2 + RBAC
- `audit-service` — append-only audit log to Mongo + Kafka

## Phase 7 — Frontend expansion
- `admin-panel` (Angular) — system admin
- `realtime-monitor` (Angular) — live workflow visualizer
- `analytics-dashboard` (Angular) — BI views

## Phase 8 — DevOps / Production
- Kubernetes manifests (`devops/kubernetes`)
- Helm charts per service
- GitHub Actions CI/CD pipelines
- NGINX / Traefik ingress with mTLS

## Design constants
- **Communication:** RabbitMQ fanout for events, gRPC for sync queries, SignalR for client realtime, Kafka for high-volume analytics streams.
- **Event contracts** live in `backend/shared/contracts/IWX.Contracts/Events`.
- **Department registry** in `backend/shared/contracts/IWX.Contracts/Departments`.
- **Plugin model:** each new department/connector is added by (1) creating its folder under the right root, (2) referencing `IWX.Contracts`, (3) binding a queue to the relevant fanout, (4) registering in API Gateway routes.
