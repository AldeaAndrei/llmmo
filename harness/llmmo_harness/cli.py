import argparse
import json
import sys
import time
from pathlib import Path

from llmmo_harness.client import GameClient
from llmmo_harness.config import HarnessConfig, load_config
from llmmo_harness.executor import execute_pending
from llmmo_harness.memory import DecisionMemory
from llmmo_harness.planner.mock import MockPlanner
from llmmo_harness.planner.ollama import OllamaPlanner
from llmmo_harness.queue import CommandQueue
from llmmo_harness.state import resolve_first_city_id


def _memory(config: HarnessConfig) -> DecisionMemory:
    return DecisionMemory(config.resolve_path(config.database.path))


def _build_planner(config: HarnessConfig, memory: DecisionMemory):
    if config.planner.type == "mock":
        return MockPlanner(config.resolve_path(config.planner.mock_plan_path))
    if config.planner.type == "ollama":
        return OllamaPlanner(config.planner.ollama, memory)
    raise ValueError(f"Unknown planner type: {config.planner.type}")


def cmd_plan(config: HarnessConfig) -> int:
    api_key = config.resolve_api_key()
    client = GameClient(config.api.base_url, api_key)
    queue = CommandQueue(config.resolve_path(config.database.path))
    memory = _memory(config)
    planner = _build_planner(config, memory)

    try:
        plan = planner.plan(client)
        city_id = resolve_first_city_id(client)
        plan_id = queue.enqueue_plan(plan, config.planner.type, city_id)
    except Exception as error:
        print(f"plan failed: {error}", file=sys.stderr)
        return 1

    print(
        f"Enqueued plan {plan_id}: {len(plan.commands)} command(s), "
        f"tick={plan.observedAtTick}, city={city_id}"
    )
    return 0


def cmd_execute(config: HarnessConfig) -> int:
    api_key = config.resolve_api_key()
    client = GameClient(config.api.base_url, api_key)
    queue = CommandQueue(config.resolve_path(config.database.path))
    memory = _memory(config)

    try:
        count = execute_pending(
            client,
            queue,
            memory,
            max_commands=config.schedule.max_commands_per_execute,
        )
    except Exception as error:
        print(f"execute failed: {error}", file=sys.stderr)
        return 1

    pending = queue.pending_count()
    print(f"Executed {count} command(s). {pending} pending.")
    return 0


def cmd_log(config: HarnessConfig, last: int) -> int:
    queue = CommandQueue(config.resolve_path(config.database.path))
    rows = queue.recent_logs(limit=last)
    if not rows:
        print("No execution log entries.")
        return 0

    for row in rows:
        status = "OK" if row["ok"] else "FAIL"
        print(
            f"[{row['id']}] {row['executed_at']} queue={row['queue_id']} "
            f"{status} http={row['http_status']}"
        )
        if row["error_message"]:
            print(f"  error: {row['error_message']}")
        try:
            request = json.loads(row["request_json"])
            reason = request.get("payload", {}).get("reason")
            if reason:
                print(f"  why: {reason}")
            print(f"  request: {json.dumps(request)}")
        except json.JSONDecodeError:
            print(f"  request: {row['request_json']}")
        if row["response_json"]:
            print(f"  response: {row['response_json']}")
    return 0


def cmd_run(config: HarnessConfig) -> int:
    plan_interval = config.schedule.plan_interval_seconds
    execute_interval = config.schedule.execute_interval_seconds
    last_plan = 0.0
    last_execute = 0.0

    print(
        f"Running harness loop (plan every {plan_interval}s, "
        f"execute every {execute_interval}s). Ctrl+C to stop."
    )

    try:
        while True:
            now = time.monotonic()
            if now - last_plan >= plan_interval:
                cmd_plan(config)
                last_plan = now
            if now - last_execute >= execute_interval:
                cmd_execute(config)
                last_execute = now
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nStopped.")
        return 0


def main(argv: list[str] | None = None) -> None:
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument(
        "-c",
        "--config",
        default="config.yaml",
        help="Path to config YAML (default: config.yaml)",
    )

    parser = argparse.ArgumentParser(description="LLMMO agent harness")
    parser.add_argument(
        "-c",
        "--config",
        default="config.yaml",
        help="Path to config YAML (default: config.yaml)",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser(
        "plan", parents=[common], help="Run planner and enqueue commands"
    )
    subparsers.add_parser(
        "execute", parents=[common], help="Execute pending commands"
    )
    subparsers.add_parser(
        "run", parents=[common], help="Loop plan + execute on schedule"
    )

    log_parser = subparsers.add_parser(
        "log", parents=[common], help="Show recent execution log"
    )
    log_parser.add_argument(
        "--last",
        type=int,
        default=20,
        help="Number of log entries to show (default: 20)",
    )

    args = parser.parse_args(argv)
    config_path = Path(args.config)
    if not config_path.exists():
        print(f"Config not found: {config_path}", file=sys.stderr)
        sys.exit(1)

    config = load_config(config_path)

    if args.command == "plan":
        sys.exit(cmd_plan(config))
    if args.command == "execute":
        sys.exit(cmd_execute(config))
    if args.command == "log":
        sys.exit(cmd_log(config, args.last))
    if args.command == "run":
        sys.exit(cmd_run(config))


if __name__ == "__main__":
    main()
