from typing import Literal, Optional

import httpx
from pydantic import BaseModel, Field

from .config import settings


Provider = Literal["ollama", "openai"]


class CompletionRequest(BaseModel):
    provider: Provider = "ollama"
    model: Optional[str] = None
    prompt: str
    system: Optional[str] = None
    temperature: float = Field(0.2, ge=0.0, le=2.0)
    max_tokens: int = Field(1024, gt=0, le=8192)


class CompletionResponse(BaseModel):
    provider: Provider
    model: str
    output: str
    usage: dict | None = None


async def complete(req: CompletionRequest) -> CompletionResponse:
    if req.provider == "ollama":
        return await _ollama(req)
    if req.provider == "openai":
        return await _openai(req)
    raise ValueError(f"Unsupported provider: {req.provider}")


async def _ollama(req: CompletionRequest) -> CompletionResponse:
    model = req.model or settings.ollama_default_model
    body = {
        "model": model,
        "prompt": req.prompt if not req.system else f"System: {req.system}\n\nUser: {req.prompt}",
        "stream": False,
        "options": {"temperature": req.temperature, "num_predict": req.max_tokens},
    }
    async with httpx.AsyncClient(timeout=settings.request_timeout_seconds) as c:
        r = await c.post(f"{settings.ollama_host}/api/generate", json=body)
        r.raise_for_status()
        data = r.json()
    return CompletionResponse(
        provider="ollama",
        model=model,
        output=data.get("response", ""),
        usage={
            "promptTokens": data.get("prompt_eval_count"),
            "completionTokens": data.get("eval_count"),
            "totalDurationNs": data.get("total_duration"),
        },
    )


async def _openai(req: CompletionRequest) -> CompletionResponse:
    if not settings.openai_api_key:
        raise RuntimeError("OpenAI provider not configured (IWX_OPENAI_API_KEY missing).")
    model = req.model or settings.openai_default_model
    messages = []
    if req.system:
        messages.append({"role": "system", "content": req.system})
    messages.append({"role": "user", "content": req.prompt})
    body = {
        "model": model,
        "messages": messages,
        "temperature": req.temperature,
        "max_tokens": req.max_tokens,
    }
    async with httpx.AsyncClient(timeout=settings.request_timeout_seconds) as c:
        r = await c.post(
            f"{settings.openai_base_url}/chat/completions",
            json=body,
            headers={"Authorization": f"Bearer {settings.openai_api_key}"},
        )
        r.raise_for_status()
        data = r.json()
    choice = data["choices"][0]["message"]["content"]
    return CompletionResponse(
        provider="openai",
        model=model,
        output=choice,
        usage=data.get("usage"),
    )
