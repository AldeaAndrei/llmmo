import json
import re

import httpx

from llmmo_harness.client import GameClient
from llmmo_harness.config import AgentsConfig, OllamaConfig
from llmmo_harness.memory import DecisionMemory
from llmmo_harness.planner.normalize import parse_plan_from_llm
from llmmo_harness.planner.validation import filter_plan_to_possible_actions
from llmmo_harness.schema import CommandPlan, set_building_types
from llmmo_harness.state import (
    BUILDING_COMMAND_TYPES,
    SOCIAL_COMMAND_TYPES,
    STRATEGIC_COMMAND_TYPES,
    build_building_context,
    build_social_alert,
    build_social_context,
    build_strategic_alert,
    build_strategic_context,
    enrich_possible_for_validation,
    format_recent_decisions,
    has_unread_message,
    resolve_first_city,
)

BUILDING_PERSONALITY_PROMPT = """You are the BUILDING MANAGER for a tick-based strategy city. You focus only on the city's economy and infrastructure.

Your priorities, in order:
1. Sustain. Keep food production ahead of upkeep; avoid upgrades that would leave food negative.
2. Grow. Upgrade production buildings (mines, bakery, timber) to increase income each tick.
3. Store. Raise storage when resources are near capacity so production is not wasted.
4. Fortify. Upgrade the wall when the city is vulnerable and you can afford it.

Temperament: patient and practical. You invest in compounding upgrades. When several upgrades are affordable, prefer production or wall over repeating the same building type. An empty command is only correct when availableActions.upgrades is empty."""

STRATEGIC_PERSONALITY_PROMPT = """You are the STRATEGIC COMMANDER for a tick-based strategy city. You focus only on troops, combat, and scouting.

Your priorities, in order:
1. Survive. Field soldiers for defense; spies do not protect the city.
2. Rebuild. After defeats, train soldiers in batches when train is available.
3. See. Scout unknown neighbors with spy attacks when you have spies to spare and soldiers are not urgently needed.
4. Strike. Attack declared enemies with soldiers when canAttack is true — never attack declared allies.

Temperament: neutral but resolute. You do not sit idle when train or valid attacks are available. When you have no soldiers and can train them, rebuilding soldiers overrides scouting and idle. An empty command is only correct when availableActions has no train and no valid attack targets for your troops."""

SOCIAL_PERSONALITY_PROMPT = """You are the SOCIAL ENVOY for a tick-based strategy city. You focus only on diplomacy, messaging, and interpreting reports.

Your priorities, in order:
1. Reply. Answer every unread message in your inbox immediately.
2. Relate. Set ally or enemy relations when it serves the city's interests.
3. Inform. Use unread reports to inform your tone and diplomatic stance.
4. Reach out. Message any player when message cooldown allows and you have something to say.

Temperament: articulate and bold. You do not stay silent when canSendMessage is true. An empty command is only correct when both diplomacy cooldowns block you."""

SOCIAL_RULES_PROMPT = """You are the SOCIAL ENVOY. You decide messages and diplomacy ONLY.
You see socialSummary, replyTarget (when inbox is blocked), players, unread messages, and unread reports. You do not control buildings, troops, or attacks.

Output ONLY valid JSON (no markdown, no commentary outside the JSON).

Schema:
{{
  "schemaVersion": 1,
  "observedAtTick": <number>,
  "commands": [
    {{ "type": "message", "toPlayerId": "<uuid>", "subject": "<text>", "body": "<text>", "reason": "<1-2 sentences>" }},
    {{ "type": "ally", "toPlayerId": "<uuid>", "reason": "<1-2 sentences>" }},
    {{ "type": "enemy", "toPlayerId": "<uuid>", "reason": "<1-2 sentences>" }},
    {{ "type": "clear_relation", "toPlayerId": "<uuid>", "reason": "<1-2 sentences>" }}
  ]
}}

Example reply to unread (use replyTarget.toPlayerId and subject "Re: <their subject>"):
{{"schemaVersion":1,"observedAtTick":71521,"commands":[{{"type":"message","toPlayerId":"<uuid>","subject":"Re: Attack","body":"We received your message.","reason":"Replying to the war declaration."}}]}}

Rules (follow strictly):
0. Every command MUST include a non-empty "reason".
1. Return 0 or 1 commands. Return "commands": [] only when availableActions.canSendMessage and availableActions.canDeclareDiplomacy are both false.
2. If unreadMessages is non-empty and canSendMessage is true, you MUST return a message to replyTarget.toPlayerId (or mustReplyToPlayerId). Use subject "Re: <their subject>". Do not return [].
3. If unreadMessages is empty, canSendMessage is true, and socialSummary.unreadDefeatCount > 0, you MUST message a declared enemy from socialSummary.declaredEnemies or a human from socialSummary.humanPlayers. Do not return [].
4. If unreadMessages is empty and canSendMessage is true, you MUST message any player from players. Do not return [] when message cooldown is ready.
5. Only send a message when canSendMessage is true. Only declare ally/enemy/clear_relation when canDeclareDiplomacy is true.
6. When diplomacyOnly is true, your command MUST be type "message" to mustReplyToPlayerId unless canSendMessage is false.
7. Use any playerId from players when cooldowns allow.
8. Do not return [] because you messaged recently. Each reason must be specific to this tick."""

