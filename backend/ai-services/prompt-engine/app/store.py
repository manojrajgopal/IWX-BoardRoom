from datetime import datetime, timezone
from typing import Any, Optional

from jinja2 import Environment, StrictUndefined, TemplateError
from motor.motor_asyncio import AsyncIOMotorClient
from pydantic import BaseModel, Field

from .config import settings

_client = AsyncIOMotorClient(settings.mongo_uri)
_db = _client[settings.mongo_db]
_col = _db["templates"]


class PromptUpsert(BaseModel):
    name: str = Field(..., min_length=1, max_length=128)
    template: str
    metadata: dict[str, Any] = {}


class PromptVersion(BaseModel):
    name: str
    version: int
    template: str
    metadata: dict[str, Any]
    created_at_utc: datetime


class RenderRequest(BaseModel):
    variables: dict[str, Any] = {}


async def ensure_indexes() -> None:
    await _col.create_index([("name", 1), ("version", -1)], unique=True)


async def upsert(p: PromptUpsert) -> PromptVersion:
    latest = await _col.find_one({"name": p.name}, sort=[("version", -1)])
    next_version = (latest["version"] + 1) if latest else 1
    doc = {
        "name": p.name,
        "version": next_version,
        "template": p.template,
        "metadata": p.metadata,
        "created_at_utc": datetime.now(timezone.utc),
    }
    await _col.insert_one(doc)
    return PromptVersion(**doc)


async def get_version(name: str, version: Optional[int]) -> Optional[PromptVersion]:
    query = {"name": name}
    sort: list[tuple[str, int]] = [("version", -1)]
    if version is not None:
        query["version"] = version
        sort = []
    doc = await _col.find_one(query, sort=sort or None)
    return PromptVersion(**doc) if doc else None


async def list_versions(name: str) -> list[PromptVersion]:
    cursor = _col.find({"name": name}).sort("version", -1)
    return [PromptVersion(**d) async for d in cursor]


async def list_all_names() -> list[str]:
    return await _col.distinct("name")


_env = Environment(undefined=StrictUndefined, autoescape=False)


def render(template_str: str, variables: dict[str, Any]) -> str:
    try:
        return _env.from_string(template_str).render(**variables)
    except TemplateError as e:
        raise ValueError(f"Template render failed: {e}") from e
