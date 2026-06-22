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
STRATEGIC_COMMAND_TYPES = frozenset({"train", "attack"})
SOCIAL_COMMAND_TYPES = frozenset({"message", "ally", "enemy", "clear_relation"})


def unread_reports(reports: list[dict]) -> list[dict]:
    return [report for report in reports if report.get("readAt") is None]


def troop_count(troops: list[dict], troop_type: str) -> int:
    return sum(
        int(entry.get("count", 0))
        for entry in troops
        if entry.get("type") == troop_type
    )


def build_threat_summary(
    reports: list[dict],
    troops: list[dict],
    diplomacy: dict,
) -> dict:
    unread = unread_reports(reports)
    defeat_reports: list[dict] = []
    for report in unread:
        if report.get("type") != "attack":
            continue
        payload = report.get("payload") or {}
        if payload.get("perspective") == "defender" and payload.get("outcome") == "defeat":
            defeat_reports.append(report)

    relations = diplomacy.get("relations") or diplomacy.get("players") or []
    declared_enemies = [
        relation["name"]
        for relation in relations
        if relation.get("relation") == "enemy" and relation.get("name")
    ]

    summary: dict = {
        "unreadReportCount": len(unread),
        "unreadDefeatCount": len(defeat_reports),
        "soldierCount": troop_count(troops, "soldier"),
        "spyCount": troop_count(troops, "spy"),
    }
    if declared_enemies:
        summary["declaredEnemies"] = declared_enemies

    if defeat_reports:
        latest = max(defeat_reports, key=lambda report: report.get("createdAt") or "")
        summary["mostRecentDefeat"] = {
            "outcome": "defeat",
            "sourceCityId": latest.get("sourceCityId"),
            "createdAt": latest.get("createdAt"),
        }

    return summary


def build_strategic_alert(threat_summary: dict, actions: dict) -> str | None:
    if actions.get("diplomacyOnly"):
        return None
    train = actions.get("train") or []
    can_train_soldier = any(option.get("troopType") == "soldier" for option in train)
    if threat_summary.get("soldierCount", 0) != 0 or not can_train_soldier:
        return None
    defeats = int(threat_summary.get("unreadDefeatCount", 0))
    if defeats > 0:
        return (
            f"Alert: 0 soldiers, {defeats} unread defeat(s) — "
            "train soldiers if train is available."
        )
    return "Alert: 0 soldiers — train soldiers if train is available."


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


def merge_players_with_relations(
    players: list[dict],
    relations: list[dict],
) -> list[dict]:
    relation_by_id: dict[str, str] = {}
    for relation in relations:
        player_id = (
            relation.get("otherPlayerId")
            or relation.get("playerId")
            or relation.get("toPlayerId")
        )
        relation_value = relation.get("relation")
        if player_id and relation_value:
            relation_by_id[str(player_id).lower()] = relation_value

    merged: list[dict] = []
    for player in players:
        player_id = player.get("id") or player.get("playerId")
        relation = relation_by_id.get(str(player_id).lower()) if player_id else None
        merged.append(
            {
                "playerId": player_id,
                "name": player.get("name"),
                "playerType": player.get("playerType"),
                "relation": relation,
            }
        )
    return merged


def compact_social_report(report: dict) -> dict:
    payload = report.get("payload") or {}
    entry: dict = {
        "id": report.get("id"),
        "type": report.get("type"),
        "createdAt": report.get("createdAt"),
    }
    if report.get("type") == "attack":
        entry["outcome"] = payload.get("outcome")
        entry["perspective"] = payload.get("perspective")
        if report.get("sourceCityId"):
            entry["sourceCityId"] = report.get("sourceCityId")
        attacker = payload.get("attacker") or {}
        defender = payload.get("defender") or {}
        if payload.get("perspective") == "defender":
            entry["attackerPower"] = attacker.get("totalPower")
            entry["defenderPower"] = defender.get("totalPower")
            entry["wallBonus"] = defender.get("wallBonus")
    elif report.get("type") == "scout":
        entry["outcome"] = payload.get("outcome")
    return entry


def compact_unread_messages(messages: list[dict], auth_player_id: str) -> list[dict]:
    auth = auth_player_id.lower()
    compact: list[dict] = []
    for message in messages:
        if message.get("readAt") is not None:
            continue
        to_player_id = str(message.get("toPlayerId", "")).lower()
        if to_player_id != auth:
            continue
        compact.append(
            {
                "id": message.get("id"),
                "fromPlayerId": message.get("fromPlayerId"),
                "fromPlayerName": message.get("fromPlayerName"),
                "subject": message.get("subject"),
                "body": message.get("body"),
                "sentAtTick": message.get("sentAtTick"),
            }
        )
    return compact


def compact_social_reports(reports: list[dict]) -> list[dict]:
    return [compact_social_report(report) for report in unread_reports(reports)]


def build_social_actions(possible: dict) -> dict:
    diplomacy = possible.get("diplomacy") or {}
    latest = diplomacy.get("latestUnreadMessage") or {}
    actions: dict = {
        "canSendMessage": diplomacy.get("canSendMessage", False),
        "canDeclareDiplomacy": diplomacy.get("canDeclareDiplomacy", False),
    }
    if has_unread_message(possible):
        actions["diplomacyOnly"] = True
        actions["mustReplyToPlayerId"] = latest.get("fromPlayerId")
        actions["mustReplyToPlayerName"] = latest.get("fromPlayerName")
    return actions


