from llmmo_harness.schema import AttackCommand, CommandPlan, TrainCommand, UpgradeCommand


def _command_summary(command: UpgradeCommand | TrainCommand | AttackCommand) -> str:
    if isinstance(command, UpgradeCommand):
        return f"upgrade {command.buildingType}"
    if isinstance(command, TrainCommand):
        return f"train {command.troopType} x{command.count}"
    return f"attack {command.targetCityId} {command.troopType} x{command.count}"


def command_allowed(
    command: UpgradeCommand | TrainCommand | AttackCommand,
    possible: dict,
) -> bool:
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
    for entry in possible.get("attacks", []):
        if str(entry.get("targetCityId", "")).lower() != target_id:
            continue
        for troop in entry.get("troops", []):
            if (
                str(troop.get("type", "")).lower() == command.troopType.lower()
                and int(troop.get("count", 0)) == command.count
            ):
                return True
    return False


def filter_plan_to_possible_actions(
    plan: CommandPlan,
    possible: dict,
) -> tuple[CommandPlan, list[str]]:
    kept: list[UpgradeCommand | TrainCommand | AttackCommand] = []
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
