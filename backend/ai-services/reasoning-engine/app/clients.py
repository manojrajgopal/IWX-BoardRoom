from typing import Any
import httpx
from .config import settings


async def complete(prompt: str, system: str | None = None, *, temperature: float = 0.2) -> str:
    async with httpx.AsyncClient(timeout=settings.request_timeout_seconds) as c:
        r = await c.post(
            f"{settings.llm_router_url}/complete",
            json={
                "provider": settings.default_provider,
                "model": settings.default_model,
                "prompt": prompt,
                "system": system,
                "temperature": temperature,
            },
        )
        r.raise_for_status()
        return r.json()["output"]


async def render_prompt(name: str, variables: dict[str, Any]) -> str | None:
    async with httpx.AsyncClient(timeout=30.0) as c:
        try:
            r = await c.post(f"{settings.prompt_engine_url}/prompts/{name}/render", json={"variables": variables})
            if r.status_code == 404:
                return None
            r.raise_for_status()
            return r.json()["rendered"]
        except httpx.RequestError:
            return None


async def memory_search(scope: str, query: str, limit: int = 5) -> list[dict[str, Any]]:
    async with httpx.AsyncClient(timeout=30.0) as c:
        try:
            r = await c.get(f"{settings.memory_engine_url}/memory/long/{scope}/search", params={"q": query, "limit": limit})
            if r.status_code != 200:
                return []
            return r.json()
        except httpx.RequestError:
            return []


async def rag_query(scope: str, query: str, top_k: int = 5) -> list[dict[str, Any]]:
    async with httpx.AsyncClient(timeout=30.0) as c:
        try:
            r = await c.post(f"{settings.rag_engine_url}/query", json={"scope": scope, "query": query, "top_k": top_k})
            if r.status_code != 200:
                return []
            return r.json().get("hits", [])
        except httpx.RequestError:
            return []
