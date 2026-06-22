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


def has_unread_message(possible: dict) -> bool:
    latest = (possible.get("diplomacy") or {}).get("latestUnreadMessage")
    return isinstance(latest, dict) and bool(latest.get("id"))


def compact_buildings(buildings: list[dict]) -> list[dict]:
    compact: list[dict] = []
    for building in buildings:
        entry: dict = {
            "type": building.get("type"),
            "level": building.get("level"),
            "description": building.get("description"),
            "currentEffect": building.get("currentEffect"),
        }
        next_effect = building.get("nextLevelEffect")
        if next_effect:
            entry["nextLevelEffect"] = next_effect
        production = building.get("productionPerTick")
        if production is not None:
            entry["productionPerTick"] = production
            resource = building.get("productionResource")
            if resource:
                entry["productionResource"] = resource
        upgrade_cost = building.get("nextUpgradeCost")
        if upgrade_cost:
            entry["nextUpgradeCost"] = upgrade_cost
        compact.append(entry)
    return compact


def compact_diplomacy(diplomacy: dict) -> dict:
    relations = diplomacy.get("relations") or diplomacy.get("players") or []
    latest_unread = diplomacy.get("latestUnreadMessage")

    compact: dict = {
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

    if isinstance(latest_unread, dict):
        compact["latestUnreadMessage"] = {
            "id": latest_unread.get("id"),
            "fromPlayerId": latest_unread.get("fromPlayerId"),
            "fromPlayerName": latest_unread.get("fromPlayerName"),
            "subject": latest_unread.get("subject"),
            "body": latest_unread.get("body"),
            "sentAtTick": latest_unread.get("sentAtTick"),
        }

    return compact


def compact_targets(targets: list[dict]) -> list[dict]:
    compact_targets_list: list[dict] = []
    for target in targets:
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
        compact_targets_list.append(entry)
    return compact_targets_list


def build_available_actions(possible: dict) -> dict:
    diplomacy = possible.get("diplomacy") or {}

    if has_unread_message(possible):
        latest = diplomacy.get("latestUnreadMessage") or {}
        return {
            "diplomacyOnly": True,
            "canSendMessage": diplomacy.get("canSendMessage", False),
            "canDeclareDiplomacy": diplomacy.get("canDeclareDiplomacy", False),
            "mustReplyToPlayerId": latest.get("fromPlayerId"),
            "mustReplyToPlayerName": latest.get("fromPlayerName"),
        }

    return {
        "upgrades": [
            {
                "buildingType": upgrade["buildingType"],
                "fromLevel": upgrade["fromLevel"],
                "toLevel": upgrade["toLevel"],
            }
            for upgrade in possible.get("upgrades", [])
        ],
        "train": [
            {
                "troopType": option["troopType"],
                "maxCount": option.get("maxCount", option.get("count", 1)),
            }
            for option in possible.get("train", [])
        ],
        "targets": compact_targets(possible.get("targets", [])),
    }


BUILDING_COMMAND_TYPES = frozenset({"upgrade"})
STRATEGIC_COMMAND_TYPES = frozenset(
    {"train", "attack", "message", "ally", "enemy", "clear_relation"}
)


def unread_reports(reports: list[dict]) -> list[dict]:
    return [report for report in reports if report.get("readAt") is None]


def build_building_context(
    city: dict,
    possible: dict,
    recent: list[dict],
) -> dict:
    """Context for the Building Manager agent: resources + buildings only."""
    return {
        "cityState": {
            "currentTick": possible.get("currentTick"),
            "resources": possible.get("resources"),
            "foodProductionPerTick": possible.get("foodProductionPerTick"),
            "foodUpkeepPerTick": possible.get("foodUpkeepPerTick"),
            "buildings": compact_buildings(city.get("buildings", [])),
        },
        "availableActions": {
            "upgrades": [
                {
                    "buildingType": upgrade["buildingType"],
                    "fromLevel": upgrade["fromLevel"],
                    "toLevel": upgrade["toLevel"],
                }
                for upgrade in possible.get("upgrades", [])
            ],
        },
        "recentDecisions": recent,
    }


def build_strategic_actions(possible: dict) -> dict:
    diplomacy = possible.get("diplomacy") or {}

    if has_unread_message(possible):
        latest = diplomacy.get("latestUnreadMessage") or {}
        return {
            "diplomacyOnly": True,
            "canSendMessage": diplomacy.get("canSendMessage", False),
            "canDeclareDiplomacy": diplomacy.get("canDeclareDiplomacy", False),
            "mustReplyToPlayerId": latest.get("fromPlayerId"),
            "mustReplyToPlayerName": latest.get("fromPlayerName"),
        }

    return {
        "train": [
            {
                "troopType": option["troopType"],
                "maxCount": option.get("maxCount", option.get("count", 1)),
            }
            for option in possible.get("train", [])
        ],
        "targets": compact_targets(possible.get("targets", [])),
    }


def build_strategic_context(
    possible: dict,
    reports: list[dict],
    recent: list[dict],
) -> dict:
    """Context for the Strategic agent: troops, diplomacy, reports — no economy."""
    return {
        "currentTick": possible.get("currentTick"),
        "troops": possible.get("troops", []),
        "diplomacy": compact_diplomacy(possible.get("diplomacy") or {}),
        "unreadReports": unread_reports(reports),
        "availableActions": build_strategic_actions(possible),
        "recentDecisions": recent,
    }


def build_planner_context(
    city: dict,
    possible: dict,
    reports: list[dict],
    recent: list[dict],
) -> dict:
    return {
        "cityState": {
            "currentTick": possible.get("currentTick"),
            "resources": possible.get("resources"),
            "foodProductionPerTick": possible.get("foodProductionPerTick"),
            "foodUpkeepPerTick": possible.get("foodUpkeepPerTick"),
            "troops": possible.get("troops", []),
            "buildings": compact_buildings(city.get("buildings", [])),
        },
        "diplomacy": compact_diplomacy(possible.get("diplomacy") or {}),
        "unreadReports": unread_reports(reports),
        "availableActions": build_available_actions(possible),
        "recentDecisions": recent,
    }


def compact_possible_actions(actions: dict) -> dict:
    """Legacy compact shape — prefer build_planner_context for prompts."""
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
            {
                "troopType": option["troopType"],
                "maxCount": option.get("maxCount", option.get("count", 1)),
            }
            for option in actions.get("train", [])
        ],
        "targets": compact_targets(actions.get("targets", [])),
        "diplomacy": compact_diplomacy(actions.get("diplomacy") or {}),
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


def resolve_first_city(client: GameClient) -> dict:
    cities = client.get_cities_me()
    if not cities:
        raise ValueError("No cities found for this agent.")
    return cities[0]


def resolve_first_city_id(client: GameClient) -> str:
    city = resolve_first_city(client)
    city_id = city.get("id")
    if not city_id:
        raise ValueError("City record missing id.")
    return str(city_id)
