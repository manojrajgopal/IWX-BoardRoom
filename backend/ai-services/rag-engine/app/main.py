import logging
import uuid
from typing import Any

import chromadb
from chromadb.config import Settings as ChromaSettings
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from . import embeddings
from .config import settings

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")

_client = chromadb.PersistentClient(
    path=settings.chroma_path,
    settings=ChromaSettings(anonymized_telemetry=False, allow_reset=False),
)


def _collection(scope: str):
    return _client.get_or_create_collection(name=f"iwx_{scope}", metadata={"hnsw:space": "cosine"})


class Document(BaseModel):
    id: str | None = None
    text: str
    metadata: dict[str, Any] = {}


class IngestRequest(BaseModel):
    scope: str = Field(..., min_length=1, max_length=64)
    documents: list[Document]


class QueryRequest(BaseModel):
    scope: str
    query: str
    top_k: int = Field(5, ge=1, le=50)
    where: dict[str, Any] | None = None


app = FastAPI(title="IWX rag-engine", version="0.1.0")


@app.get("/health")
async def health():
    return {"status": "ok", "service": "rag-engine"}


@app.get("/collections")
async def collections():
    return {"collections": [c.name for c in _client.list_collections()]}


@app.post("/ingest")
async def ingest(req: IngestRequest):
    if not req.documents:
        raise HTTPException(400, "documents must not be empty")
    col = _collection(req.scope)
    texts = [d.text for d in req.documents]
    ids = [d.id or str(uuid.uuid4()) for d in req.documents]
    metas = [d.metadata or {} for d in req.documents]
    vectors = await embeddings.embed(texts)
    col.upsert(ids=ids, embeddings=vectors, documents=texts, metadatas=metas)
    return {"scope": req.scope, "ingested": len(ids), "ids": ids}


@app.post("/query")
async def query(req: QueryRequest):
    col = _collection(req.scope)
    q_vec = (await embeddings.embed([req.query]))[0]
    res = col.query(
        query_embeddings=[q_vec],
        n_results=req.top_k,
        where=req.where,
    )
    hits = []
    if res and res.get("ids") and res["ids"][0]:
        for i, _id in enumerate(res["ids"][0]):
            hits.append({
                "id": _id,
                "text": res["documents"][0][i] if res.get("documents") else None,
                "metadata": res["metadatas"][0][i] if res.get("metadatas") else None,
                "distance": res["distances"][0][i] if res.get("distances") else None,
            })
    return {"scope": req.scope, "query": req.query, "hits": hits}


@app.delete("/collections/{scope}")
async def drop(scope: str):
    _client.delete_collection(name=f"iwx_{scope}")
    return {"deleted": scope}
