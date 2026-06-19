import unittest

from llmmo_harness.schema import AttackCommand, TrainCommand, UpgradeCommand


class SchemaReasonTests(unittest.TestCase):
    def test_train_requires_count_one(self) -> None:
        with self.assertRaises(ValueError):
            TrainCommand(
                type="train",
                troopType="soldier",
                count=2,
                reason="Too many",
            )

    def test_attack_requires_count_one(self) -> None:
        with self.assertRaises(ValueError):
            AttackCommand(
                type="attack",
                targetCityId="00000000-0000-0000-0000-000000000001",
                troopType="soldier",
                count=5,
                reason="Too many",
            )


if __name__ == "__main__":
    unittest.main()
