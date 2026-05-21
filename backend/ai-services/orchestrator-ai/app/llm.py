import httpx
from tenacity import retry, stop_after_attempt, wait_exponential
from .config import settings


@retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=1, max=8))
async def run_llm(prompt: str) -> str:
    payload = {
        "model": settings.ollama_model,
        "prompt": prompt,
        "stream": False,
        "options": {"temperature": 0.4},
    }
    async with httpx.AsyncClient(timeout=120.0) as client:
        r = await client.post(f"{settings.ollama_host}/api/generate", json=payload)
        r.raise_for_status()
        data = r.json()
        return data.get("response", "").strip()