def _social_defeat_count(reports: list[dict]) -> int:
    return sum(
        1
        for report in reports
        if report.get("type") == "attack"
        and report.get("perspective") == "defender"
        and report.get("outcome") == "defeat"
    )


def build_social_summary(
    merged_players: list[dict],
    unread_messages: list[dict],
    unread_reports: list[dict],
) -> dict:
    summary: dict = {
        "unreadMessageCount": len(unread_messages),
        "unreadDefeatCount": _social_defeat_count(unread_reports),
    }
    enemies = [
        player["name"]
        for player in merged_players
        if player.get("relation") == "enemy" and player.get("name")
    ]
    humans = [
        player["name"]
        for player in merged_players
        if player.get("playerType") == "human" and player.get("name")
    ]
    if enemies:
        summary["declaredEnemies"] = enemies
    if humans:
        summary["humanPlayers"] = humans
    return summary


def build_reply_target(
    unread_messages: list[dict],
    actions: dict,
) -> dict | None:
    if not unread_messages:
        return None
    first = unread_messages[0]
    return {
        "toPlayerId": actions.get("mustReplyToPlayerId") or first.get("fromPlayerId"),
        "toPlayerName": actions.get("mustReplyToPlayerName")
        or first.get("fromPlayerName"),
        "subject": first.get("subject"),
        "body": first.get("body"),
    }


def slim_social_players(
    merged_players: list[dict],
    actions: dict,
    unread_messages: list[dict],
) -> list[dict]:
    if not actions.get("diplomacyOnly"):
        return merged_players

    keep_ids: set[str] = set()
    reply_id = actions.get("mustReplyToPlayerId")
    if reply_id:
        keep_ids.add(str(reply_id).lower())
    for message in unread_messages:
        from_player_id = message.get("fromPlayerId")
        if from_player_id:
            keep_ids.add(str(from_player_id).lower())

    slimmed: list[dict] = []
    seen: set[str] = set()
    for player in merged_players:
        player_id = str(player.get("playerId", "")).lower()
        if not player_id or player_id in seen:
            continue
        include = (
            player_id in keep_ids
            or player.get("relation") == "enemy"
            or player.get("playerType") == "human"
        )
        if include:
            seen.add(player_id)
            slimmed.append(player)
    return slimmed if slimmed else merged_players[:5]


def build_social_alert(
    social_summary: dict,
    actions: dict,
    reply_target: dict | None,
) -> str | None:
    if reply_target and actions.get("canSendMessage"):
        name = reply_target.get("toPlayerName") or "sender"
        player_id = reply_target.get("toPlayerId")
        return (
            f"Alert: Reply now — send a message to toPlayerId={player_id} ({name})."
        )

    if actions.get("canSendMessage"):
        defeats = int(social_summary.get("unreadDefeatCount", 0))
        enemies = social_summary.get("declaredEnemies") or []
        humans = social_summary.get("humanPlayers") or []
        if defeats > 0 and enemies:
            return (
                f"Alert: {defeats} unread defeat(s) — message declared enemy "
                f"{enemies[0]}."
            )
        if defeats > 0 and humans:
            return (
                f"Alert: {defeats} unread defeat(s) — message a human player "
                f"from players."
            )
        return "Alert: Message cooldown ready — message a player from players."

    if actions.get("canDeclareDiplomacy"):
        return "Alert: Diplomacy cooldown ready — set a relation if you have a stance."

    return None


def build_social_context(
    possible: dict,
    players: list[dict],
    relations: list[dict],
    messages: list[dict],
    reports: list[dict],
    auth_player_id: str,
    recent: list[dict],
) -> dict:
    """Context for the Social agent: summary, reply target, slim players when inbox blocked."""
    merged_players = merge_players_with_relations(players, relations)
    unread_messages = compact_unread_messages(messages, auth_player_id)
    unread_reports = compact_social_reports(reports)
    actions = build_social_actions(possible)
    reply_target = build_reply_target(unread_messages, actions)

    context: dict = {
        "currentTick": possible.get("currentTick"),
        "socialSummary": build_social_summary(
            merged_players, unread_messages, unread_reports
        ),
        "players": slim_social_players(merged_players, actions, unread_messages),
        "unreadMessages": unread_messages,
        "unreadReports": unread_reports,
        "availableActions": actions,
        "recentDecisions": recent,
    }
    if reply_target is not None:
        context["replyTarget"] = reply_target
    return context


def enrich_possible_for_validation(
    possible: dict,
    players: list[dict],
) -> dict:
    return {
        **possible,
        "allPlayerIds": [
            str(player.get("id") or player.get("playerId"))
            for player in players
            if player.get("id") or player.get("playerId")
        ],
    }


def build_strategic_context(
    possible: dict,
    reports: list[dict],
    recent: list[dict],
) -> dict:
    """Context for the Strategic agent: troops and military threat summary — no diplomacy."""
    troops = possible.get("troops", [])
    diplomacy = possible.get("diplomacy") or {}
    return {
        "currentTick": possible.get("currentTick"),
        "troops": troops,
        "threatSummary": build_threat_summary(reports, troops, diplomacy),
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
