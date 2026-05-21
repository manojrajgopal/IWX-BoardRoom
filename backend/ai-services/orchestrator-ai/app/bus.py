import asyncio
import json
import logging
from datetime import datetime, timezone
from typing import Optional

import aio_pika
from aio_pika import ExchangeType, Message
from aio_pika.abc import AbstractRobustConnection

from .config import settings
from .llm import run_llm

logger = logging.getLogger("orchestrator.bus")

_connection: Optional[AbstractRobustConnection] = None


async def get_connection() -> AbstractRobustConnection:
    global _connection
    if _connection is None or _connection.is_closed:
        url = f"amqp://{settings.rabbitmq_user}:{settings.rabbitmq_pass}@{settings.rabbitmq_host}:{settings.rabbitmq_port}/"
        _connection = await aio_pika.connect_robust(url)
    return _connection


async def publish(exchange_name: str, payload: dict) -> None:
    conn = await get_connection()
    channel = await conn.channel()
    exchange = await channel.declare_exchange(exchange_name, ExchangeType.FANOUT, durable=True)
    body = json.dumps(payload).encode("utf-8")
    await exchange.publish(
        Message(body=body, content_type="application/json", delivery_mode=aio_pika.DeliveryMode.PERSISTENT),
        routing_key="",
    )


async def emit_thinking(task_id: str, department: str, stage: str, message: str) -> None:
    await publish(
        settings.queue_agent_thinking,
        {
            "taskId": task_id,
            "department": department,
            "stage": stage,
            "message": message,
            "timestampUtc": datetime.now(timezone.utc).isoformat(),
        },
    )


async def emit_completed(task_id: str, department: str, summary: str, payload_json: str) -> None:
    await publish(
        settings.queue_task_completed,
        {
            "taskId": task_id,
            "targetDepartment": department,
            "resultSummary": summary,
            "resultPayloadJson": payload_json,
            "completedAtUtc": datetime.now(timezone.utc).isoformat(),
        },
    )


async def handle_task_approved(message: aio_pika.IncomingMessage) -> None:
    async with message.process(requeue=False):
        try:
            envelope = json.loads(message.body.decode("utf-8"))
            data = envelope.get("message", envelope)
            task_id = data["taskId"]
            title = data["title"]
            description = data["description"]
            department = data["targetDepartment"]

            # Phase 2: dedicated department agent services own their own work.
            # The orchestrator only acts on tasks routed to the CEO board
            # (i.e. cross-departmental / unrouted work that needs LLM planning).
            if department.lower() != "ceo":
                logger.info("Skipping task %s for %s (handled by dept service)", task_id, department)
                return

            logger.info("Task %s approved for %s", task_id, department)
            await emit_thinking(task_id, department, "received", f"Department '{department}' picked up task.")
            await emit_thinking(task_id, department, "planning", "Drafting execution plan with LLM.")

            prompt = (
                f"You are the {department.upper()} department head of an autonomous AI company.\n"
                f"Task title: {title}\n"
                f"Task description: {description}\n\n"
                f"Produce a concise, actionable plan (5-8 bullet points), then a 2-sentence executive summary."
            )
            try:
                output = await run_llm(prompt)
            except Exception as e:
                logger.exception("LLM call failed: %s", e)
                output = f"[LLM unavailable] Stub plan for task '{title}'."

            await emit_thinking(task_id, department, "completed", "Plan drafted, reporting to CEO.")
            summary = output.split("\n")[0][:280] if output else "No summary"
            payload_json = json.dumps({"plan": output, "model": settings.ollama_model})
            await emit_completed(task_id, department, summary, payload_json)
        except Exception as e:
            logger.exception("Failed handling task: %s", e)


async def consume_forever() -> None:
    conn = await get_connection()
    channel = await conn.channel()
    await channel.set_qos(prefetch_count=4)
    exchange = await channel.declare_exchange(settings.queue_task_approved, ExchangeType.FANOUT, durable=True)
    queue = await channel.declare_queue("orchestrator.task.approved", durable=True)
    await queue.bind(exchange)
    logger.info("Consuming %s", settings.queue_task_approved)
    await queue.consume(handle_task_approved)
    await asyncio.Future()
