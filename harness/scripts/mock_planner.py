#!/usr/bin/env python3
"""Print the default mock plan JSON to stdout."""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PLAN_PATH = ROOT / "plans" / "mock_default.json"


def main() -> None:
    if PLAN_PATH.exists():
        print(PLAN_PATH.read_text(encoding="utf-8"), end="")
    else:
        from llmmo_harness.planner.mock import DEFAULT_PLAN

        json.dump(DEFAULT_PLAN, sys.stdout, indent=2)
        sys.stdout.write("\n")


if __name__ == "__main__":
    main()
