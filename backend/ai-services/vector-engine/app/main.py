import logging
from typing import List

from fastapi import FastAPI
from pydantic import BaseModel, Field
from sentence_transformers import SentenceTransformer

from .config import settings

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
log = logging.getLogger("vector-engine")

_model: SentenceTransformer | None = None


def _get_model() -> SentenceTransformer:
    global _model
    if _model is None:
        log.info("Loading embedding model %s on %s", settings.model_name, settings.device)
        _model = SentenceTransformer(settings.model_name, device=settings.device)
    return _model


app = FastAPI(title="IWX vector-engine", version="0.1.0")


class EmbedRequest(BaseModel):
    texts: List[str] = Field(..., min_length=1)
    normalize: bool = True


class EmbedResponse(BaseModel):
    model: str
    dimension: int
    vectors: List[List[float]]


@app.get("/health")
async def health():
    return {"status": "ok", "service": "vector-engine", "model": settings.model_name}


@app.on_event("startup")
async def warmup():
    _get_model().encode(["warmup"])


@app.post("/embed", response_model=EmbedResponse)
async def embed(req: EmbedRequest):
    model = _get_model()
    vecs = model.encode(req.texts, normalize_embeddings=req.normalize, convert_to_numpy=True)
    return EmbedResponse(
        model=settings.model_name,
        dimension=int(vecs.shape[1]),
        vectors=vecs.tolist(),
    )
