from llmmo_harness.executor import command_action_dict
from llmmo_harness.planner.validation import filter_plan_to_possible_actions
from llmmo_harness.schema import (
    AttackCommand,
    CommandPlan,
    MessageCommand,
    TrainCommand,
    UpgradeCommand,
)
from llmmo_harness.state import (
    build_planner_context,
    build_available_actions,
    compact_possible_actions,
    has_unread_message,
)
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

    def test_blocks_non_diplomacy_when_unread_message(self) -> None:
        plan = CommandPlan.model_validate(
            {
                "schemaVersion": 1,
                "observedAtTick": 100,
                "commands": [
                    {
                        "type": "train",
                        "troopType": "spy",
                        "count": 1,
                        "reason": "Should drop",
                    },
                    {
                        "type": "message",
                        "toPlayerId": "00000000-0000-0000-0000-000000000001",
                        "subject": "Re: hello",
                        "body": "Acknowledged.",
                        "reason": "Reply to sender.",
                    },
                ],
            }
        )
        possible = {
            "train": [{"troopType": "spy", "count": 1}],
            "targets": [],
            "diplomacy": {
                "relations": [],
                "canSendMessage": True,
                "latestUnreadMessage": {
                    "id": "msg-1",
                    "fromPlayerId": "00000000-0000-0000-0000-000000000001",
                },
            },
        }

        filtered, dropped = filter_plan_to_possible_actions(plan, possible)

        self.assertEqual(1, len(filtered.commands))
        self.assertIsInstance(filtered.commands[0], MessageCommand)
        self.assertEqual(1, len(dropped))

    def test_blocks_attack_on_ally(self) -> None:
        plan = CommandPlan.model_validate(
            {
                "schemaVersion": 1,
                "observedAtTick": 100,
                "commands": [
                    {
                        "type": "attack",
                        "targetCityId": "ally-city",
                        "troopType": "soldier",
                        "count": 1,
                        "reason": "Bad idea",
                    }
                ],
            }
        )
        possible = {
            "targets": [
                {
                    "targetCityId": "ally-city",
                    "targetPlayerId": "player-1",
                    "canAttack": True,
                    "canScout": False,
                    "relation": "ally",
                }
            ],
            "diplomacy": {"relations": []},
        }

        filtered, dropped = filter_plan_to_possible_actions(plan, possible)

        self.assertEqual([], filtered.commands)
        self.assertEqual(1, len(dropped))


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


class PlannerContextTests(unittest.TestCase):
    def test_build_planner_context_includes_buildings_and_unread_reports(self) -> None:
        city = {
            "buildings": [
                {
                    "type": "bakery",
                    "level": 2,
                    "description": "Produces food",
                    "currentEffect": "+3 food/tick",
                    "nextLevelEffect": "+4 food/tick",
                    "productionPerTick": 3,
                    "productionResource": "food",
                    "nextUpgradeCost": {"wood": 10, "stone": 10, "gold": 5, "food": 0},
                }
            ]
        }
        possible = {
            "currentTick": 50,
            "resources": {"food": 100},
            "foodProductionPerTick": 3,
            "foodUpkeepPerTick": 1,
            "troops": [{"type": "soldier", "count": 1}],
            "upgrades": [],
            "train": [],
            "targets": [],
            "diplomacy": {"relations": [], "canSendMessage": True, "canDeclareDiplomacy": True},
        }
        reports = [
            {"id": "r1", "type": "scout", "payload": {"seen": True}, "readAt": None},
            {"id": "r2", "type": "attack", "payload": {}, "readAt": "2026-01-01T00:00:00Z"},
        ]

        context = build_planner_context(city, possible, reports, [])

        self.assertEqual(1, len(context["cityState"]["buildings"]))
        self.assertEqual("bakery", context["cityState"]["buildings"][0]["type"])
        self.assertEqual(1, len(context["unreadReports"]))
        self.assertEqual("r1", context["unreadReports"][0]["id"])

    def test_available_actions_diplomacy_only_when_unread_message(self) -> None:
        possible = {
            "upgrades": [{"buildingType": "wall", "fromLevel": 1, "toLevel": 2}],
            "train": [{"troopType": "spy", "count": 1}],
            "targets": [{"targetCityId": "x", "targetPlayerId": "p", "targetName": "T", "distance": 1, "travelTicks": 1}],
            "diplomacy": {
                "canSendMessage": True,
                "canDeclareDiplomacy": False,
                "latestUnreadMessage": {"id": "m1", "fromPlayerId": "p"},
            },
        }

        self.assertTrue(has_unread_message(possible))
        actions = build_available_actions(possible)

        self.assertTrue(actions["diplomacyOnly"])
        self.assertNotIn("train", actions)
        self.assertNotIn("targets", actions)


if __name__ == "__main__":
    unittest.main()
