import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException

from . import store
from .store import PromptUpsert, PromptVersion, RenderRequest

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")


@asynccontextmanager
async def lifespan(_: FastAPI):
    await store.ensure_indexes()
    yield


app = FastAPI(title="IWX prompt-engine", version="0.1.0", lifespan=lifespan)


@app.get("/health")
async def health():
    return {"status": "ok", "service": "prompt-engine"}


@app.post("/prompts", response_model=PromptVersion)
async def post_prompt(p: PromptUpsert):
    return await store.upsert(p)


@app.get("/prompts")
async def list_names():
    return {"names": await store.list_all_names()}


@app.get("/prompts/{name}", response_model=list[PromptVersion])
async def list_versions(name: str):
    return await store.list_versions(name)


@app.get("/prompts/{name}/latest", response_model=PromptVersion)
async def latest(name: str):
    v = await store.get_version(name, None)
    if v is None:
        raise HTTPException(404, "not found")
    return v


@app.get("/prompts/{name}/{version}", response_model=PromptVersion)
async def specific(name: str, version: int):
    v = await store.get_version(name, version)
    if v is None:
        raise HTTPException(404, "not found")
    return v


@app.post("/prompts/{name}/render")
async def render_latest(name: str, req: RenderRequest):
    v = await store.get_version(name, None)
    if v is None:
        raise HTTPException(404, "prompt not found")
    try:
        return {"name": name, "version": v.version, "rendered": store.render(v.template, req.variables)}
    except ValueError as e:
        raise HTTPException(400, str(e))


@app.post("/prompts/{name}/{version}/render")
async def render_specific(name: str, version: int, req: RenderRequest):
    v = await store.get_version(name, version)
    if v is None:
        raise HTTPException(404, "prompt+version not found")
    try:
        return {"name": name, "version": version, "rendered": store.render(v.template, req.variables)}
    except ValueError as e:
        raise HTTPException(400, str(e))
