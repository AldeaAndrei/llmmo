import json

from llmmo_harness.client import GameApiError, GameClient
from llmmo_harness.memory import DecisionMemory
from llmmo_harness.queue import CommandQueue, QueuedCommand
from llmmo_harness.schema import (
    AllyCommand,
    AttackCommand,
    ClearRelationCommand,
    EnemyCommand,
    MessageCommand,
    TrainCommand,
    UpgradeCommand,
)
from llmmo_harness.state import resolve_first_city_id

AnyCommand = (
    UpgradeCommand
    | TrainCommand
    | AttackCommand
    | MessageCommand
    | AllyCommand
    | EnemyCommand
    | ClearRelationCommand
)


def command_action_dict(command: AnyCommand) -> dict:
    if isinstance(command, UpgradeCommand):
        return {"type": "upgrade", "buildingType": command.buildingType}
    if isinstance(command, TrainCommand):
        return {
            "type": "train",
            "troopType": command.troopType,
            "count": command.count,
        }
    if isinstance(command, AttackCommand):
        return {
            "type": "attack",
            "targetCityId": command.targetCityId,
            "troopType": command.troopType,
            "count": command.count,
        }
    if isinstance(command, MessageCommand):
        return {
            "type": "message",
            "toPlayerId": command.toPlayerId,
            "subject": command.subject,
        }
    if isinstance(command, AllyCommand):
        return {"type": "ally", "toPlayerId": command.toPlayerId}
    if isinstance(command, EnemyCommand):
        return {"type": "enemy", "toPlayerId": command.toPlayerId}
    if isinstance(command, ClearRelationCommand):
        return {"type": "clear_relation", "toPlayerId": command.toPlayerId}
    raise ValueError(f"unsupported command type: {type(command)}")


def build_action_request(city_id: str, command: UpgradeCommand | TrainCommand) -> tuple[str, dict]:
    if isinstance(command, UpgradeCommand):
        return "upgrade", {
            "buildingType": command.buildingType,
            "reason": command.reason,
        }
    return "train", {
        "buildingType": "barracks",
        "troopType": command.troopType,
        "count": command.count,
        "reason": command.reason,
    }


def _parse_command(command_data: dict) -> AnyCommand:
    command_type = command_data["type"]
    if command_type == "upgrade":
        return UpgradeCommand.model_validate(command_data)
    if command_type == "attack":
        return AttackCommand.model_validate(command_data)
    if command_type == "message":
        return MessageCommand.model_validate(command_data)
    if command_type == "ally":
        return AllyCommand.model_validate(command_data)
    if command_type == "enemy":
        return EnemyCommand.model_validate(command_data)
    if command_type == "clear_relation":
        return ClearRelationCommand.model_validate(command_data)
    return TrainCommand.model_validate(command_data)


