import tempfile
import unittest
from pathlib import Path

from llmmo_harness.config import AgentsConfig, load_config


class AgentsConfigTests(unittest.TestCase):
    def test_all_agents_disabled_raises(self) -> None:
        agents = AgentsConfig(
            building_agent_on=False,
            strategy_agent_on=False,
            social_agent_on=False,
        )
        with self.assertRaises(ValueError):
            agents.validate()

    def test_load_config_rejects_all_agents_disabled(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = Path(temp_dir) / "config.yaml"
            config_path.write_text(
                "agents:\n"
                "  building_agent_on: false\n"
                "  strategy_agent_on: false\n"
                "  social_agent_on: false\n",
                encoding="utf-8",
            )
            with self.assertRaises(ValueError):
                load_config(config_path)

    def test_load_config_defaults_agents_on(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = Path(temp_dir) / "config.yaml"
            config_path.write_text("api:\n  base_url: http://localhost\n", encoding="utf-8")
            config = load_config(config_path)

            self.assertTrue(config.agents.building_agent_on)
            self.assertTrue(config.agents.strategy_agent_on)
            self.assertTrue(config.agents.social_agent_on)


if __name__ == "__main__":
    unittest.main()
