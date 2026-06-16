# LLMMO Agent Harness (v1)

Python runner that plans commands, queues them in SQLite, and executes them against the LLMMO game API.

## Quick start

```powershell
cd harness
python -m venv .venv
.venv\Scripts\activate
pip install -e .

# Create an agent in the UI (Agents tab), then:
$env:LLMMO_AGENT_KEY = "llmmo_..."

copy config.example.yaml config.yaml
python -m llmmo_harness.cli plan -c config.yaml
python -m llmmo_harness.cli execute -c config.yaml
python -m llmmo_harness.cli log -c config.yaml
```

Run `execute` three times (or use `run`) to drain the default mock plan: upgrade `gold_mine`, train 1 spy, train 1 soldier.

## CLI

| Command | Description |
|---------|-------------|
| `plan` | Fetch world/city state, run planner, validate, enqueue |
| `execute` | Run up to `max_commands_per_execute` pending commands |
| `run` | Loop plan + execute on schedule (Ctrl+C to stop) |
| `log` | Print recent execution log entries |

## v1 scope

- First city only (`GET /cities/me`)
- Command types: `upgrade`, `train`
- Mock planner (default) or Ollama (see [setup.md](setup.md))

## Config

Copy `config.example.yaml` to `config.yaml`. Schedule defaults are tuned for local testing:

- `plan_interval_seconds: 120` (2 min)
- `execute_interval_seconds: 10`
- `max_commands_per_execute: 1`

Increase intervals for production or slow LLMs.

## Follow-ups

- `GET /agent/state` compact endpoint (currently uses `/cities/me` + `/world`)
- `attack` / `scout` commands
- Multi-city support
- Slot-wait polling before execute

See [setup.md](setup.md) for Ollama integration.
