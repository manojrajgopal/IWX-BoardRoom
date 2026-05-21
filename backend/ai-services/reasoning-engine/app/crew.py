"""
Lightweight multi-agent reasoning loop.

Each agent role gets a focused prompt, runs through the llm-router, and
feeds its output into the next role's context. Inspired by CrewAI/AutoGen
but with zero heavy deps so it boots in seconds and runs on CPU-only Ollama.
"""
from dataclasses import dataclass
from typing import Any

from . import clients


@dataclass
class AgentTurn:
    role: str
    goal: str
    output: str


DEFAULT_CREW = [
    ("planner", "Break the task into 3-6 concrete, ordered sub-steps with success criteria for each."),
    ("researcher", "Gather background facts and constraints relevant to the plan. Cite any retrieved context."),
    ("executor", "Produce the concrete deliverable (text, draft, summary, decision) implementing the plan."),
    ("critic", "Identify the top 3 risks or weaknesses in the executor's output. Be terse and surgical."),
    ("finalizer", "Apply the critic's fixes and produce a polished final response with an executive summary."),
]


async def run_crew(
    department: str,
    task_title: str,
    task_description: str,
    *,
    extra_context: dict[str, Any] | None = None,
) -> dict[str, Any]:
    # 1) Pull background from memory + rag (best effort)
    memory_hits = await clients.memory_search(department, task_title, limit=5)
    rag_hits = await clients.rag_query(department, f"{task_title}\n{task_description}", top_k=5)

    background = "\n".join(
        [f"- (memory) {h.get('key')}: {h.get('value','')[:300]}" for h in memory_hits]
        + [f"- (rag) {h.get('text','')[:300]}" for h in rag_hits]
    ) or "(no prior context available)"

    system = (
        f"You are an autonomous AI inside the IWX Boardroom acting for the "
        f"'{department}' department. Stay focused, terse, and actionable."
    )

    transcript: list[AgentTurn] = []
    prior = ""

    for role, goal in DEFAULT_CREW:
        prompt = (
            f"### Task\n{task_title}\n\n{task_description}\n\n"
            f"### Background\n{background}\n\n"
            f"### Prior crew output\n{prior or '(none yet)'}\n\n"
            f"### Your role: {role}\n{goal}\n\n"
            f"Respond in plain text. No preamble."
        )
        output = await clients.complete(prompt, system=system, temperature=0.3 if role != "executor" else 0.5)
        turn = AgentTurn(role=role, goal=goal, output=output.strip())
        transcript.append(turn)
        prior += f"\n[{role}]\n{turn.output}\n"

    final = transcript[-1].output
    return {
        "department": department,
        "task": {"title": task_title, "description": task_description},
        "background": {"memory": memory_hits, "rag": rag_hits},
        "transcript": [t.__dict__ for t in transcript],
        "final": final,
        "extra_context": extra_context or {},
    }
