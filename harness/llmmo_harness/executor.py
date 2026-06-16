import json

from llmmo_harness.client import GameApiError, GameClient
from llmmo_harness.queue import CommandQueue, QueuedCommand
from llmmo_harness.schema import CommandPlan, TrainCommand, UpgradeCommand
from llmmo_harness.state import resolve_first_city_id


def build_action_request(city_id: str, command: UpgradeCommand | TrainCommand) -> tuple[str, dict]:
    if isinstance(command, UpgradeCommand):
        return "upgrade", {"buildingType": command.buildingType}
    return "train", {
        "buildingType": "barracks",
        "troopType": command.troopType,
        "count": command.count,
    }


def execute_command(
    client: GameClient,
    queue: CommandQueue,
    item: QueuedCommand,
    city_id: str,
) -> bool:
    command_data = json.loads(item.command_json)
    command_type = command_data["type"]

    if command_type == "upgrade":
        command = UpgradeCommand.model_validate(command_data)
    else:
        command = TrainCommand.model_validate(command_data)

    action_type, payload = build_action_request(city_id, command)
    request_body = {
        "cityId": city_id,
        "type": action_type,
        "payload": payload,
    }

    try:
        response = client.create_action(city_id, action_type, payload)
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
    max_commands: int = 1,
) -> int:
    city_id = resolve_first_city_id(client)
    items = queue.pop_pending(limit=max_commands)
    executed = 0
    for item in items:
        execute_command(client, queue, item, city_id)
        executed += 1
    return executed