BUILDING_RULES_PROMPT = """You are the BUILDING MANAGER. You decide building upgrades ONLY.
You see your resources and your buildings. You do not control troops, attacks, or diplomacy.

Output ONLY valid JSON (no markdown, no commentary outside the JSON).

Schema:
{{
  "schemaVersion": 1,
  "observedAtTick": <number>,
  "commands": [
    {{ "type": "upgrade", "buildingType": "<building>", "reason": "<1-2 sentences>" }}
  ]
}}

Rules (follow strictly):
0. Every command MUST include a non-empty "reason".
1. Return 0 or 1 commands. Return "commands": [] only when availableActions.upgrades is empty.
2. You may ONLY upgrade a buildingType listed in availableActions.upgrades.
3. In the reason, name the fromLevel→toLevel from the matching upgrade entry.
4. Prefer upgrades that compound your economy (production, storage) or shore up survival (wall) given the buildings and resources you see.
5. Avoid upgrading the same buildingType as your most recent upgrade in recentDecisions unless no other upgrade is worthwhile. Repeating a different buildingType is fine. Do not return [] merely because you upgraded storage recently — pick another listed upgrade if upgrades is non-empty.
6. The reason must be specific to this tick (no copy-paste)."""

STRATEGIC_RULES_PROMPT = """You are the STRATEGIC COMMANDER. You decide troops and attacks ONLY.
You see your troops and a military threat summary. You do not control building upgrades or diplomacy.

Output ONLY valid JSON (no markdown, no commentary outside the JSON).

Schema:
{{
  "schemaVersion": 1,
  "observedAtTick": <number>,
  "commands": [
    {{ "type": "train", "troopType": "soldier"|"spy", "count": <1..maxCount>, "reason": "<1-2 sentences>" }},
    {{ "type": "attack", "targetCityId": "<uuid>", "troopType": "soldier", "count": 1, "reason": "<1-2 sentences>" }},
    {{ "type": "attack", "targetCityId": "<uuid>", "troopType": "spy", "count": 1, "reason": "<1-2 sentences>" }}
  ]
}}

Troop roles (game facts):
- Soldiers provide your attack power AND your defensive power when your city is raided.
- Spies only scout/gather intel; they have ZERO attack and ZERO defensive power. Hoarding spies does not protect you.

Rules (follow strictly):
0. Every command MUST include a non-empty "reason".
1. Return 0 or 1 commands. Return "commands": [] only when availableActions has no train and no valid attack targets for your troops.
2. If threatSummary.soldierCount is 0 (or troops has no soldier), availableActions.train includes soldier, you MUST return a train command for soldier with count equal to that entry's maxCount. Do not return [] in this situation.
3. For attack, "count" is always exactly 1. For train, "count" may be any whole number from 1 up to that troop's maxCount in availableActions.train — train a batch (not just 1) when rebuilding forces. troops[].count is current inventory, NOT the command count.
4. For train, use a troopType from availableActions.train with count between 1 and its maxCount.
5. For attack, use a targetCityId from availableActions.targets where canAttack is true (soldier) or canScout is true (spy).
6. Training a spy does NOT scout. Scouting requires attack with troopType spy, count 1.
7. Never attack a target whose relation is "ally".
8. Avoid repeating the same command type AND troopType/target as your most recent entry in recentDecisions (e.g. do not train spy again right after train spy). Training soldiers after training spies, or attacking after training, is encouraged. Do not return [] merely because you trained recently — choose a different valid action if one exists. Each reason must be specific to this tick."""

