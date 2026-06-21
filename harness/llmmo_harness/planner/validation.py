from llmmo_harness.schema import (
    AllyCommand,
    AttackCommand,
    ClearRelationCommand,
    CommandPlan,
    EnemyCommand,
    MessageCommand,
    TrainCommand,
    UpgradeCommand,
)


def _command_summary(
    command: UpgradeCommand
    | TrainCommand
    | AttackCommand
    | MessageCommand
    | AllyCommand
    | EnemyCommand
    | ClearRelationCommand,
) -> str:
    if isinstance(command, UpgradeCommand):
        return f"upgrade {command.buildingType}"
    if isinstance(command, TrainCommand):
        return f"train {command.troopType} x{command.count}"
    if isinstance(command, AttackCommand):
        return f"attack {command.targetCityId} {command.troopType} x{command.count}"
    if isinstance(command, MessageCommand):
        return f"message {command.toPlayerId}"
    if isinstance(command, AllyCommand):
        return f"ally {command.toPlayerId}"
    if isinstance(command, EnemyCommand):
        return f"enemy {command.toPlayerId}"
    return f"clear_relation {command.toPlayerId}"


def _relation_player_ids(possible: dict) -> set[str]:
    diplomacy = possible.get("diplomacy") or {}
    relations = diplomacy.get("relations") or diplomacy.get("players") or []
    return {
        str(relation.get("playerId", "")).lower()
        for relation in relations
        if relation.get("playerId")
    }


def _target_player_ids(possible: dict) -> set[str]:
    return {
        str(target.get("targetPlayerId", "")).lower()
        for target in possible.get("targets", [])
        if target.get("targetPlayerId")
    }


def _diplomacy_recipient_ids(possible: dict) -> set[str]:
    recipient_ids = _target_player_ids(possible) | _relation_player_ids(possible)
    latest = (possible.get("diplomacy") or {}).get("latestUnreadMessage")
    if isinstance(latest, dict) and latest.get("fromPlayerId"):
        recipient_ids.add(str(latest["fromPlayerId"]).lower())
    return recipient_ids


def command_allowed(
    command: UpgradeCommand
    | TrainCommand
    | AttackCommand
    | MessageCommand
    | AllyCommand
    | EnemyCommand
    | ClearRelationCommand,
    possible: dict,
) -> bool:
    if isinstance(command, MessageCommand):
        diplomacy = possible.get("diplomacy") or {}
        if not diplomacy.get("canSendMessage", False):
            return False
        return command.toPlayerId.lower() in _diplomacy_recipient_ids(possible)

    if isinstance(command, (AllyCommand, EnemyCommand, ClearRelationCommand)):
        diplomacy = possible.get("diplomacy") or {}
        if not diplomacy.get("canDeclareDiplomacy", False):
            return False
        return command.toPlayerId.lower() in _diplomacy_recipient_ids(possible)

    if isinstance(command, UpgradeCommand):
        allowed = {
            entry["buildingType"].lower()
            for entry in possible.get("upgrades", [])
        }
        return command.buildingType.lower() in allowed

    if isinstance(command, TrainCommand):
        allowed = {
            (entry["troopType"].lower(), int(entry["count"]))
            for entry in possible.get("train", [])
        }
        return (command.troopType.lower(), command.count) in allowed

    target_id = command.targetCityId.lower()
    for entry in possible.get("targets", []):
        if str(entry.get("targetCityId", "")).lower() != target_id:
            continue
        if command.troopType.lower() == "soldier" and entry.get("canAttack"):
            return command.count == 1
        if command.troopType.lower() == "spy" and entry.get("canScout"):
            return command.count == 1
    return False


def filter_plan_to_possible_actions(
    plan: CommandPlan,
    possible: dict,
) -> tuple[CommandPlan, list[str]]:
    kept: list[
        UpgradeCommand
        | TrainCommand
        | AttackCommand
        | MessageCommand
        | AllyCommand
        | EnemyCommand
        | ClearRelationCommand
    ] = []
    dropped: list[str] = []

    for command in plan.commands:
        if command_allowed(command, possible):
            kept.append(command)
        else:
            dropped.append(_command_summary(command))

    if len(kept) == len(plan.commands):
        return plan, dropped

    return (
        CommandPlan(
            schemaVersion=plan.schemaVersion,
            observedAtTick=plan.observedAtTick,
            commands=kept,
        ),
        dropped,
    )
