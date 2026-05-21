# IWX Boardroom — Implementation Roadmap (Phase 2 cycle)

> The first cycle (Phases 1–8 in `ROADMAP.md`) delivered the runtime, all 13 departments, the AI substrate, connectors, automation, security, and devops scaffolding.
>
> **This roadmap covers everything still missing**: placeholder folders that contain only a `README.md`, security vulnerabilities flagged by NuGet audit, env-var externalization, observability, tests, and the production polish layer.
>
> **How to use this file:** prompt me with **"Continue Phase A"**, **"Continue Phase B"**, … one at a time, exactly like the previous roadmap. Each phase is self-contained, validated with a build/test step, and ends with this file flipping `⬜ → ✅ COMPLETE` for that row.

Legend: ✅ shipped & verified · ⏳ in progress · ⬜ not started

---

## Phase A — Repository hygiene & .env wiring  ⬜
- [ ] `.env.example` + `.env` at repo root (✅ shipped this turn — refer here)
- [ ] Refactor `devops/docker/docker-compose.yml` to use `${VAR}` substitution everywhere instead of hard-coded `IwxStrong!Passw0rd`, `iwx`, etc.
- [ ] Patch every `appsettings.json` to read from environment variables only (no committed secrets)
- [ ] Patch `start.sh` / `start.ps1` to validate required env vars before bringing up the stack
- [ ] Bump vulnerable transitive deps:
  - `OpenTelemetry.Api` 1.12.0 → ≥ 1.13.x  (NU1902 — moderate)
  - `SharpCompress` 0.30.1 → latest  (NU1902 — moderate)
  - `Snappier` 1.0.0 → latest  (NU1903 — high)
- [ ] Replace obsolete `Rfc2898DeriveBytes` ctor in `auth-service/Infrastructure/Auth.cs` (SYSLIB0060)
- [ ] Verify: `dotnet build -warnaserror` succeeds, `docker compose config` is OK

---

## Phase B — Shared infrastructure libraries  ⬜
Folders today: only README.md.

- [ ] `backend/infrastructure/logging/IWX.Infrastructure.Logging` — Serilog config + structured logging extensions, request enrichment, correlation IDs
- [ ] `backend/infrastructure/telemetry/IWX.Infrastructure.Telemetry` — OpenTelemetry tracing/metrics setup with OTLP exporter
- [ ] `backend/infrastructure/monitoring/IWX.Infrastructure.Monitoring` — `/health`, `/health/ready`, `/health/live` endpoints + Prometheus `/metrics` scrape
- [ ] `backend/infrastructure/caching/IWX.Infrastructure.Caching` — Redis-backed `IIwxCache` with namespaced keys + TTL helpers
- [ ] Wire all 4 libraries into `IWX.Common` so every service gets them with one extension method
- [ ] Add to slnx + verify build

---

## Phase C — Communication adapters  ⬜
Folders today: only README.md.

- [ ] `backend/communication/rabbitmq/IWX.Communication.RabbitMq` — typed publisher/consumer extensions wrapping MassTransit, retry policy, DLQ helpers
- [ ] `backend/communication/kafka/IWX.Communication.Kafka` — Confluent.Kafka wrapper with typed producer/consumer + JSON envelope + schema versioning
- [ ] `backend/communication/signalr/IWX.Communication.SignalR` — shared hub contracts + groups by tenant/department + auth filter
- [ ] Migrate ad-hoc `RabbitConfig`/`ThreatPublisher`/etc. to use these libraries
- [ ] Add to slnx + verify build

---

## Phase D — Shared events & models  ⬜
Folders today: only README.md.

- [ ] `backend/shared/events` — split `IWX.Contracts/Events` into a versioned event-only library `IWX.Shared.Events` (V1 namespace, schema docs)
- [ ] `backend/shared/shared-models` — `IWX.Shared.Models` with cross-cutting DTOs (Pagination, ApiError, Money, Tenant, AuditMeta)
- [ ] `backend/shared/utilities` — `IWX.Shared.Utilities` helpers (clock, ids, json, hashing, retry policies)
- [ ] Reference from departments + connectors + engines, drop duplicates
- [ ] Add to slnx + verify build

---

## Phase E — Instagram sub-analyzers  ⬜
Folders today: 5 placeholders under `backend/platform-connectors/instagram/`.

