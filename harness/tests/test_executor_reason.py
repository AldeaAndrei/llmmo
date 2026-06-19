from llmmo_harness.executor import build_action_request
from llmmo_harness.schema import TrainCommand, UpgradeCommand
import unittest


class ExecutorReasonTests(unittest.TestCase):
    def test_build_action_request_includes_reason_for_upgrade(self) -> None:
        command = UpgradeCommand(
            type="upgrade",
            buildingType="gold_mine",
            reason="Gold is lowest",
        )
        action_type, payload = build_action_request("city-1", command)
        self.assertEqual("upgrade", action_type)
        self.assertEqual("gold_mine", payload["buildingType"])
        self.assertEqual("Gold is lowest", payload["reason"])

    def test_build_action_request_includes_reason_for_train(self) -> None:
        command = TrainCommand(
            type="train",
            troopType="soldier",
            count=1,
            reason="Need troops",
        )
        action_type, payload = build_action_request("city-1", command)
        self.assertEqual("train", action_type)
        self.assertEqual(1, payload["count"])
        self.assertEqual("Need troops", payload["reason"])


if __name__ == "__main__":
    unittest.main()
