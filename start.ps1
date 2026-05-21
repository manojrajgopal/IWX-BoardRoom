#!/usr/bin/env pwsh
# =============================================================================
# IWX Boardroom — one-shot bootstrap (Windows / PowerShell)
# =============================================================================
# Usage:
#   ./start.ps1                  → build + start everything (detached)
#   ./start.ps1 -Stop            → stop the stack
#   ./start.ps1 -Down            → stop + remove containers/networks
#   ./start.ps1 -Logs ceo        → tail logs of a specific service
#   ./start.ps1 -PullLLM         → also pull the Ollama model
#   ./start.ps1 -NoBuild         → start without rebuilding images
#   ./start.ps1 -OnlyInfra       → start only infra (SQL/Mongo/Redis/RMQ/Kafka/Ollama)
# =============================================================================

[CmdletBinding()]
param(
    [switch]$Stop,
    [switch]$Down,
    [switch]$PullLLM,
    [switch]$NoBuild,
    [switch]$OnlyInfra,
    [string]$Logs
)

$ErrorActionPreference = 'Stop'
$root        = $PSScriptRoot
$envFile     = Join-Path $root '.env'
$envExample  = Join-Path $root '.env.example'
$composeFile = Join-Path $root 'devops/docker/docker-compose.yml'

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[!]  $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "[X]  $msg" -ForegroundColor Red }

# ---------- 1. Sanity checks ----------
Write-Step "Checking prerequisites"
foreach ($cmd in @('docker')) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Err "$cmd is required but not on PATH."
        exit 1
    }
}
try { docker info | Out-Null } catch { Write-Err "Docker daemon is not running."; exit 1 }
Write-Ok "Docker is up."

# ---------- 2. Ensure .env exists ----------
if (-not (Test-Path $envFile)) {
    if (Test-Path $envExample) {
        Copy-Item $envExample $envFile
        Write-Warn ".env was missing — created from .env.example. Edit it to add real keys."
    } else {
        Write-Err ".env and .env.example both missing — cannot continue."
        exit 1
    }
} else {
    Write-Ok ".env present."
}

$compose = @('docker','compose','--env-file',$envFile,'-f',$composeFile)

# ---------- 3. Handle commands ----------
if ($Stop) {
    Write-Step "Stopping IWX stack"
    & $compose[0] $compose[1..($compose.Length-1)] stop
    Write-Ok "Stopped."
    exit 0
}
if ($Down) {
    Write-Step "Tearing down IWX stack (containers + network, volumes preserved)"
    & $compose[0] $compose[1..($compose.Length-1)] down
    Write-Ok "Removed."
    exit 0
}
if ($Logs) {
    & $compose[0] $compose[1..($compose.Length-1)] logs -f --tail=200 $Logs
    exit $LASTEXITCODE
}

# ---------- 4. Bring up infra first (so apps depend on healthy infra) ----------
$infra = @('sqlserver','mongo','redis','rabbitmq','kafka','ollama')
Write-Step "Starting infrastructure: $($infra -join ', ')"
$args = @('up','-d')
if (-not $NoBuild) { $args += '--build' }
& $compose[0] $compose[1..($compose.Length-1)] @args @infra
if ($LASTEXITCODE -ne 0) { Write-Err "Infra failed to start."; exit 1 }
Write-Ok "Infra running."

if ($OnlyInfra) {
    Write-Step "OnlyInfra mode — skipping app services."
    exit 0
}

# ---------- 5. Pull LLM model if requested ----------
if ($PullLLM) {
    $model = (Get-Content $envFile | Select-String '^OLLAMA_MODEL=').ToString().Split('=')[1]
    if (-not $model) { $model = 'llama3.2:3b' }
    Write-Step "Pulling Ollama model: $model (may take a few minutes the first time)"
    docker exec -it iwx-ollama ollama pull $model
}

# ---------- 6. Bring up everything else ----------
Write-Step "Starting full application stack"
$args = @('up','-d')
if (-not $NoBuild) { $args += '--build' }
& $compose[0] $compose[1..($compose.Length-1)] @args
if ($LASTEXITCODE -ne 0) { Write-Err "Stack failed to start."; exit 1 }

# ---------- 7. Summary ----------
Write-Host ""
Write-Ok  "IWX Boardroom is up."
Write-Host ""
Write-Host " Dashboard            http://localhost:4200" -ForegroundColor Green
Write-Host " Admin Panel          http://localhost:4201" -ForegroundColor Green
Write-Host " Realtime Monitor     http://localhost:4202" -ForegroundColor Green
Write-Host " Analytics Dashboard  http://localhost:4203" -ForegroundColor Green
Write-Host " API Gateway          http://localhost:8080" -ForegroundColor Green
Write-Host " CEO Agent (swagger)  http://localhost:8081/swagger" -ForegroundColor Green
Write-Host " Orchestrator AI      http://localhost:8000/docs" -ForegroundColor Green
Write-Host " RabbitMQ UI          http://localhost:15672  (iwx / iwx)" -ForegroundColor Green
Write-Host ""
Write-Host " Logs:    ./start.ps1 -Logs <service>"
Write-Host " Stop:    ./start.ps1 -Stop"
Write-Host " Down:    ./start.ps1 -Down"
Write-Host ""
