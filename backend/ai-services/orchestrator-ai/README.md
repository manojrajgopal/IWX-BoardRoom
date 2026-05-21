# Orchestrator AI

FastAPI service that:
- Subscribes to `iwx.task.approved` on RabbitMQ
- Calls Ollama via LangChain/HTTP to produce a plan
- Emits `iwx.agent.thinking` and `iwx.task.completed` events

## Local run

```bash
cd backend/ai-services/orchestrator-ai
python -m venv .venv && source .venv/Scripts/activate
pip install -r requirements.txt
export IWX_RABBITMQ_HOST=localhost IWX_OLLAMA_HOST=http://localhost:11434
uvicorn app.main:app --reload --port 8000
```
