# IWX Boardroom

> An AI-powered autonomous company. CEO approves. AI departments execute.

A modular enterprise platform where every department is an autonomous AI microservice working under a CEO agent. Built for clean architecture, microservices, event-driven coordination, and full plugin-style extensibility.

## Tech stack (Phase 1 runtime)

- **API Gateway** — .NET 10 + YARP
- **CEO Agent Service** — .NET 10, MediatR + CQRS, EF Core (SQL Server), MassTransit (RabbitMQ), SignalR
- **Orchestrator AI** — Python 3.12, FastAPI, LangChain, Ollama, aio-pika
- **Dashboard** — Angular 21, PrimeNG, TailwindCSS, SignalR client, NgRx Signals
- **Infra** — SQL Server, MongoDB, Redis, RabbitMQ, Kafka (KRaft), Ollama

See [docs/architecture/ROADMAP.md](docs/architecture/ROADMAP.md) for the phased build plan.

## Repository layout

```
backend/
  api-gateway/                  YARP gateway
  services/                     One folder per AI department (13)
  ai-services/                  Orchestrator + AI substrate (RAG, memory, routing)
  platform-connectors/          Per-platform analyzers (Instagram, YouTube, …)
  automation-engines/           Workflow, scheduler, approval engines
  communication/                RabbitMQ / Kafka / SignalR adapters
  security/                     Java security engine, auth, audit
  infrastructure/               Logging, monitoring, telemetry, caching
  shared/                       Contracts, common, events, models
  IWX.Boardroom.slnx
frontend/
  angular-dashboard/            CEO console (Phase 1)
  admin-panel/  realtime-monitor/  analytics-dashboard/
python-ai/                      CrewAI/LangChain agents, RAG, embeddings
devops/
  docker/docker-compose.yml     Full infra stack
  kubernetes/  nginx/  github-actions/
docs/
```

## Quick start (Phase 1 — full stack via Docker)

Prereqs: Docker Desktop, Node 20+, .NET 10 SDK, Python 3.12.

```bash
# 1. Bring up infrastructure + backend + AI services
cd devops/docker
docker compose up -d --build

# 2. Pull a local LLM (one-time)
docker exec -it iwx-ollama ollama pull llama3.2:3b

# 3. Start the Angular dashboard
cd ../../frontend/angular-dashboard
npm install
npm start
# → http://localhost:4200
```

Service URLs:

| Service | URL |
|---|---|
| Angular dashboard | http://localhost:4200 |
| API Gateway | http://localhost:8080 |
| CEO Agent API | http://localhost:8081/swagger |
| Orchestrator AI | http://localhost:8000/docs |
| RabbitMQ UI | http://localhost:15672 (iwx / iwx) |
| Ollama | http://localhost:11434 |

## End-to-end demo flow

1. Open the dashboard at `http://localhost:4200`.
2. Submit a new directive (e.g. *“Generate Instagram trend report for fintech niche”* → `marketing`).
3. Click **Approve** on the new row.
4. CEO Service publishes `iwx.task.approved` → Orchestrator consumes → calls Ollama → streams `iwx.agent.thinking` events → publishes `iwx.task.completed`.
5. CEO Service updates the task in SQL and broadcasts to SignalR.
6. Dashboard updates the **AI Thinking** stream and the task row's status/summary in real time.

## Local development (without Docker)

```bash
# Start only infra
cd devops/docker
docker compose up -d sqlserver mongo redis rabbitmq kafka ollama
docker exec -it iwx-ollama ollama pull llama3.2:3b

# Backend
cd ../../backend
dotnet build IWX.Boardroom.slnx
dotnet run --project services/ceo-agent-service/IWX.CeoAgent.Api    # :8081
dotnet run --project api-gateway/IWX.ApiGateway                     # :8080

# Orchestrator
cd ai-services/orchestrator-ai
python -m venv .venv && source .venv/Scripts/activate
pip install -r requirements.txt
IWX_RABBITMQ_HOST=localhost IWX_OLLAMA_HOST=http://localhost:11434 \
  uvicorn app.main:app --reload --port 8000

# Frontend
cd ../../../frontend/angular-dashboard
npm install && npm start
```

## Event contracts

All inter-service contracts live in `backend/shared/contracts/IWX.Contracts/`:

- `TaskApprovedEvent` — emitted by CEO Service on approval; consumed by Orchestrator + every department service.
- `AgentThinkingEvent` — emitted by any AI agent; consumed by CEO Service → SignalR.
- `TaskCompletedEvent` — emitted by department/orchestrator; CEO Service updates DB + notifies UI.

## Extending the system

Adding a new department is a four-step plug-in:

1. `dotnet new webapi -n IWX.<Dept>.Api -o backend/services/<dept>-agent-service/IWX.<Dept>.Api`
2. Reference `IWX.Contracts` + `IWX.Common`.
3. Add a MassTransit consumer bound to `Queues.TaskApproved`, filtering by `TargetDepartment`.
4. Register a route in `api-gateway/IWX.ApiGateway/appsettings.json`.

No core changes required — same pattern for platform connectors and AI engines.

## Security posture

- JWT + OAuth2 (`security/auth-service` — Phase 6)
- RBAC enforced at API Gateway
- AI prompt-injection detection (`security/java-security-engine` — Phase 6)
- Append-only audit log (`security/audit-service` — Phase 6)
- All inter-service traffic on private Docker network in dev; mTLS via Traefik/NGINX in prod

## License

Proprietary © InfiniteWaveX.
