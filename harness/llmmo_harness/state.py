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


def compact_possible_actions(actions: dict) -> dict:
    """Trim possible-actions payload for the LLM prompt."""
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
        "attacks": [
            {
                "targetCityId": attack["targetCityId"],
                "targetName": attack["targetName"],
                "targetX": attack["targetX"],
                "targetY": attack["targetY"],
                "troops": attack.get("troops", []),
            }
            for attack in actions.get("attacks", [])
        ],
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
