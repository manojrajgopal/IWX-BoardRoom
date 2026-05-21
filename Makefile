# =============================================================================
# IWX Boardroom — Makefile
# Cross-platform convenience targets. Wraps start.sh / docker compose.
# =============================================================================
SHELL := /bin/bash
ROOT  := $(shell pwd)
COMPOSE := docker compose --env-file $(ROOT)/.env -f $(ROOT)/devops/docker/docker-compose.yml

.PHONY: help up down stop restart logs ps build infra pull-llm \
        be-build fe-build clean test reset

help:
	@echo "IWX Boardroom — available targets:"
	@echo "  make up         Build + start everything (detached)"
	@echo "  make infra      Start only datastores + MQ + Ollama"
	@echo "  make pull-llm   Pull the Ollama model"
	@echo "  make down       Stop + remove containers (keep volumes)"
	@echo "  make reset      Down + delete volumes (DESTRUCTIVE)"
	@echo "  make stop       Stop containers (keep them)"
	@echo "  make restart    Restart everything"
	@echo "  make logs s=ceo-agent-service   Tail logs for a service"
	@echo "  make ps         List running containers"
	@echo "  make be-build   dotnet build the solution"
	@echo "  make fe-build   Build all 4 Angular apps"
	@echo "  make test       Run .NET + Angular tests"

up:        ; ./start.sh up
infra:     ; ./start.sh infra
pull-llm:  ; ./start.sh pull-llm
down:      ; $(COMPOSE) down
stop:      ; $(COMPOSE) stop
restart:   ; $(COMPOSE) restart
ps:        ; $(COMPOSE) ps
logs:      ; $(COMPOSE) logs -f --tail=200 $(s)
reset:     ; $(COMPOSE) down -v
build:     ; $(COMPOSE) build

be-build:
	cd backend && dotnet build IWX.Boardroom.slnx -c Release

fe-build:
	for app in angular-dashboard admin-panel realtime-monitor analytics-dashboard; do \
	  echo "==> $$app"; \
	  (cd frontend/$$app && npm ci && npm run build); \
	done

test:
	cd backend && dotnet test IWX.Boardroom.slnx --no-build -c Release || true
	for app in angular-dashboard admin-panel realtime-monitor analytics-dashboard; do \
	  (cd frontend/$$app && npm test -- --watch=false --browsers=ChromeHeadless) || true; \
	done

clean:
	cd backend && dotnet clean IWX.Boardroom.slnx || true
	find . -type d -name 'bin' -prune -exec rm -rf {} +
	find . -type d -name 'obj' -prune -exec rm -rf {} +
	find . -type d -name 'node_modules' -prune -exec rm -rf {} +
	find . -type d -name 'dist' -prune -exec rm -rf {} +
