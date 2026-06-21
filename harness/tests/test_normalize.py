from llmmo_harness.planner.normalize import normalize_command, parse_plan_from_llm
from llmmo_harness.planner.validation import command_allowed
from llmmo_harness.schema import MessageCommand
import unittest


class NormalizeCommandTests(unittest.TestCase):
    def test_adds_default_reason_to_attack_missing_it(self) -> None:
        possible = {
            "targets": [
                {
                    "targetCityId": "abc",
                    "targetName": "BrightCrown",
                }
            ]
        }
        normalized, repairs = normalize_command(
            {
                "type": "attack",
                "targetCityId": "abc",
                "troopType": "spy",
                "count": 1,
            },
            possible,
        )

        self.assertTrue(normalized["reason"])
        self.assertTrue(any("default reason" in repair for repair in repairs))

    def test_clamps_train_count_to_one(self) -> None:
        normalized, repairs = normalize_command(
            {
                "type": "train",
                "troopType": "spy",
                "count": 9,
                "reason": "Too many",
            },
            {},
        )

        self.assertEqual(1, normalized["count"])
        self.assertTrue(any("clamped" in repair for repair in repairs))

    def test_strips_extra_upgrade_fields(self) -> None:
        normalized, repairs = normalize_command(
            {
                "type": "upgrade",
                "buildingType": "bakery",
                "fromLevel": 3,
                "toLevel": 4,
                "reason": "Food",
            },
            {},
        )

        self.assertNotIn("fromLevel", normalized)
        self.assertNotIn("toLevel", normalized)
        self.assertTrue(any("stripped extra fields" in repair for repair in repairs))


class ParsePlanFromLlmTests(unittest.TestCase):
    def test_mixed_plan_keeps_valid_scout_and_drops_invalid_train(self) -> None:
        possible = {
            "targets": [
                {
                    "targetCityId": "abc",
                    "targetName": "BrightCrown",
                    "canScout": True,
                }
            ],
            "train": [{"troopType": "spy", "count": 1}],
        }
        plan, repairs = parse_plan_from_llm(
            {
                "schemaVersion": 1,
                "observedAtTick": 100,
                "commands": [
                    {
                        "type": "attack",
                        "targetCityId": "abc",
                        "troopType": "spy",
                        "count": 1,
                    },
                    {
                        "type": "train",
                        "troopType": "invalid",
                        "count": 9,
                        "reason": "bad",
                    },
                ],
            },
            possible,
        )

        self.assertEqual(1, len(plan.commands))
        self.assertEqual("attack", plan.commands[0].type)
        self.assertTrue(any("clamped" in repair for repair in repairs))


class DiplomacyRecipientValidationTests(unittest.TestCase):
    def test_message_allows_target_player_id(self) -> None:
        possible = {
            "targets": [
                {
                    "targetCityId": "city-1",
                    "targetPlayerId": "player-1",
                    "canAttack": False,
                    "canScout": False,
                }
            ],
            "diplomacy": {
                "relations": [],
                "canSendMessage": True,
            },
        }
        command = MessageCommand(
            type="message",
            toPlayerId="player-1",
            subject="Hello",
            body="Are you friendly?",
            reason="Opening diplomacy",
        )

        self.assertTrue(command_allowed(command, possible))

    def test_unread_message_restricts_recipient_to_sender(self) -> None:
        possible = {
            "targets": [
                {
                    "targetCityId": "city-1",
                    "targetPlayerId": "other-player",
                    "canAttack": False,
                    "canScout": False,
                }
            ],
            "diplomacy": {
                "relations": [],
                "canSendMessage": True,
                "latestUnreadMessage": {
                    "id": "msg-1",
                    "fromPlayerId": "sender-player",
                },
            },
        }

        to_other = MessageCommand(
            type="message",
            toPlayerId="other-player",
            subject="Hi",
            body="Unrelated message",
            reason="Dodging the sender",
        )
        to_sender = MessageCommand(
            type="message",
            toPlayerId="sender-player",
            subject="Re: your message",
            body="Acknowledged.",
            reason="Replying to the sender",
        )

        self.assertFalse(command_allowed(to_other, possible))
        self.assertTrue(command_allowed(to_sender, possible))


if __name__ == "__main__":
    unittest.main()