| Sub-analyzer | Capability | Output |
|---|---|---|
| `audience-analyzer` | Demographic + engagement breakdown of followers via Graph API | `AudienceReport` event |
| `competitor-analyzer` | Track 5–20 rival accounts: post cadence, hashtags, growth | `CompetitorReport` event |
| `hashtag-analyzer` | Score hashtags by volume, recency, relevance | `HashtagReport` event |
| `reels-analyzer` | Pull reel metadata, watch-through, hooks; cluster top performers | `ReelsReport` event |
| `trend-detector` | Detect rising sounds/hashtags/formats per niche | `TrendReport` event |

Each is its own .NET 10 worker referencing the existing `instagram-connector` for raw Graph access; communicates via RabbitMQ + writes results to Mongo.

---

## Phase F — YouTube sub-analyzers  ⬜
Folders today: 4 placeholders under `backend/platform-connectors/youtube/`.

| Sub-analyzer | Capability |
|---|---|
| `comments-analyzer` | Sentiment + topic clustering of comments per video |
| `seo-ai` | Title/description/tag suggestions using `prompt-engine` + `rag-engine` |
| `shorts-analyzer` | Shorts watch-time + hook detection; recommend posting cadence |
| `thumbnail-ai` | CTR prediction + thumbnail variant suggestions (uses `vector-engine` for similar-thumbnail retrieval) |

---

## Phase G — LinkedIn sub-analyzers  ⬜
Folders today: 2 placeholders under `backend/platform-connectors/linkedin/`.

| Sub-analyzer | Capability |
|---|---|
| `business-analysis` | Company-page benchmarking, industry trend feed, share-of-voice |
| `lead-generation` | ICP scoring + outreach sequencing; pushes to `sales-agent-service` |

---

## Phase H — Remaining connector stubs  ⬜
Folders today (all duplicates of the *real* `*-connector` projects, currently empty):

- [ ] `platform-connectors/email`        → consolidate into `email-connector` (drop the empty folder)
- [ ] `platform-connectors/facebook`     → consolidate into `facebook-connector`
- [ ] `platform-connectors/reddit`       → consolidate into `reddit-connector`
- [ ] `platform-connectors/twitter`      → consolidate into `twitter-connector`
- [ ] `platform-connectors/websites`     → consolidate into `websites-connector`
- [ ] `platform-connectors/whatsapp`     → consolidate into `whatsapp-connector`

Decision per folder: either delete (preferred — they are duplicates) or repurpose as a `*/analyzers/` subtree mirroring Phases E/F/G. Phase H closes whichever path is chosen.

---

## Phase I — Python AI workers  ⬜
Folders today: only README.md under `python-ai/`.

| Folder | Purpose | Stack |
|---|---|---|
| `langchain-agents` | LangChain ReAct + tool-using agents; one entrypoint per dept | Python 3.12 + LangChain + FastAPI |
| `crew-agents` | CrewAI multi-agent collaboration flows (planner/researcher/writer/critic) | CrewAI |
| `autonomous-workers` | Long-running goal-seeking loops (BabyAGI-style) writing to `memory-engine` | Python 3.12 |
| `embeddings` | Embedding workers (text/image) feeding `vector-engine` | sentence-transformers + Pillow |
| `rag-system` | Document ingestion pipeline (chunk → embed → upsert → retrieve) tied to `rag-engine` | unstructured + langchain |
| `ai-memory` | Episodic + semantic memory writers/consolidators on top of `memory-engine` | Python 3.12 |

Each becomes a standalone container in `docker-compose.yml`, talks to existing engines via REST/RMQ.

---

## Phase J — Observability stack  ⬜
- [ ] Add to compose: `otel-collector`, `prometheus`, `grafana`, `loki`, `tempo`
- [ ] Pre-built Grafana dashboards in `devops/observability/grafana/dashboards/`:
  - Service health (CPU/mem/req-rate per service)
  - Event-bus throughput (RabbitMQ + Kafka)
  - LLM cost & latency (per-route on `llm-router`)
  - Audit chain integrity over time
- [ ] Loki log-shipping for every service via promtail or Docker driver
- [ ] Wire `IWX.Infrastructure.Telemetry` (Phase B) to push to OTLP

---

## Phase K — Tests & quality gates  ⬜
- [ ] xUnit projects per backend layer:
  - `tests/IWX.Contracts.Tests`
  - `tests/IWX.CeoAgent.Api.Tests` (integration: in-mem RMQ + SQLite)
  - `tests/IWX.AuditService.Api.Tests` (hash-chain + Kafka)
  - `tests/IWX.AuthService.Api.Tests` (PBKDF2 + JWT round-trip)