BUILDING_SYSTEM_PROMPT = f"{BUILDING_PERSONALITY_PROMPT}\n\n{BUILDING_RULES_PROMPT}"
STRATEGIC_SYSTEM_PROMPT = f"{STRATEGIC_PERSONALITY_PROMPT}\n\n{STRATEGIC_RULES_PROMPT}"
SOCIAL_SYSTEM_PROMPT = f"{SOCIAL_PERSONALITY_PROMPT}\n\n{SOCIAL_RULES_PROMPT}"


def _extract_json(text: str) -> str:
    text = text.strip()
    if text.startswith("```"):
        match = re.search(r"```(?:json)?\s*([\s\S]*?)```", text)
        if match:
            return match.group(1).strip()
    return text


def _log_agent_exchange(
    *,
    label: str,
    model: str,
    system: str,
    user: str,
    response: str,
) -> None:
    divider = "=" * 60
    print(divider, flush=True)
    print(f"=== start {label} ===  model={model}", flush=True)
    print("--- system ---", flush=True)
    print(system, flush=True)
    print("--- user ---", flush=True)
    print(user, flush=True)
    print("--- response ---", flush=True)
    print(response, flush=True)
    print(f"=== end {label} ===", flush=True)
    print(divider, flush=True)


def _summarize_command(command: dict) -> str:
    command_type = command.get("type", "?")
    if command_type == "upgrade":
        return f"upgrade {command.get('buildingType')}"
    if command_type == "train":
        return f"train {command.get('troopType')} x{command.get('count', 1)}"
    if command_type == "attack":
        return f"attack {command.get('targetCityId')} {command.get('troopType')}"
    return f"{command_type} {command.get('toPlayerId', '')}".strip()


