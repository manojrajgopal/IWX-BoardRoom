import logging

from fastapi import FastAPI, HTTPException

from .providers import CompletionRequest, CompletionResponse, complete

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")

app = FastAPI(title="IWX llm-router", version="0.1.0")


@app.get("/health")
async def health():
    return {"status": "ok", "service": "llm-router"}


@app.get("/providers")
async def providers():
    return {"providers": ["ollama", "openai"]}


@app.post("/complete", response_model=CompletionResponse)
async def post_complete(req: CompletionRequest):
    try:
        return await complete(req)
    except Exception as e:
        raise HTTPException(status_code=502, detail=str(e))
