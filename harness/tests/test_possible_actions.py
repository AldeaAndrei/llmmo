from llmmo_harness.executor import command_action_dict
from llmmo_harness.planner.validation import filter_plan_to_possible_actions
from llmmo_harness.schema import AttackCommand, CommandPlan, TrainCommand
from llmmo_harness.state import build_planner_hints, compact_possible_actions
import unittest


class FilterPlanTests(unittest.TestCase):
    def test_drops_commands_not_in_possible_actions(self) -> None:
        plan = CommandPlan.model_validate(
            {
                "schemaVersion": 1,
                "observedAtTick": 100,
                "commands": [
                    {
                        "type": "upgrade",
                        "buildingType": "barracks",
                        "reason": "invalid",
                    },
                    {
                        "type": "train",
                        "troopType": "spy",
                        "count": 1,
                        "reason": "invalid",
                    },
                ],
            }
        )
        possible = {"upgrades": [], "train": [], "targets": [], "diplomacy": {"relations": []}}

        filtered, dropped = filter_plan_to_possible_actions(plan, possible)

        self.assertEqual([], filtered.commands)
        self.assertEqual(2, len(dropped))


class CommandActionDictTests(unittest.TestCase):
    def test_command_action_dict_includes_train_fields(self) -> None:
        command = TrainCommand(
            type="train",
            troopType="spy",
            count=1,
            reason="Scout nearby cities",
        )
        action = command_action_dict(command)
        self.assertEqual("train", action["type"])
        self.assertEqual("spy", action["troopType"])
        self.assertEqual(1, action["count"])

    def test_command_action_dict_includes_attack_fields(self) -> None:
        command = AttackCommand(
            type="attack",
            targetCityId="00000000-0000-0000-0000-000000000001",
            troopType="soldier",
            count=1,
            reason="Weak neighbour",
        )
        action = command_action_dict(command)
        self.assertEqual("attack", action["type"])
        self.assertEqual("00000000-0000-0000-0000-000000000001", action["targetCityId"])
        self.assertEqual("soldier", action["troopType"])
        self.assertEqual(1, action["count"])


class CompactPossibleActionsTests(unittest.TestCase):
    def test_compact_possible_actions_strips_costs(self) -> None:
        raw = {
            "currentTick": 100,
            "resources": {"wood": 10, "stone": 10, "gold": 10, "food": 0},
            "foodProductionPerTick": 3,
            "foodUpkeepPerTick": 3,
            "troops": [{"type": "spy", "count": 2}],
            "upgrades": [
                {
                    "buildingType": "bakery",
                    "fromLevel": 1,
                    "toLevel": 2,
                    "cost": {"wood": 70, "stone": 50, "gold": 30, "food": 20},
                }
            ],
            "train": [
                {
                    "troopType": "soldier",
                    "count": 1,
                    "cost": {"wood": 0, "stone": 0, "gold": 1, "food": 1},
                }
            ],
            "targets": [
                {
                    "targetCityId": "abc",
                    "targetPlayerId": "player-1",
                    "targetName": "Enemy",
                    "distance": 5,
                    "travelTicks": 3,
                    "canAttack": True,
                    "canScout": False,
                }
            ],
            "diplomacy": {
                "relations": [
                    {
                        "playerId": "player-2",
                        "name": "Ally",
                        "playerType": "llm",
                        "relation": "ally",
                    }
                ],
                "players": [
                    {
                        "playerId": "player-1",
                        "name": "Neutral",
                        "playerType": "llm",
                        "relation": None,
                    }
                ],
                "canSendMessage": True,
                "canDeclareDiplomacy": False,
            },
        }

        compact = compact_possible_actions(raw)

        self.assertEqual(100, compact["currentTick"])
        self.assertEqual([{"buildingType": "bakery", "fromLevel": 1, "toLevel": 2}], compact["upgrades"])
        self.assertNotIn("cost", compact["upgrades"][0])
        self.assertEqual([{"troopType": "soldier", "count": 1}], compact["train"])
        self.assertEqual("abc", compact["targets"][0]["targetCityId"])
        self.assertTrue(compact["diplomacy"]["canSendMessage"])
        self.assertFalse(compact["diplomacy"]["canDeclareDiplomacy"])
        self.assertEqual(1, len(compact["diplomacy"]["relations"]))
        self.assertEqual("ally", compact["diplomacy"]["relations"][0]["relation"])


class PlannerHintsTests(unittest.TestCase):
    def test_hints_push_scout_when_hoarding_spies(self) -> None:
        possible = {
            "troops": [{"type": "soldier", "count": 1}, {"type": "spy", "count": 7}],
            "targets": [
                {
                    "targetCityId": "abc",
                    "targetName": "BrightCrown",
                    "travelTicks": 34,
                    "canScout": True,
                }
            ],
        }
        recent = [
            {"tick": 1, "action": {"type": "train", "troopType": "spy"}, "reason": "x"},
            {"tick": 2, "action": {"type": "upgrade", "buildingType": "wall"}, "reason": "y"},
        ]

        hints = build_planner_hints(possible, recent)

        self.assertTrue(any('"reason"' in hint for hint in hints))
        self.assertTrue(any("7 spy" in hint for hint in hints))

    def test_hints_prioritize_enemy_attack(self) -> None:
        possible = {
            "troops": [{"type": "soldier", "count": 1}, {"type": "spy", "count": 2}],
            "targets": [
                {
                    "targetCityId": "enemy-city",
                    "targetName": "Admin",
                    "travelTicks": 53,
                    "canAttack": True,
                    "canScout": True,
                    "relation": "enemy",
                }
            ],
        }

        hints = build_planner_hints(possible, [])

        self.assertTrue(any("declared enemy" in hint for hint in hints))
        self.assertTrue(any("Admin" in hint for hint in hints))
        self.assertTrue(hints[1].startswith("Priority this plan: attack declared enemy"))

    def test_hints_include_inventory_count_reminder_without_scout_targets(self) -> None:
        possible = {
            "troops": [{"type": "spy", "count": 5}],
            "targets": [{"targetCityId": "abc", "canScout": False}],
        }

        hints = build_planner_hints(possible, [])

        self.assertEqual(1, len(hints))
        self.assertIn("Command count is always exactly 1", hints[0])


if __name__ == "__main__":
    unittest.main()
