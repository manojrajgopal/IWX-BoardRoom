from typing import List
import httpx
from .config import settings


async def embed(texts: List[str]) -> List[List[float]]:
    async with httpx.AsyncClient(timeout=settings.request_timeout_seconds) as c:
        r = await c.post(f"{settings.vector_engine_url}/embed", json={"texts": texts})
        r.raise_for_status()
        return r.json()["vectors"]
