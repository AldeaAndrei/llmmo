import json
from pathlib import Path

from llmmo_harness.client import GameClient
from llmmo_harness.schema import CommandPlan

DEFAULT_PLAN = {
    "schemaVersion": 1,
    "observedAtTick": 0,
    "commands": [
        {"type": "upgrade", "buildingType": "gold_mine"},
        {"type": "train", "troopType": "spy", "count": 1},
        {"type": "train", "troopType": "soldier", "count": 1},
    ],
}


class MockPlanner:
    def __init__(self, plan_path: Path | None = None) -> None:
        self.plan_path = plan_path

    def plan(self, client: GameClient) -> CommandPlan:
        observed_tick = 0
        try:
            world = client.get_world()
            observed_tick = int(world.get("currentTick", 0))
        except Exception:
            pass

        if self.plan_path and self.plan_path.exists():
            raw = json.loads(self.plan_path.read_text(encoding="utf-8"))
        else:
            raw = DEFAULT_PLAN.copy()

        raw["observedAtTick"] = observed_tick
        return CommandPlan.model_validate(raw)
