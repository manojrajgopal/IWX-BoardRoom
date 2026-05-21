import logging
from typing import Any

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from .crew import run_crew

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")

app = FastAPI(title="IWX reasoning-engine", version="0.1.0")


class ReasonRequest(BaseModel):
    department: str = Field(..., min_length=1, max_length=64)
    task_title: str
    task_description: str
    extra_context: dict[str, Any] | None = None


@app.get("/health")
async def health():
    return {"status": "ok", "service": "reasoning-engine"}


@app.post("/reason")
async def reason(req: ReasonRequest):
    try:
        return await run_crew(
            department=req.department,
            task_title=req.task_title,
            task_description=req.task_description,
            extra_context=req.extra_context,
        )
    except Exception as e:
        raise HTTPException(status_code=502, detail=str(e))