- [ ] Pytest suite for `orchestrator-ai` + Python AI workers (mock Ollama)
- [ ] Karma/Vitest unit tests for each Angular app + Playwright e2e for the demo flow
- [ ] Postman/Bruno collection committed under `tests/postman/` covering gateway → CEO → audit
- [ ] Update `.github/workflows/ci-*` to fail PRs on test failure
- [ ] Coverage threshold: 70% on changed lines

---

## Phase L — Security hardening  ⬜
- [ ] Snyk + GitHub CodeQL workflows on PR
- [ ] Replace plaintext compose secrets with Docker secrets / SOPS-encrypted file
- [ ] Switch all internal HTTP between services to HTTPS with the IWX private CA (mTLS east-west, not just north-south)
- [ ] Rotate `JWT_SECRET` via env-driven KMS reference
- [ ] Rate-limiting + IP-allowlist middleware on api-gateway
- [ ] Centralize all RBAC checks in `auth-service` and require gateway forwards `X-Iwx-User-*` headers from validated JWT
- [ ] Penetration test plan + threat-model doc (`docs/architecture/threat-model.md`)

---

## Phase M — Docs, diagrams, runbooks  ⬜
Folders today: only README.md under `docs/api-docs`, `docs/diagrams`, `docs/workflows`.

- [ ] `docs/api-docs` — auto-generated OpenAPI bundle for every .NET service + a redoc HTML index
- [ ] `docs/diagrams` — committed PlantUML/Mermaid sources for: system context, container, deployment, sequence diagrams (CEO→Dept→AI, audit chain, mTLS handshake)
- [ ] `docs/workflows` — runbooks: bootstrap, incident response, rotate-jwt, rotate-mTLS-CA, scale department, add new connector
- [ ] User-facing manual: how to onboard a new tenant, configure CEO bootstrap, plug a new connector

---

## Phase N — Production launch checklist  ⬜
- [ ] Helm umbrella chart smoke-deploy to a real K8s cluster (kind / k3d local first, then a managed cluster)
- [ ] cert-manager + Traefik mTLS verified end-to-end with a test client cert
- [ ] Backups: automated nightly dump of SQL Server + Mongo + RabbitMQ definitions to S3-compatible storage
- [ ] Disaster-recovery drill (restore the dump into a fresh cluster, run smoke suite)
- [ ] SLO/SLI definitions per service in Grafana
- [ ] Cutover runbook + rollback plan + on-call rota

---

## Bug & gap inventory (snapshot taken 2026-05-21)

| Area | Issue | Phase |
|---|---|---|
| `start.sh`/`start.ps1` are missing today | `start.ps1` + `start.sh` shipped this turn | A |
| `.env` was missing | `.env.example` + `.env` shipped this turn | A |
| Compose hard-codes `IwxStrong!Passw0rd`, `iwx`, JWT secret | Refactor to `${VAR}` from `.env` | A |
| `OpenTelemetry.Api 1.12.0` flagged NU1902 (moderate) | Upgrade | A |
| `SharpCompress 0.30.1` flagged NU1902 (moderate) | Upgrade | A |
| `Snappier 1.0.0` flagged NU1903 (HIGH) | Upgrade | A |
| `Rfc2898DeriveBytes` ctor obsolete (SYSLIB0060) in auth-service | Switch to `Rfc2898DeriveBytes.Pbkdf2` static | A |
| `backend/infrastructure/{caching,logging,monitoring,telemetry}` empty | Implement libs | B |
| `backend/communication/{kafka,rabbitmq,signalr}` empty | Implement adapters | C |
| `backend/shared/{events,shared-models,utilities}` empty | Implement libs | D |
| `backend/platform-connectors/instagram/{5 sub-analyzers}` empty | Implement | E |
| `backend/platform-connectors/youtube/{4 sub-analyzers}` empty | Implement | F |
| `backend/platform-connectors/linkedin/{business-analysis,lead-generation}` empty | Implement | G |
| `backend/platform-connectors/{email,facebook,reddit,twitter,websites,whatsapp}` empty (duplicates of `*-connector`) | Consolidate or repurpose | H |
| `python-ai/{ai-memory,autonomous-workers,crew-agents,embeddings,langchain-agents,rag-system}` empty | Implement workers | I |
| No observability stack | Prometheus/Grafana/Loki/Tempo/OTel | J |
| No automated test projects | xUnit / Pytest / Karma / Playwright | K |
| Plaintext secrets in compose; no mTLS east-west | Hardening | L |
| `docs/{api-docs,diagrams,workflows}` empty | Author docs | M |
| No production checklist | Helm smoke + DR drill | N |