def _execute_diplomacy_command(
    client: GameClient,
    queue: CommandQueue,
    memory: DecisionMemory,
    item: QueuedCommand,
    city_id: str,
    command: MessageCommand | AllyCommand | EnemyCommand | ClearRelationCommand,
) -> bool:
    if isinstance(command, MessageCommand):
        request_body = {
            "toPlayerId": command.toPlayerId,
            "subject": command.subject,
            "body": command.body,
            "reason": command.reason,
        }
        try:
            response = client.send_message(
                command.toPlayerId,
                command.subject,
                command.body,
            )
            submitted_tick = int(response.get("sentAtTick", 0))
            memory.record(
                city_id,
                submitted_tick,
                command_action_dict(command),
                command.reason,
            )
            queue.log_execution(
                queue_id=item.id,
                ok=True,
                http_status=201,
                request=request_body,
                response=response,
            )
            queue.mark_status(item.id, "done")
            return True
        except GameApiError as error:
            queue.log_execution(
                queue_id=item.id,
                ok=False,
                http_status=error.status_code,
                request=request_body,
                response=error.body if isinstance(error.body, dict) else None,
                error_message=str(error),
            )
            queue.mark_status(item.id, "failed")
            return False

    if isinstance(command, ClearRelationCommand):
        request_body = {
            "toPlayerId": command.toPlayerId,
            "reason": command.reason,
        }
        try:
            client.clear_relation(command.toPlayerId)
            memory.record(
                city_id,
                0,
                command_action_dict(command),
                command.reason,
            )
            queue.log_execution(
                queue_id=item.id,
                ok=True,
                http_status=204,
                request=request_body,
                response=None,
            )
            queue.mark_status(item.id, "done")
            return True
        except GameApiError as error:
            queue.log_execution(
                queue_id=item.id,
                ok=False,
                http_status=error.status_code,
                request=request_body,
                response=error.body if isinstance(error.body, dict) else None,
                error_message=str(error),
            )
            queue.mark_status(item.id, "failed")
            return False

    if isinstance(command, AllyCommand):
        relation = "ally"
    elif isinstance(command, EnemyCommand):
        relation = "enemy"
    else:
        raise ValueError(f"unexpected diplomacy command: {type(command)}")

    request_body = {
        "toPlayerId": command.toPlayerId,
        "relation": relation,
        "reason": command.reason,
    }
    try:
        response = client.set_relation(command.toPlayerId, relation)
        submitted_tick = int(response.get("updatedAtTick", 0))
        memory.record(
            city_id,
            submitted_tick,
            command_action_dict(command),
            command.reason,
        )
        queue.log_execution(
            queue_id=item.id,
            ok=True,
            http_status=200,
            request=request_body,
            response=response,
        )
        queue.mark_status(item.id, "done")
        return True
    except GameApiError as error:
        queue.log_execution(
            queue_id=item.id,
            ok=False,
            http_status=error.status_code,
            request=request_body,
            response=error.body if isinstance(error.body, dict) else None,
            error_message=str(error),
        )
        queue.mark_status(item.id, "failed")
        return False


def execute_command(
    client: GameClient,
    queue: CommandQueue,
    memory: DecisionMemory,
    item: QueuedCommand,
    city_id: str,
) -> bool:
    command_data = json.loads(item.command_json)
    command = _parse_command(command_data)

    if isinstance(
        command,
        (MessageCommand, AllyCommand, EnemyCommand, ClearRelationCommand),
    ):
        return _execute_diplomacy_command(client, queue, memory, item, city_id, command)

    if isinstance(command, AttackCommand):
        request_body = {
            "sourceCityId": city_id,
            "targetCityId": command.targetCityId,
            "type": "attack",
            "troops": [{"type": command.troopType, "count": command.count}],
            "reason": command.reason,
        }
        try:
            response = client.create_attack(
                city_id,
                command.targetCityId,
                command.troopType,
                command.count,
            )
            submitted_tick = int(response.get("departedAtTick", 0))
            memory.record(
                city_id,
                submitted_tick,
                command_action_dict(command),
                command.reason,
            )
            queue.log_execution(
                queue_id=item.id,
                ok=True,
                http_status=201,
                request=request_body,
                response=response,
            )
            queue.mark_status(item.id, "done")
            return True
        except GameApiError as error:
            queue.log_execution(
                queue_id=item.id,
                ok=False,
                http_status=error.status_code,
                request=request_body,
                response=error.body if isinstance(error.body, dict) else None,
                error_message=str(error),
            )
            queue.mark_status(item.id, "failed")
            return False

    action_type, payload = build_action_request(city_id, command)
    request_body = {
        "cityId": city_id,
        "type": action_type,
        "payload": payload,
    }

    try:
        response = client.create_action(city_id, action_type, payload)
        submitted_tick = int(response.get("submittedAtTick", 0))
        memory.record(
            city_id,
            submitted_tick,
            command_action_dict(command),
            command.reason,
        )
        queue.log_execution(
            queue_id=item.id,
            ok=True,
            http_status=201,
            request=request_body,
            response=response,
        )
        queue.mark_status(item.id, "done")
        return True
    except GameApiError as error:
        queue.log_execution(
            queue_id=item.id,
            ok=False,
            http_status=error.status_code,
            request=request_body,
            response=error.body if isinstance(error.body, dict) else None,
            error_message=str(error),
        )
        queue.mark_status(item.id, "failed")
        return False


def execute_pending(
    client: GameClient,
    queue: CommandQueue,
    memory: DecisionMemory,
    max_commands: int = 1,
) -> int:
    city_id = resolve_first_city_id(client)
    items = queue.pop_pending(limit=max_commands)
    executed = 0
    for item in items:
        execute_command(client, queue, memory, item, city_id)
        executed += 1
    return executed
