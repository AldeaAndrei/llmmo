import unittest
from pathlib import Path

from pydantic import ValidationError

from llmmo_harness.memory import DecisionMemory
from llmmo_harness.schema import CommandPlan, TrainCommand, UpgradeCommand


class SchemaReasonTests(unittest.TestCase):
    def test_upgrade_requires_reason(self) -> None:
        with self.assertRaises(ValidationError):
            UpgradeCommand(type="upgrade", buildingType="gold_mine")

    def test_upgrade_rejects_empty_reason(self) -> None:
        with self.assertRaises(ValidationError):
            UpgradeCommand(
                type="upgrade",
                buildingType="gold_mine",
                reason="",
            )

    def test_train_accepts_valid_reason(self) -> None:
        command = TrainCommand(
            type="train",
            troopType="soldier",
            count=1,
            reason="Need defenders",
        )
        self.assertEqual("Need defenders", command.reason)

    def test_command_plan_validates_with_reasons(self) -> None:
        plan = CommandPlan.model_validate(
            {
                "schemaVersion": 1,
                "observedAtTick": 10,
                "commands": [
                    {
                        "type": "upgrade",
                        "buildingType": "gold_mine",
                        "reason": "Boost gold income",
                    }
                ],
            }
        )
        self.assertEqual(1, len(plan.commands))


class DecisionMemoryTests(unittest.TestCase):
    def setUp(self) -> None:
        self.db_path = Path(f"file:mem_{self._testMethodName}?mode=memory&cache=shared")
        self.memory = DecisionMemory(self.db_path)
        self.city_id = "city-1"

    def tearDown(self) -> None:
        del self.memory

    def test_record_keeps_only_last_two_per_city(self) -> None:
        for index in range(3):
            self.memory.record(
                self.city_id,
                100 + index,
                {"type": "upgrade", "buildingType": "gold_mine"},
                f"reason {index}",
            )

        recent = self.memory.get_recent(self.city_id, limit=2)

        self.assertEqual(2, len(recent))
        self.assertEqual(102, recent[0].tick)
        self.assertEqual("reason 2", recent[0].reason)
        self.assertEqual(101, recent[1].tick)

    def test_record_prunes_to_retention_limit(self) -> None:
        from llmmo_harness.memory import RECENT_RETENTION

        for index in range(RECENT_RETENTION + 5):
            self.memory.record(
                self.city_id,
                index,
                {"type": "upgrade", "buildingType": "gold_mine"},
                f"reason {index}",
            )

        recent = self.memory.get_recent(self.city_id, limit=RECENT_RETENTION + 10)

        self.assertEqual(RECENT_RETENTION, len(recent))

    def test_get_recent_by_types_filters_by_action_type(self) -> None:
        self.memory.record(
            self.city_id, 1, {"type": "upgrade", "buildingType": "wall"}, "wall"
        )
        self.memory.record(
            self.city_id, 2, {"type": "train", "troopType": "spy", "count": 1}, "spy"
        )
        self.memory.record(
            self.city_id, 3, {"type": "upgrade", "buildingType": "bakery"}, "bakery"
        )
        self.memory.record(
            self.city_id,
            4,
            {"type": "message", "toPlayerId": "p1", "subject": "hi"},
            "msg",
        )

        upgrades = self.memory.get_recent_by_types(self.city_id, {"upgrade"}, limit=2)
        strategic = self.memory.get_recent_by_types(
            self.city_id, {"train", "attack"}, limit=5
        )
        social = self.memory.get_recent_by_types(
            self.city_id, {"message"}, limit=5
        )

        self.assertEqual(["bakery", "wall"], [r.action["buildingType"] for r in upgrades])
        self.assertEqual({"train"}, {r.action["type"] for r in strategic})
        self.assertEqual({"message"}, {r.action["type"] for r in social})

    def test_get_recent_is_scoped_per_city(self) -> None:
        self.memory.record(
            "city-a",
            1,
            {"type": "upgrade", "buildingType": "wall"},
            "wall for city a",
        )
        self.memory.record(
            "city-b",
            2,
            {"type": "train", "troopType": "spy", "count": 1},
            "spy for city b",
        )

        recent_a = self.memory.get_recent("city-a")
        recent_b = self.memory.get_recent("city-b")

        self.assertEqual(1, len(recent_a))
        self.assertEqual("wall for city a", recent_a[0].reason)
        self.assertEqual(1, len(recent_b))
        self.assertEqual("spy for city b", recent_b[0].reason)
        self.assertEqual("spy", recent_b[0].action["troopType"])


if __name__ == "__main__":
    unittest.main()
