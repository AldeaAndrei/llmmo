import json
import re

import httpx

from llmmo_harness.client import GameClient
from llmmo_harness.config import OllamaConfig
from llmmo_harness.schema import BUILDING_TYPES, CommandPlan, TROOP_TYPES
from llmmo_harness.state import resolve_first_city

SYSTEM_PROMPT = """You are a game agent planner for LLMMO.
Output ONLY valid JSON matching this schema (no markdown, no commentary):

{
  "schemaVersion": 1,
  "observedAtTick": <number>,
  "commands": [
    { "type": "upgrade", "buildingType": "<building>" },
    { "type": "train", "troopType": "<troop>", "count": <positive int> }
  ]
}

Allowed buildingType values: {buildings}
Allowed troopType values: {troops}
Allowed command types: upgrade, train
Do not include cityId. Prefer economical upgrades and small troop training.
"""


def _extract_json(text: str) -> str:
    text = text.strip()
    if text.startswith("```"):
        match = re.search(r"```(?:json)?\s*([\s\S]*?)```", text)
        if match:
            return match.group(1).strip()
    return text


class OllamaPlanner:
    def __init__(self, config: OllamaConfig) -> None:
        self.config = config

    def plan(self, client: GameClient) -> CommandPlan:
        world = client.get_world()
        city = resolve_first_city(client)
        troops = client.get_troop_catalog()

        observed_tick = int(world.get("currentTick", 0))
        state = {
            "world": world,
            "city": city,
            "troopCatalog": troops,
        }

        system = SYSTEM_PROMPT.format(
            buildings=", ".join(sorted(BUILDING_TYPES)),
            troops=", ".join(sorted(TROOP_TYPES)),
        )
        user_content = (
            f"Current game state:\n{json.dumps(state, indent=2)}\n\n"
            f"Set observedAtTick to {observed_tick}. "
            "Return a short plan with upgrade and train commands."
        )

        url = f"{self.config.base_url.rstrip('/')}/chat/completions"
        payload = {
            "model": self.config.model,
            "temperature": self.config.temperature,
            "messages": [
                {"role": "system", "content": system},
                {"role": "user", "content": user_content},
            ],
        }

        with httpx.Client(timeout=120.0) as http:
            response = http.post(url, json=payload)
            response.raise_for_status()
            data = response.json()

        content = data["choices"][0]["message"]["content"]
        raw = json.loads(_extract_json(content))
        raw["observedAtTick"] = observed_tick
        return CommandPlan.model_validate(raw)
