import asyncio
import logging

from fastapi import FastAPI
from pydantic import BaseModel

from .bus import consume_forever, emit_completed, emit_thinking
from .config import settings
from .llm import run_llm

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(name)s - %(message)s")
logger = logging.getLogger("orchestrator")

app = FastAPI(title="IWX Orchestrator AI", version="0.1.0")

_consumer_task: asyncio.Task | None = None


@app.on_event("startup")
async def _startup() -> None:
    global _consumer_task
    _consumer_task = asyncio.create_task(_safe_consumer())


async def _safe_consumer() -> None:
    while True:
        try:
            await consume_forever()
        except Exception as e:
            logger.exception("Consumer crashed, restarting in 5s: %s", e)
            await asyncio.sleep(5)


@app.on_event("shutdown")
async def _shutdown() -> None:
    if _consumer_task:
        _consumer_task.cancel()


@app.get("/health")
async def health() -> dict:
    return {"status": "ok", "service": settings.service_name, "model": settings.ollama_model}


class TestPromptRequest(BaseModel):
    prompt: str


@app.post("/test-llm")
async def test_llm(req: TestPromptRequest) -> dict:
    text = await run_llm(req.prompt)
    return {"response": text}


class ManualDispatchRequest(BaseModel):
    taskId: str
    title: str
    description: str
    department: str


@app.post("/dispatch")
async def manual_dispatch(req: ManualDispatchRequest) -> dict:
    await emit_thinking(req.taskId, req.department, "manual-dispatch", "Manual dispatch via API.")
    prompt = f"Department: {req.department}\nTask: {req.title}\n{req.description}\nProduce a short plan."
    out = await run_llm(prompt)
    await emit_completed(req.taskId, req.department, out.split("\n")[0][:280], out)
    return {"dispatched": True}
