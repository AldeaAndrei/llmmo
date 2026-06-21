import json
import re

import httpx

from llmmo_harness.client import GameClient
from llmmo_harness.config import OllamaConfig
from llmmo_harness.memory import DecisionMemory
from llmmo_harness.planner.normalize import parse_plan_from_llm
from llmmo_harness.planner.validation import filter_plan_to_possible_actions
from llmmo_harness.schema import CommandPlan, set_building_types
from llmmo_harness.state import (
    build_planner_context,
    format_recent_decisions,
    resolve_first_city,
    resolve_first_city_id,
)

PERSONALITY_PROMPT = """You are a neutral strategic governor for a tick-based city builder.
You pursue economic growth and survival.
You attack players you have declared as enemies when you can field troops.
You never attack declared allies.
You respond to diplomacy when you receive messages."""

GAME_RULES_PROMPT = """Output ONLY valid JSON (no markdown, no commentary outside the JSON).

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

Example valid plan (scout):
{{
  "schemaVersion": 1,
  "observedAtTick": 100,
  "commands": [
    {{
      "type": "attack",
      "targetCityId": "00000000-0000-0000-0000-000000000001",
      "troopType": "spy",
      "count": 1,
      "reason": "Scout the nearest city before training more troops."
    }}
  ]
}}

Game rules (follow strictly):
0. Every command object MUST include "reason" (non-empty string). Never omit reason.
0b. For train and attack, "count" is always exactly 1. troops[].count is inventory, not command count.
1. You may ONLY choose commands allowed by availableActions in the context below.
2. For upgrade, use a buildingType from availableActions.upgrades.
3. For train, use troopType/count exactly as listed in availableActions.train (always count 1).
4. For attack, use targetCityId from availableActions.targets where canAttack is true (soldier) or canScout is true (spy).
5. Training a spy does NOT scout. Scouting requires attack with troopType spy, count 1, and a reason.
6. diplomacy.relations lists only declared allies and enemies (no neutral players).
7. Message/ally/enemy/clear_relation use toPlayerId from targets[].targetPlayerId, diplomacy.relations, or latestUnreadMessage.fromPlayerId.
8. Only send a message when diplomacy.canSendMessage is true.
9. Only declare ally/enemy/clear_relation when diplomacy.canDeclareDiplomacy is true.
10. Prefer 1–2 commands per plan. If no valid actions exist, return `"commands": []`.
11. In upgrade reasons, name fromLevel→toLevel from the matching upgrade entry.
12. Do not repeat the same action as your last two decisions unless still clearly optimal.
13. Each reason must be specific to this tick (no copy-paste).
14. When availableActions.diplomacyOnly is true, only message/ally/enemy/clear_relation commands are valid.
15. Never attack a target with relation "ally"."""

SYSTEM_PROMPT = f"{PERSONALITY_PROMPT}\n\n{GAME_RULES_PROMPT}"


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
        city = resolve_first_city(client)
        city_id = str(city["id"])
        buildings = client.get_building_catalog()
        set_building_types(entry["type"] for entry in buildings)

        possible = client.get_possible_actions(city_id)
        reports = client.get_reports()
        observed_tick = int(possible.get("currentTick", 0))
        recent = format_recent_decisions(self.memory.get_recent(city_id, limit=2))
        context = build_planner_context(city, possible, reports, recent)

        system = SYSTEM_PROMPT

        user_content = (
            "Game context (JSON sections):\n"
            f"{json.dumps(context, separators=(',', ':'))}\n\n"
            f"Set observedAtTick to {observed_tick}. "
            "Return a plan with 0–2 commands allowed by availableActions."
        )

        url = f"{self.config.base_url.rstrip('/')}/chat/completions"
        payload = {
            "model": self.config.model,
            "temperature": self.config.temperature,
            "format": "json",
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
        plan, repairs = parse_plan_from_llm(raw, possible)
        if repairs:
            print(f"plan repairs: {'; '.join(repairs)}", flush=True)
        plan, dropped = filter_plan_to_possible_actions(plan, possible)
        if dropped:
            print(
                f"dropped {len(dropped)} command(s) not in possible actions: "
                + ", ".join(dropped),
                flush=True,
            )
        return plan
