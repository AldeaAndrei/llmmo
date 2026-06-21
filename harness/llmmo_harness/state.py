import json

from llmmo_harness.client import GameClient


def format_recent_decisions(records: list) -> list[dict]:
    return [
        {
            "tick": record.tick,
            "action": record.action,
            "reason": record.reason,
        }
        for record in records
    ]


def _troop_count(possible: dict, troop_type: str) -> int:
    for troop in possible.get("troops") or []:
        if troop.get("type") == troop_type:
            return int(troop.get("count", 0))
    return 0


def _spy_count(possible: dict) -> int:
    return _troop_count(possible, "spy")


def _soldier_count(possible: dict) -> int:
    return _troop_count(possible, "soldier")


def _recent_train_spy_count(recent: list[dict]) -> int:
    count = 0
    for entry in recent:
        action = entry.get("action") or {}
        if action.get("type") == "train" and action.get("troopType") == "spy":
            count += 1
    return count


def _recent_scout_count(recent: list[dict]) -> int:
    count = 0
    for entry in recent:
        action = entry.get("action") or {}
        if action.get("type") == "attack" and action.get("troopType") == "spy":
            count += 1
    return count


def build_planner_hints(possible: dict, recent: list[dict]) -> list[str]:
    """Short tactical hints derived from game state (not available to the model otherwise)."""
    hints: list[str] = [
        "Command count is always exactly 1 for train and attack — "
        "troops[].count is your inventory, NOT the command count.",
    ]

    targets = possible.get("targets") or []
    soldier_count = _soldier_count(possible)
    enemy_attack_targets = [
        target
        for target in targets
        if target.get("relation") == "enemy" and target.get("canAttack")
    ]

    if soldier_count >= 1 and enemy_attack_targets:
        nearest = min(enemy_attack_targets, key=lambda target: target.get("travelTicks", 999))
        hints.append(
            "Priority this plan: attack declared enemy "
            f"{nearest['targetName']} with a soldier: "
            + json.dumps(
                {
                    "type": "attack",
                    "targetCityId": nearest["targetCityId"],
                    "troopType": "soldier",
                    "count": 1,
                    "reason": (
                        f"Strike enemy {nearest['targetName']} "
                        f"(travelTicks={nearest['travelTicks']}) while soldiers are ready."
                    ),
                },
                separators=(",", ":"),
            )
        )

    spy_count = _spy_count(possible)
    scout_targets = [target for target in targets if target.get("canScout")]

    if scout_targets:
        nearest = min(scout_targets, key=lambda target: target.get("travelTicks", 999))
        target_line = (
            f"{nearest['targetName']} (targetCityId={nearest['targetCityId']}, "
            f"travelTicks={nearest['travelTicks']})"
        )

        hints.append(
            "Scouting is NOT training. To scout, use "
            + json.dumps(
                {
                    "type": "attack",
                    "targetCityId": nearest["targetCityId"],
                    "troopType": "spy",
                    "count": 1,
                    "reason": f"Scout {nearest['targetName']} before committing troops.",
                },
                separators=(",", ":"),
            )
            + " on a target where canScout is true."
        )

        if spy_count >= 1:
            hints.append(f"You currently have {spy_count} spy/spies available to send.")

        recent_train_spy = _recent_train_spy_count(recent)
        recent_scouts = _recent_scout_count(recent)

        if spy_count >= 2 or (
            spy_count >= 1 and recent_train_spy >= 1 and recent_scouts == 0
        ):
            if not enemy_attack_targets:
                hints.append(
                    f"Priority this plan: scout {target_line} instead of training another spy."
                )

        if recent_train_spy >= 2 and recent_scouts == 0:
            hints.append(
                "Do not choose train spy again until you have scouted at least one city."
            )

    return hints


