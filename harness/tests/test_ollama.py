import json
import unittest

from llmmo_harness.planner.ollama import _parse_llm_json, _repair_doubled_json_braces


class OllamaJsonParseTests(unittest.TestCase):
    def test_repair_doubled_braces_in_commands(self) -> None:
        broken = (
            '{"schemaVersion":1,"observedAtTick":73644,"commands":'
            '[{{"type":"message","toPlayerId":"p1","subject":"Re: Hi",'
            '"body":"Hello","reason":"Reply"}}]}'
        )

        parsed = _parse_llm_json(broken)

        self.assertEqual(1, len(parsed["commands"]))
        self.assertEqual("message", parsed["commands"][0]["type"])
        self.assertEqual("p1", parsed["commands"][0]["toPlayerId"])

    def test_valid_json_unchanged(self) -> None:
        valid = json.dumps(
            {
                "schemaVersion": 1,
                "observedAtTick": 100,
                "commands": [],
            }
        )

        parsed = _parse_llm_json(valid)

        self.assertEqual(100, parsed["observedAtTick"])
        self.assertEqual([], parsed["commands"])

    def test_repair_helper_collapses_doubles(self) -> None:
        self.assertEqual('{"a":1}', _repair_doubled_json_braces('{{"a":1}}'))


if __name__ == "__main__":
    unittest.main()