class OllamaPlanner:
    def __init__(
        self,
        config: OllamaConfig,
        memory: DecisionMemory,
        agents: AgentsConfig,
    ) -> None:
        self.config = config
        self.memory = memory
        self.agents = agents

    def _complete(self, system: str, user: str) -> str:
        url = f"{self.config.base_url.rstrip('/')}/chat/completions"
        payload = {
            "model": self.config.model,
            "temperature": self.config.temperature,
            "format": "json",
            "messages": [
                {"role": "system", "content": system},
                {"role": "user", "content": user},
            ],
        }
        with httpx.Client(timeout=self.config.request_timeout_seconds) as http:
            response = http.post(url, json=payload)
            response.raise_for_status()
            data = response.json()
        return data["choices"][0]["message"]["content"]

    def _run_agent(
        self,
        *,
        label: str,
        system: str,
        context: dict,
        observed_tick: int,
        allowed_types: frozenset[str],
        user_suffix: str | None = None,
    ) -> list[dict]:
        suffix_block = f"{user_suffix}\n\n" if user_suffix else ""
        user_content = (
            f"{label.capitalize()} context (JSON):\n"
            f"{json.dumps(context, separators=(',', ':'))}\n\n"
            f"{suffix_block}"
            f"Set observedAtTick to {observed_tick}. "
            "Return 0 or 1 commands allowed by availableActions."
        )

        content = self._complete(system, user_content)

        if self.config.log_prompts:
            _log_agent_exchange(
                label=label,
                model=self.config.model,
                system=system,
                user=user_content,
                response=content,
            )

        try:
            raw = json.loads(_extract_json(content))
        except json.JSONDecodeError:
            print(f"{label}: invalid JSON response, no command", flush=True)
            return []

        commands: list[dict] = []
        for command in raw.get("commands") or []:
            if not isinstance(command, dict):
                continue
            command_type = str(command.get("type", "")).lower()
            if command_type not in allowed_types:
                print(
                    f"{label}: dropped out-of-scope command type '{command_type}'",
                    flush=True,
                )
                continue
            commands.append(command)

        commands = commands[:1]
        if commands:
            print(f"{label} -> {_summarize_command(commands[0])}", flush=True)
        else:
            print(f"{label} -> no command", flush=True)
        return commands

    def plan(self, client: GameClient) -> CommandPlan:
        city = resolve_first_city(client)
        city_id = str(city["id"])
        auth_player_id = str(city.get("playerId", ""))
        buildings = client.get_building_catalog()
        set_building_types(entry["type"] for entry in buildings)

        possible = client.get_possible_actions(city_id)
        reports = client.get_reports()
        players = client.get_diplomacy_players()
        relations = client.get_diplomacy_relations()
        messages = client.get_diplomacy_messages()
        possible_for_validation = enrich_possible_for_validation(possible, players)

        observed_tick = int(possible.get("currentTick", 0))
        unread = has_unread_message(possible)
        social_only = unread and self.agents.social_agent_on

        commands: list[dict] = []

        if social_only:
            print(
                "=== building/strategy agents skipped (unread message, social only) ===",
                flush=True,
            )
        else:
            if unread:
                print("=== building agent skipped (unread message) ===", flush=True)
            elif self.agents.building_agent_on:
                building_recent = format_recent_decisions(
                    self.memory.get_recent_by_types(
                        city_id, BUILDING_COMMAND_TYPES, limit=2
                    )
                )
                building_context = build_building_context(
                    city, possible, building_recent
                )
                commands.extend(
                    self._run_agent(
                        label="building agent",
                        system=BUILDING_SYSTEM_PROMPT,
                        context=building_context,
                        observed_tick=observed_tick,
                        allowed_types=BUILDING_COMMAND_TYPES,
                    )
                )
            else:
                print("=== building agent disabled ===", flush=True)

            if self.agents.strategy_agent_on:
                strategic_recent = format_recent_decisions(
                    self.memory.get_recent_by_types(
                        city_id, STRATEGIC_COMMAND_TYPES, limit=2
                    )
                )
                strategic_context = build_strategic_context(
                    possible, reports, strategic_recent
                )
                strategic_actions = strategic_context["availableActions"]
                strategic_alert = build_strategic_alert(
                    strategic_context["threatSummary"],
                    strategic_actions,
                )
                commands.extend(
                    self._run_agent(
                        label="strategy agent",
                        system=STRATEGIC_SYSTEM_PROMPT,
                        context=strategic_context,
                        observed_tick=observed_tick,
                        allowed_types=STRATEGIC_COMMAND_TYPES,
                        user_suffix=strategic_alert,
                    )
                )
            else:
                print("=== strategy agent disabled ===", flush=True)

        if self.agents.social_agent_on:
            social_recent = format_recent_decisions(
                self.memory.get_recent_by_types(
                    city_id, SOCIAL_COMMAND_TYPES, limit=2
                )
            )
            social_context = build_social_context(
                possible,
                players,
                relations,
                messages,
                reports,
                auth_player_id,
                social_recent,
            )
            social_alert = build_social_alert(
                social_context["socialSummary"],
                social_context["availableActions"],
                social_context.get("replyTarget"),
            )
            commands.extend(
                self._run_agent(
                    label="social agent",
                    system=SOCIAL_SYSTEM_PROMPT,
                    context=social_context,
                    observed_tick=observed_tick,
                    allowed_types=SOCIAL_COMMAND_TYPES,
                    user_suffix=social_alert,
                )
            )
        else:
            print("=== social agent disabled ===", flush=True)

        raw_plan = {
            "schemaVersion": 1,
            "observedAtTick": observed_tick,
            "commands": commands,
        }
        plan, repairs = parse_plan_from_llm(raw_plan, possible_for_validation)
        if repairs:
            print(f"plan repairs: {'; '.join(repairs)}", flush=True)
        plan, dropped = filter_plan_to_possible_actions(
            plan, possible_for_validation
        )
        if dropped:
            print(
                f"dropped {len(dropped)} command(s) not in possible actions: "
                + ", ".join(dropped),
                flush=True,
            )
        return plan