def compact_possible_actions(actions: dict) -> dict:
    """Trim possible-actions payload for the LLM prompt."""
    diplomacy = actions.get("diplomacy") or {}
    relations = diplomacy.get("relations") or diplomacy.get("players") or []
    latest_unread = diplomacy.get("latestUnreadMessage")

    compact_diplomacy = {
        "relations": [
            {
                "playerId": relation.get("playerId"),
                "name": relation.get("name"),
                "playerType": relation.get("playerType"),
                "relation": relation.get("relation"),
            }
            for relation in relations
            if relation.get("relation") in ("ally", "enemy")
        ],
        "canSendMessage": diplomacy.get("canSendMessage", False),
        "canDeclareDiplomacy": diplomacy.get("canDeclareDiplomacy", False),
    }

    if latest_unread:
        compact_diplomacy["latestUnreadMessage"] = {
            "id": latest_unread.get("id"),
            "fromPlayerId": latest_unread.get("fromPlayerId"),
            "fromPlayerName": latest_unread.get("fromPlayerName"),
            "subject": latest_unread.get("subject"),
            "body": latest_unread.get("body"),
            "sentAtTick": latest_unread.get("sentAtTick"),
        }

    compact_targets = []
    for target in actions.get("targets", []):
        entry = {
            "targetCityId": target["targetCityId"],
            "targetPlayerId": target["targetPlayerId"],
            "targetName": target["targetName"],
            "distance": target["distance"],
            "travelTicks": target["travelTicks"],
            "canAttack": target.get("canAttack", False),
            "canScout": target.get("canScout", False),
        }
        relation = target.get("relation")
        if relation in ("ally", "enemy"):
            entry["relation"] = relation
        compact_targets.append(entry)

    return {
        "currentTick": actions.get("currentTick"),
        "resources": actions.get("resources"),
        "foodProductionPerTick": actions.get("foodProductionPerTick"),
        "foodUpkeepPerTick": actions.get("foodUpkeepPerTick"),
        "troops": actions.get("troops", []),
        "upgrades": [
            {
                "buildingType": upgrade["buildingType"],
                "fromLevel": upgrade["fromLevel"],
                "toLevel": upgrade["toLevel"],
            }
            for upgrade in actions.get("upgrades", [])
        ],
        "train": [
            {"troopType": option["troopType"], "count": option["count"]}
            for option in actions.get("train", [])
        ],
        "targets": compact_targets,
        "diplomacy": compact_diplomacy,
    }


def compact_diplomacy_overview(overview: dict) -> dict:
    """Trim diplomacy overview for the LLM prompt."""
    latest = overview.get("latestUnreadMessage")
    compact_message = None
    if isinstance(latest, dict):
        compact_message = {
            "id": latest.get("id"),
            "fromPlayerId": latest.get("fromPlayerId"),
            "fromPlayerName": latest.get("fromPlayerName"),
            "subject": latest.get("subject"),
            "body": latest.get("body"),
            "sentAtTick": latest.get("sentAtTick"),
        }

    return {
        "cooldowns": overview.get("cooldowns"),
        "relations": [
            {
                "playerId": relation.get("playerId"),
                "name": relation.get("name"),
                "playerType": relation.get("playerType"),
                "relation": relation.get("relation"),
            }
            for relation in overview.get("relations", [])
            if relation.get("relation") in ("ally", "enemy")
        ],
        "latestUnreadMessage": compact_message,
    }


def compact_planner_state(world: dict, city: dict, troop_catalog: list[dict]) -> dict:
    """Minimal state for the LLM planner — full API payloads are very token-heavy."""
    resources = city.get("resources") or {}
    resource_amounts = {}
    for key in ("gold", "stone", "wood", "food"):
        view = resources.get(key)
        if isinstance(view, dict):
            resource_amounts[key] = view.get("amount", 0)
        else:
            resource_amounts[key] = city.get(key, 0)

    return {
        "currentTick": world.get("currentTick"),
        "cityName": city.get("name"),
        "resources": resource_amounts,
        "buildings": [
            {
                "type": building["type"],
                "level": building.get("level"),
                "nextUpgradeCost": building.get("nextUpgradeCost"),
                "canTrainTroops": building.get("canTrainTroops"),
            }
            for building in city.get("buildings", [])
        ],
        "troops": [
            {"type": troop["type"], "quantity": troop.get("quantity", 0)}
            for troop in city.get("troops", [])
        ],
        "trainOptions": [
            {
                "type": troop["type"],
                "trainAtBuilding": troop.get("trainAtBuilding"),
                "trainCostPerUnit": troop.get("trainCostPerUnit"),
            }
            for troop in troop_catalog
        ],
    }


def resolve_first_city(client) -> dict:
    cities = client.get_cities_me()
    if not cities:
        raise ValueError("No cities found for this agent.")
    return cities[0]


def resolve_first_city_id(client) -> str:
    city = resolve_first_city(client)
    city_id = city.get("id")
    if not city_id:
        raise ValueError("City record missing id.")
    return str(city_id)
