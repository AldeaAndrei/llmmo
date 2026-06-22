import json

from llmmo_harness.executor import command_action_dict
from llmmo_harness.planner.validation import command_allowed, filter_plan_to_possible_actions
from llmmo_harness.schema import (
    AttackCommand,
    CommandPlan,
    MessageCommand,
    TrainCommand,
    UpgradeCommand,
)
from llmmo_harness.state import (
    build_available_actions,
    build_building_context,
    build_planner_context,
    build_social_alert,
    build_social_context,
    build_strategic_alert,
    build_strategic_context,
    build_threat_summary,
    compact_possible_actions,
    enrich_possible_for_validation,
    has_unread_message,
    merge_players_with_relations,
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

    def test_train_count_within_max_count_is_allowed(self) -> None:
        plan = CommandPlan.model_validate(
            {
                "schemaVersion": 1,
                "observedAtTick": 100,
                "commands": [
                    {
                        "type": "train",
                        "troopType": "soldier",
                        "count": 10,
                        "reason": "Rebuild garrison.",
                    },
                    {
                        "type": "train",
                        "troopType": "soldier",
                        "count": 99,
                        "reason": "Over the cap.",
                    },
                ],
            }
        )
        possible = {
            "train": [{"troopType": "soldier", "maxCount": 12}],
            "targets": [],
            "diplomacy": {"relations": []},
        }

        filtered, dropped = filter_plan_to_possible_actions(plan, possible)

        self.assertEqual(1, len(filtered.commands))
        self.assertEqual(10, filtered.commands[0].count)
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
        self.assertEqual([{"troopType": "soldier", "maxCount": 1}], compact["train"])
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


class AgentContextSliceTests(unittest.TestCase):
    def _possible(self) -> dict:
        return {
            "currentTick": 200,
            "resources": {"wood": 100, "stone": 100, "gold": 100, "food": 100},
            "foodProductionPerTick": 10,
            "foodUpkeepPerTick": 3,
            "troops": [{"type": "spy", "count": 4}],
            "upgrades": [{"buildingType": "wall", "fromLevel": 2, "toLevel": 3}],
            "train": [{"troopType": "soldier", "maxCount": 8}],
            "targets": [
                {
                    "targetCityId": "c1",
                    "targetPlayerId": "p1",
                    "targetName": "Foe",
                    "distance": 5,
                    "travelTicks": 5,
                    "canAttack": False,
                    "canScout": True,
                }
            ],
            "diplomacy": {
                "relations": [],
                "canSendMessage": True,
                "canDeclareDiplomacy": True,
            },
        }

    def test_building_context_excludes_troops_and_diplomacy(self) -> None:
        city = {"buildings": [{"type": "wall", "level": 2, "description": "d"}]}

        context = build_building_context(city, self._possible(), [])

        self.assertIn("resources", context["cityState"])
        self.assertIn("buildings", context["cityState"])
        self.assertEqual(10, context["cityState"]["foodProductionPerTick"])
        self.assertIn("upgrades", context["availableActions"])
        self.assertNotIn("train", context["availableActions"])
        self.assertNotIn("targets", context["availableActions"])
        # No troop/diplomacy/report leakage anywhere in the slice.
        blob = json.dumps(context)
        self.assertNotIn("troops", blob)
        self.assertNotIn("diplomacy", blob)

    def test_strategic_context_excludes_resources_and_buildings(self) -> None:
        context = build_strategic_context(self._possible(), [], [])

        self.assertIn("troops", context)
        self.assertNotIn("diplomacy", context)
        self.assertIn("threatSummary", context)
        self.assertNotIn("unreadReports", context)
        self.assertIn("train", context["availableActions"])
        self.assertIn("targets", context["availableActions"])
        blob = json.dumps(context)
        self.assertNotIn("buildings", blob)
        self.assertNotIn("resources", blob)
        self.assertNotIn("upgrades", blob)

    def test_threat_summary_counts_defeats_and_enemies(self) -> None:
        reports = [
            {
                "id": "d1",
                "type": "attack",
                "sourceCityId": "city-a",
                "readAt": None,
                "createdAt": "2026-06-22T08:00:00Z",
                "payload": {"perspective": "defender", "outcome": "defeat"},
            },
            {
                "id": "d2",
                "type": "attack",
                "sourceCityId": "city-a",
                "readAt": None,
                "createdAt": "2026-06-22T11:00:00Z",
                "payload": {"perspective": "defender", "outcome": "defeat"},
            },
            {
                "id": "r3",
                "type": "scout",
                "readAt": None,
                "payload": {},
            },
            {
                "id": "r4",
                "type": "attack",
                "readAt": "2026-01-01T00:00:00Z",
                "payload": {"perspective": "defender", "outcome": "defeat"},
            },
        ]
        troops = [{"type": "spy", "count": 1}]
        diplomacy = {
            "relations": [
                {
                    "playerId": "p-admin",
                    "name": "Admin",
                    "playerType": "human",
                    "relation": "enemy",
                }
            ]
        }

        summary = build_threat_summary(reports, troops, diplomacy)

        self.assertEqual(3, summary["unreadReportCount"])
        self.assertEqual(2, summary["unreadDefeatCount"])
        self.assertEqual(0, summary["soldierCount"])
        self.assertEqual(1, summary["spyCount"])
        self.assertEqual(["Admin"], summary["declaredEnemies"])
        self.assertEqual("city-a", summary["mostRecentDefeat"]["sourceCityId"])
        self.assertEqual("2026-06-22T11:00:00Z", summary["mostRecentDefeat"]["createdAt"])

    def test_strategic_alert_when_zero_soldiers_and_defeats(self) -> None:
        threat = {"soldierCount": 0, "unreadDefeatCount": 4}
        actions = {"train": [{"troopType": "soldier", "maxCount": 32}]}

        alert = build_strategic_alert(threat, actions)

        self.assertIn("0 soldiers", alert or "")
        self.assertIn("4 unread defeat", alert or "")

    def test_strategic_alert_none_when_diplomacy_only(self) -> None:
        threat = {"soldierCount": 0, "unreadDefeatCount": 4}
        actions = {"diplomacyOnly": True}

        self.assertIsNone(build_strategic_alert(threat, actions))

    def test_social_context_includes_players_and_unread_messages(self) -> None:
        possible = self._possible()
        players = [
            {"id": "p1", "name": "Alpha", "playerType": "llm"},
            {"id": "p2", "name": "Beta", "playerType": "human"},
        ]
        relations = [
            {
                "otherPlayerId": "p2",
                "otherPlayerName": "Beta",
                "relation": "enemy",
            }
        ]
        messages = [
            {
                "id": "m1",
                "fromPlayerId": "p2",
                "fromPlayerName": "Beta",
                "toPlayerId": "me",
                "subject": "Hello",
                "body": "Truce?",
                "sentAtTick": 100,
                "readAt": None,
            },
            {
                "id": "m2",
                "fromPlayerId": "me",
                "toPlayerId": "p1",
                "subject": "Hi",
                "body": "Hey",
                "sentAtTick": 90,
                "readAt": None,
            },
        ]

        context = build_social_context(
            possible,
            players,
            relations,
            messages,
            [],
            "me",
            [],
        )

        self.assertIn("socialSummary", context)
        self.assertIn("replyTarget", context)
        self.assertEqual(1, context["socialSummary"]["unreadMessageCount"])
        self.assertEqual(2, len(context["players"]))
        self.assertEqual("enemy", context["players"][1]["relation"])
        self.assertEqual(1, len(context["unreadMessages"]))
        self.assertEqual("Beta", context["unreadMessages"][0]["fromPlayerName"])
        self.assertTrue(context["availableActions"]["canSendMessage"])

    def test_social_alert_when_unread_message(self) -> None:
        summary = {"unreadMessageCount": 1, "unreadDefeatCount": 0}
        actions = {
            "canSendMessage": True,
            "diplomacyOnly": True,
            "mustReplyToPlayerId": "p2",
            "mustReplyToPlayerName": "Beta",
        }
        reply = {
            "toPlayerId": "p2",
            "toPlayerName": "Beta",
            "subject": "Hello",
            "body": "Truce?",
        }

        alert = build_social_alert(summary, actions, reply)

        self.assertIn("Reply now", alert or "")
        self.assertIn("p2", alert or "")

    def test_social_context_slims_players_when_diplomacy_only(self) -> None:
        possible = self._possible()
        possible["diplomacy"]["latestUnreadMessage"] = {
            "id": "m1",
            "fromPlayerId": "p2",
            "fromPlayerName": "Beta",
        }
        players = [
            {"id": "p1", "name": "Alpha", "playerType": "llm"},
            {"id": "p2", "name": "Beta", "playerType": "human"},
            {"id": "p3", "name": "Gamma", "playerType": "llm"},
        ]
        messages = [
            {
                "id": "m1",
                "fromPlayerId": "p2",
                "fromPlayerName": "Beta",
                "toPlayerId": "me",
                "subject": "Hi",
                "body": "Hello",
                "sentAtTick": 1,
                "readAt": None,
            }
        ]

        context = build_social_context(
            possible, players, [], messages, [], "me", []
        )

        self.assertLessEqual(len(context["players"]), 2)
        self.assertIn("replyTarget", context)

    def test_social_actions_diplomacy_only_when_unread_message(self) -> None:
        possible = self._possible()
        possible["diplomacy"]["latestUnreadMessage"] = {
            "id": "m1",
            "fromPlayerId": "p1",
            "fromPlayerName": "Foe",
        }

        context = build_social_context(possible, [], [], [], [], "me", [])
        actions = context["availableActions"]

        self.assertTrue(actions["diplomacyOnly"])
        self.assertEqual("p1", actions["mustReplyToPlayerId"])

    def test_merge_players_with_relations(self) -> None:
        merged = merge_players_with_relations(
            [{"id": "p1", "name": "A", "playerType": "llm"}],
            [{"otherPlayerId": "p1", "relation": "ally"}],
        )

        self.assertEqual("ally", merged[0]["relation"])

    def test_diplomacy_allows_any_player_when_all_player_ids_present(self) -> None:
        possible = {
            "diplomacy": {"canSendMessage": True, "relations": []},
            "targets": [],
            "allPlayerIds": ["distant-player-id"],
        }
        command = MessageCommand(
            type="message",
            toPlayerId="distant-player-id",
            subject="Hello",
            body="There",
            reason="Opening channel",
        )

        self.assertTrue(command_allowed(command, possible))

    def test_enrich_possible_for_validation(self) -> None:
        enriched = enrich_possible_for_validation(
            {"currentTick": 1},
            [{"id": "p1"}, {"id": "p2"}],
        )

        self.assertEqual(["p1", "p2"], enriched["allPlayerIds"])

    def test_strategic_context_keeps_train_when_unread_message(self) -> None:
        possible = self._possible()
        possible["diplomacy"]["latestUnreadMessage"] = {
            "id": "m1",
            "fromPlayerId": "p1",
            "fromPlayerName": "Foe",
        }

        context = build_strategic_context(possible, [], [])
        actions = context["availableActions"]

        self.assertIn("train", actions)
        self.assertIn("targets", actions)
        self.assertNotIn("diplomacyOnly", actions)


if __name__ == "__main__":
    unittest.main()
