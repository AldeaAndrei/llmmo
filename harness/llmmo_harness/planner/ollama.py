import json
import re

import httpx

from llmmo_harness.client import GameClient
from llmmo_harness.config import OllamaConfig
from llmmo_harness.memory import DecisionMemory
from llmmo_harness.planner.validation import filter_plan_to_possible_actions
from llmmo_harness.schema import CommandPlan, set_building_types
from llmmo_harness.state import (
    build_planner_hints,
    compact_possible_actions,
    format_recent_decisions,
    resolve_first_city_id,
)

SYSTEM_PROMPT = """You are a strategic planner for LLMMO, a tick-based city builder.
Output ONLY valid JSON (no markdown, no commentary outside the JSON).

Schema:
{{
  "schemaVersion": 1,
  "observedAtTick": <number>,
  "commands": [
    {{ "type": "upgrade", "buildingType": "<building>", "reason": "<1-2 sentences>" }},
    {{ "type": "train", "troopType": "soldier"|"spy", "count": 1, "reason": "<1-2 sentences>" }},
    {{ "type": "attack", "targetCityId": "<uuid>", "troopType": "soldier", "count": 1, "reason": "<1-2 sentences>" }},
    {{ "type": "attack", "targetCityId": "<uuid>", "troopType": "spy", "count": 1, "reason": "<1-2 sentences>" }},
    {{ "type": "message", "toPlayerId": "<uuid>", "subject": "<text>", "body": "<text>", "reason": "<1-2 sentences>" }},
    {{ "type": "ally", "toPlayerId": "<uuid>", "reason": "<1-2 sentences>" }},
    {{ "type": "enemy", "toPlayerId": "<uuid>", "reason": "<1-2 sentences>" }},
    {{ "type": "clear_relation", "toPlayerId": "<uuid>", "reason": "<1-2 sentences>" }}
  ]
}}

Planning rules (follow strictly):
1. You may ONLY choose commands that appear in the Possible actions section below.
2. For upgrade, use a buildingType from upgrades.
3. For train, use troopType/count exactly as listed in train (always count 1).
4. For attack, use targetCityId from targets where canAttack is true (soldier) or canScout is true (spy).
5. Training a spy does NOT scout. Scouting requires attack with troopType spy to a target where canScout is true.
6. Diplomacy commands use toPlayerId from diplomacy.players.
7. Only send a message when diplomacy.canSendMessage is true.
8. Only declare ally/enemy/clear_relation when diplomacy.canDeclareDiplomacy is true.
9. Prefer 1–2 commands per plan. If no valid actions exist, return `"commands": []`.
10. In upgrade reasons, name fromLevel→toLevel from the matching upgrade entry.
11. Do not repeat the same action as your last two decisions unless still clearly optimal.
12. Each reason must be specific to this tick (no copy-paste).
13. If you have 2+ spies, scout a nearby city (attack + spy) before training another spy.
14. If diplomacy.latestUnreadMessage is present, consider replying with a message command when canSendMessage is true.
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
        city_id = resolve_first_city_id(client)
        buildings = client.get_building_catalog()
        set_building_types(entry["type"] for entry in buildings)

        possible = client.get_possible_actions(city_id)
        observed_tick = int(possible.get("currentTick", 0))
        prompt_actions = compact_possible_actions(possible)
        recent = format_recent_decisions(self.memory.get_recent(city_id, limit=2))

        system = SYSTEM_PROMPT

        user_parts = [
            "Possible actions (only these commands are valid right now):\n"
            f"{json.dumps(prompt_actions, separators=(',', ':'))}",
        ]
        if recent:
            user_parts.append(
                "Your last two executed decisions (avoid repeating unless still optimal):\n"
                f"{json.dumps(recent, separators=(',', ':'))}"
            )
        hints = build_planner_hints(possible, recent)
        if hints:
            user_parts.append("Strategic hints:\n" + "\n".join(f"- {hint}" for hint in hints))
        user_parts.append(
            f"Set observedAtTick to {observed_tick}. "
            "Return a plan with 0–2 commands taken only from Possible actions."
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
        plan = CommandPlan.model_validate(raw)
        plan, dropped = filter_plan_to_possible_actions(plan, possible)
        if dropped:
            print(
                f"dropped {len(dropped)} command(s) not in possible actions: "
                + ", ".join(dropped),
                flush=True,
            )
        return plan
