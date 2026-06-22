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
from llmmo_harness.state import has_unread_message


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


def _unread_sender_id(possible: dict) -> str | None:
    latest = (possible.get("diplomacy") or {}).get("latestUnreadMessage")
    if isinstance(latest, dict) and latest.get("fromPlayerId"):
        return str(latest["fromPlayerId"]).lower()
    return None


def _diplomacy_recipient_ids(possible: dict) -> set[str]:
    # When there is an unread message, the only valid recipient is the sender,
    # so a reply actually clears the inbox and unblocks normal actions.
    sender_id = _unread_sender_id(possible)
    if sender_id is not None:
        return {sender_id}

    all_player_ids = possible.get("allPlayerIds")
    if all_player_ids:
        return {str(player_id).lower() for player_id in all_player_ids}

    return _target_player_ids(possible) | _relation_player_ids(possible)


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
    if has_unread_message(possible):
        if not isinstance(
            command,
            (MessageCommand, AllyCommand, EnemyCommand, ClearRelationCommand),
        ):
            return False

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
        for entry in possible.get("train", []):
            if str(entry.get("troopType", "")).lower() == command.troopType.lower():
                max_count = max(1, int(entry.get("maxCount", entry.get("count", 1))))
                return 1 <= command.count <= max_count
        return False

    target_id = command.targetCityId.lower()
    for entry in possible.get("targets", []):
        if str(entry.get("targetCityId", "")).lower() != target_id:
            continue
        if entry.get("relation") == "ally":
            return False
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
