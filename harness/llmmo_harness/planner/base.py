from typing import Protocol

from llmmo_harness.client import GameClient
from llmmo_harness.schema import CommandPlan


class Planner(Protocol):
    def plan(self, client: GameClient) -> CommandPlan:
        ...
