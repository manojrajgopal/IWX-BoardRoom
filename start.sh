#!/usr/bin/env bash
# =============================================================================
# IWX Boardroom — one-shot bootstrap (bash / macOS / Linux / Git Bash)
# =============================================================================
# Usage:
#   ./start.sh               build + start everything
#   ./start.sh stop          stop the stack
#   ./start.sh down          stop + remove containers/networks
#   ./start.sh logs ceo      tail logs of a specific service
#   ./start.sh pull-llm      also pull the Ollama model
#   ./start.sh infra         start only infra
# =============================================================================
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="$ROOT/.env"
ENV_EXAMPLE="$ROOT/.env.example"
COMPOSE_FILE="$ROOT/devops/docker/docker-compose.yml"

c_cyan='\033[0;36m'; c_green='\033[0;32m'; c_yellow='\033[0;33m'; c_red='\033[0;31m'; c_off='\033[0m'
step() { echo -e "${c_cyan}==> $*${c_off}"; }
ok()   { echo -e "${c_green}[OK] $*${c_off}"; }
warn() { echo -e "${c_yellow}[!]  $*${c_off}"; }
err()  { echo -e "${c_red}[X]  $*${c_off}"; }

command -v docker >/dev/null || { err "docker not on PATH"; exit 1; }
docker info >/dev/null 2>&1 || { err "Docker daemon is not running."; exit 1; }
ok "Docker is up."

if [[ ! -f "$ENV_FILE" ]]; then
  if [[ -f "$ENV_EXAMPLE" ]]; then
    cp "$ENV_EXAMPLE" "$ENV_FILE"
    warn ".env was missing — created from .env.example. Edit it to add real keys."
  else
    err ".env and .env.example both missing — cannot continue."; exit 1
  fi
else
  ok ".env present."
fi

dc=(docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE")

cmd="${1:-up}"
case "$cmd" in
  stop)     step "Stopping IWX stack"; "${dc[@]}" stop ;;
  down)     step "Tearing down IWX stack"; "${dc[@]}" down ;;
  logs)     shift; "${dc[@]}" logs -f --tail=200 "$@" ;;
  pull-llm)
            model="$(grep -E '^OLLAMA_MODEL=' "$ENV_FILE" | cut -d= -f2)"
            : "${model:=llama3.2:3b}"
            step "Pulling Ollama model: $model"
            docker exec -it iwx-ollama ollama pull "$model"
            ;;
  infra)
            step "Starting infrastructure only"
            "${dc[@]}" up -d --build sqlserver mongo redis rabbitmq kafka ollama
            ok "Infra running."
            ;;
  up|"")
            step "Starting infrastructure"
            "${dc[@]}" up -d --build sqlserver mongo redis rabbitmq kafka ollama
            step "Starting full application stack"
            "${dc[@]}" up -d --build
            echo
            ok "IWX Boardroom is up."
            cat <<EOF

  Dashboard            http://localhost:4200
  Admin Panel          http://localhost:4201
  Realtime Monitor     http://localhost:4202
  Analytics Dashboard  http://localhost:4203
  API Gateway          http://localhost:8080
  CEO Agent (swagger)  http://localhost:8081/swagger
  Orchestrator AI      http://localhost:8000/docs
  RabbitMQ UI          http://localhost:15672  (iwx / iwx)

  Logs:    ./start.sh logs <service>
  Stop:    ./start.sh stop
  Down:    ./start.sh down

EOF
            ;;
  *) err "Unknown command: $cmd"; exit 1 ;;
esac
