from pydantic import TypeAdapter, ValidationError

from llmmo_harness.schema import Command, CommandPlan

_COMMAND_ADAPTER = TypeAdapter(Command)

_ALLOWED_KEYS: dict[str, frozenset[str]] = {
    "upgrade": frozenset({"type", "buildingType", "reason"}),
    "train": frozenset({"type", "troopType", "count", "reason"}),
    "attack": frozenset({"type", "targetCityId", "troopType", "count", "reason"}),
    "message": frozenset({"type", "toPlayerId", "subject", "body", "reason"}),
    "ally": frozenset({"type", "toPlayerId", "reason"}),
    "enemy": frozenset({"type", "toPlayerId", "reason"}),
    "clear_relation": frozenset({"type", "toPlayerId", "reason"}),
}


def _target_name_for_id(possible: dict, target_city_id: str) -> str | None:
    target_id = target_city_id.lower()
    for target in possible.get("targets") or []:
        if str(target.get("targetCityId", "")).lower() == target_id:
            return target.get("targetName")
    return None


def _max_train_count(possible: dict, troop_type: str | None) -> int:
    if not troop_type:
        return 1
    troop = troop_type.lower()
    for entry in possible.get("train") or []:
        if str(entry.get("troopType", "")).lower() == troop:
            return max(1, int(entry.get("maxCount", entry.get("count", 1))))
    return 1


def _default_reason(command: dict, possible: dict) -> str:
    command_type = command.get("type")
    if command_type == "upgrade":
        building = command.get("buildingType", "building")
        return f"Upgrade {building} to strengthen the city this tick."
    if command_type == "train":
        troop = command.get("troopType", "troop")
        return f"Train one {troop} for upcoming operations."
    if command_type == "attack":
        target_name = _target_name_for_id(
            possible,
            str(command.get("targetCityId", "")),
        )
        troop = command.get("troopType", "soldier")
        if troop == "spy":
            label = target_name or "a nearby city"
            return f"Scout {label} before committing troops."
        label = target_name or "declared enemy"
        return f"Attack {label} while soldiers are available."
    if command_type == "message":
        return "Reply to the latest diplomatic message."
    if command_type == "ally":
        return "Propose a mutual alliance."
    if command_type == "enemy":
        return "Declare hostilities against this player."
    if command_type == "clear_relation":
        return "Clear the existing diplomatic relation."
    return "Execute this action for strategic advantage this tick."


def normalize_command(command: dict, possible: dict) -> tuple[dict, list[str]]:
    repairs: list[str] = []
    normalized = dict(command)
    command_type = normalized.get("type")

    if command_type not in _ALLOWED_KEYS:
        return normalized, repairs

    allowed = _ALLOWED_KEYS[command_type]
    stripped = [key for key in normalized if key not in allowed]
    if stripped:
        for key in stripped:
            normalized.pop(key, None)
        repairs.append(f"stripped extra fields from {command_type}: {', '.join(stripped)}")

    if command_type == "attack":
        count = normalized.get("count", 1)
        if count != 1:
            normalized["count"] = 1
            repairs.append(f"clamped attack count {count} to 1")

    if command_type == "train":
        raw_count = normalized.get("count", 1)
        try:
            count = int(raw_count)
        except (TypeError, ValueError):
            count = 1
        max_count = _max_train_count(possible, normalized.get("troopType"))
        clamped = max(1, min(count, max_count))
        if clamped != raw_count:
            normalized["count"] = clamped
            repairs.append(f"clamped train count {raw_count} to {clamped}")

    reason = normalized.get("reason")
    if not isinstance(reason, str) or not reason.strip():
        normalized["reason"] = _default_reason(normalized, possible)
        repairs.append(f"added default reason to {command_type}")

    return normalized, repairs


def parse_plan_from_llm(
    raw: dict,
    possible: dict,
) -> tuple[CommandPlan, list[str]]:
    repairs: list[str] = []
    commands: list[Command] = []

    for index, command_data in enumerate(raw.get("commands") or []):
        if not isinstance(command_data, dict):
            repairs.append(f"dropped commands[{index}]: not an object")
            continue

        normalized, command_repairs = normalize_command(command_data, possible)
        repairs.extend(command_repairs)

        try:
            commands.append(_COMMAND_ADAPTER.validate_python(normalized))
        except ValidationError as error:
            summary = _validation_summary(error)
            repairs.append(f"dropped commands[{index}] ({normalized.get('type')}): {summary}")

    plan = CommandPlan(
        schemaVersion=raw.get("schemaVersion", 1),
        observedAtTick=int(raw.get("observedAtTick", 0)),
        commands=commands,
    )
    return plan, repairs


def _validation_summary(error: ValidationError) -> str:
    for item in error.errors(include_url=False):
        location = ".".join(str(part) for part in item.get("loc", ()))
        message = item.get("msg", "invalid")
        if location:
            return f"{location}: {message}"
        return message
    return "invalid command"
