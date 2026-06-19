import json
import re

import httpx

from llmmo_harness.client import GameClient
from llmmo_harness.config import OllamaConfig
from llmmo_harness.memory import DecisionMemory
from llmmo_harness.schema import BUILDING_TYPES, CommandPlan, TROOP_TYPES, set_building_types
from llmmo_harness.state import (
    compact_planner_state,
    format_recent_decisions,
    resolve_first_city,
    resolve_first_city_id,
)

SYSTEM_PROMPT = """You are a game agent planner for LLMMO.
Output ONLY valid JSON matching this schema (no markdown, no commentary):

{{
  "schemaVersion": 1,
  "observedAtTick": <number>,
  "commands": [
    {{
      "type": "upgrade",
      "buildingType": "<building>",
      "reason": "<1-2 sentences grounded in game state>"
    }},
    {{
      "type": "train",
      "troopType": "<troop>",
      "count": <positive int>,
      "reason": "<1-2 sentences grounded in game state>"
    }}
  ]
}}

Allowed buildingType values: {buildings}
Allowed troopType values: {troops}
Allowed command types: upgrade, train
Every command MUST include a reason (1-2 sentences) explaining why, based on resources, building levels, and troops.
Do not include cityId. Prefer economical upgrades and small troop training.
"""


def _extract_json(text: str) -> str:
    text = text.strip()
    if text.startswith("```"):
        match = re.search(r"```(?:json)?\s*([\s\S]*?)```", text)
        if match:
            return match.group(1).strip()
    return text


def _log_llm_exchange(
    *,
    model: str,
    system: str,
    user: str,
    response: str,
) -> None:
    divider = "=" * 60
    print(divider, flush=True)
    print(f"OLLAMA PLAN  model={model}", flush=True)
    print(divider, flush=True)
    print("--- system ---", flush=True)
    print(system, flush=True)
    print("--- user ---", flush=True)
    print(user, flush=True)
    print("--- response ---", flush=True)
    print(response, flush=True)
    print(divider, flush=True)


class OllamaPlanner:
    def __init__(self, config: OllamaConfig, memory: DecisionMemory) -> None:
        self.config = config
        self.memory = memory

    def plan(self, client: GameClient) -> CommandPlan:
        world = client.get_world()
        city = resolve_first_city(client)
        city_id = resolve_first_city_id(client)
        troops = client.get_troop_catalog()
        buildings = client.get_building_catalog()
        set_building_types(entry["type"] for entry in buildings)

        observed_tick = int(world.get("currentTick", 0))
        state = compact_planner_state(world, city, troops)
        recent = format_recent_decisions(self.memory.get_recent(city_id, limit=2))

        system = SYSTEM_PROMPT.format(
            buildings=", ".join(sorted(BUILDING_TYPES)),
            troops=", ".join(sorted(TROOP_TYPES)),
        )

        user_parts = [
            f"Current game state:\n{json.dumps(state, separators=(',', ':'))}",
        ]
        if recent:
            user_parts.append(
                "Avoid repeating the same action unless still optimal. "
                f"Your last decisions:\n{json.dumps(recent, separators=(',', ':'))}"
            )
        user_parts.append(
            f"Set observedAtTick to {observed_tick}. "
            "Return a short plan with upgrade and train commands."
        )
        user_content = "\n\n".join(user_parts)

        url = f"{self.config.base_url.rstrip('/')}/chat/completions"
        payload = {
            "model": self.config.model,
            "temperature": self.config.temperature,
            "messages": [
                {"role": "system", "content": system},
                {"role": "user", "content": user_content},
            ],
        }

        with httpx.Client(timeout=self.config.request_timeout_seconds) as http:
            response = http.post(url, json=payload)
            response.raise_for_status()
            data = response.json()

        content = data["choices"][0]["message"]["content"]
        if self.config.log_prompts:
            _log_llm_exchange(
                model=self.config.model,
                system=system,
                user=user_content,
                response=content,
            )
        raw = json.loads(_extract_json(content))
        raw["observedAtTick"] = observed_tick
        return CommandPlan.model_validate(raw)
