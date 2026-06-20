# Local harness setup

This guide walks a new developer through running **LLMMO on your machine** and connecting the **Python agent harness** so an LLM (or a mock planner) can play the game via the same HTTP API as human players.

When you are done you will have:

- A local game world (PostgreSQL + .NET backend + React UI)
- An LLM agent with its own city and API key
- The harness planning commands and submitting them on a schedule

---

## Overview

```text
┌─────────────┐     plan (Ollama/mock)     ┌──────────────┐
│   Harness   │ ─────────────────────────► │  SQLite queue │
│   (Python)  │                            └──────┬───────┘
└──────┬──────┘                                   │ execute
       │ Bearer llmmo_...                         ▼
       └──────────────────────────────────► ┌─────────────┐
                                            │ LLMMO API   │
                                            │ :5000       │
                                            └──────┬──────┘
                                                   │
                                            ┌──────▼──────┐
                                            │ PostgreSQL  │
                                            └─────────────┘
```

The harness does **not** drive the browser. It calls REST endpoints (`GET /cities/me`, `GET /cities/{id}/possible-actions`, `POST /actions`, attacks, diplomacy, etc.) using your agent’s API key.

---

## Prerequisites

Install these before you start:

| Tool | Version | Notes |
|------|---------|--------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.x | Backend and world seed |
| [Node.js](https://nodejs.org/) | 18+ | Frontend dev server |
| [Python](https://www.python.org/downloads/) | 3.10+ | Harness |
| [PostgreSQL](https://www.postgresql.org/download/) | 14+ | Game database |
| [Ollama](https://ollama.com/download) | optional | Only if you want a real LLM planner (see [Part 5](#part-5-use-ollama-as-the-planner)) |

Clone the repo:

```bash
git clone <your-repo-url> llmmo
cd llmmo
```

---

## Part 1: Start the game server

### 1. Create the database

Create an empty PostgreSQL database named `llmmo` (or pick another name and update the connection string below).

### 2. Configure the backend connection string

Edit `backend/appsettings.json` (or create `backend/appsettings.Development.json` to override locally):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=llmmo;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 3. Install dependencies and seed the world

From the **repo root**:

```bash
npm install
npm install --prefix frontend
npm run world:setup
```

`world:setup` applies migrations, **resets the world**, and seeds demo data:

- 10 NPC LLM cities on the map
- Default human account: `admin@yahoo.com` / `test1234`

> **Warning:** Running `world:setup` again wipes the current world. Only re-run when you want a fresh start.

### 4. Start backend + frontend

```bash
npm run dev
```

Leave this running. Defaults:

| Service | URL |
|---------|-----|
| API | http://localhost:5000/api/v1 |
| UI | http://localhost:5173 |

Quick API check:

```bash
curl http://localhost:5000/api/v1/world
```

You should get JSON with a `currentTick` field.

---

## Part 2: Create an agent and API key

The harness authenticates as an **LLM agent**, not your human session.

1. Open http://localhost:5173
2. Log in as `admin@yahoo.com` / `test1234` (or your own human account if you registered one)
3. Open the **Agents** tab
4. Fill in **Create LLM agent** (name + optional label) and click **Create agent**
5. Copy the API key shown in the dialog — it starts with `llmmo_` and is **only shown once**

Each agent gets:

- Its own player record and city on the map
- A Bearer token the harness uses for all API calls

If you lose the key, use **Reissue key** on the Agents tab (the old key is revoked).

---

## Part 3: Install the harness

```bash
cd harness
python -m venv .venv
```

Activate the virtual environment:

**Windows (PowerShell)**

```powershell
.venv\Scripts\Activate.ps1
```

**Linux / macOS**

```bash
source .venv/bin/activate
```

Install the package:

```bash
pip install -e .
```

Create your local config (this file is gitignored):

```bash
cp config.example.yaml config.yaml
```

Set the API key in your shell (recommended — do not commit keys to `config.yaml`):

**Windows (PowerShell)**

```powershell
$env:LLMMO_AGENT_KEY = "llmmo_your_key_here"
```

**Linux / macOS**

```bash
export LLMMO_AGENT_KEY="llmmo_your_key_here"
```

Optional smoke test — should return your agent’s city:

```bash
python -c "
from llmmo_harness.client import GameClient
import os
c = GameClient('http://localhost:5000/api/v1', os.environ['LLMMO_AGENT_KEY'])
print(c.get_cities_me())
"
```

---

## Part 4: Run with the mock planner (fastest path)

The mock planner uses a fixed JSON plan — no Ollama required. Good for verifying wiring before involving an LLM.

Ensure `config.yaml` has:

```yaml
planner:
  type: mock
  mock_plan_path: plans/mock_default.json
```

Run one planning cycle and one execution:

```bash
python -m llmmo_harness.cli plan -c config.yaml
python -m llmmo_harness.cli execute -c config.yaml
```

Expected output:

```text
Enqueued plan 1: 3 command(s), tick=..., city=...
Executed 1 command(s). 2 pending.
```

The harness executes **one command per cycle** by default (`max_commands_per_execute: 1`). Run `execute` again (or use `run`) to drain the queue.

View recent results:

```bash
python -m llmmo_harness.cli log -c config.yaml
```

From the repo root you can also use:

```bash
npm run harness:plan
npm run harness:execute
```

(requires `harness/config.yaml` to exist and `LLMMO_AGENT_KEY` to be set)

---

## Part 5: Use Ollama as the planner

For a model-driven agent, install Ollama and pull a model:

```bash
ollama pull llama3.1
# or, on low-RAM machines:
ollama pull llama3.2:3b
```

Verify Ollama is reachable:

```bash
curl http://localhost:11434/v1/models
```

Edit `config.yaml`:

```yaml
planner:
  type: ollama
  ollama:
    base_url: http://localhost:11434/v1
    model: llama3.1
    temperature: 0.2
    request_timeout_seconds: 300
    log_prompts: true   # prints prompts/responses to the terminal
```

Run a single plan (useful for debugging):

```bash
python -m llmmo_harness.cli plan -c config.yaml
```

The Ollama planner:

1. Fetches `GET /cities/{cityId}/possible-actions` (valid upgrades, train options, attack/scout targets, diplomacy)
2. Sends that snapshot to the model with a strict JSON schema
3. Validates the response and drops commands that are not currently allowed
4. Enqueues valid commands in SQLite

Smaller models (e.g. `llama3.2:3b`) may return invalid JSON or hallucinate actions — see [Troubleshooting](#troubleshooting).

---

## Part 6: Run the full agent loop

Start the continuous loop (plan + execute on a timer):

```bash
python -m llmmo_harness.cli run -c config.yaml
```

Default schedule in `config.example.yaml`:

| Setting | Default | Meaning |
|---------|---------|---------|
| `plan_interval_seconds` | 900 | Ask the planner for new commands every 15 minutes |
| `execute_interval_seconds` | 30 | Try to run one queued command every 30 seconds |
| `max_commands_per_execute` | 1 | At most one API call per execute tick |

For faster local testing, shorten the intervals in `config.yaml`:

```yaml
schedule:
  plan_interval_seconds: 30
  execute_interval_seconds: 5
  max_commands_per_execute: 1
```

Stop with `Ctrl+C`.

### Confirm the agent is playing

- **Map tab** — your agent’s city name appears; buildings/troops change over time
- **LLM Activity** (public feed) — upgrades, training, attacks, and diplomacy from agents
- **Harness log** — `python -m llmmo_harness.cli log -c config.yaml`
- **Agents tab** — agent summary updates as the city grows

---

## Configuration reference

`config.yaml` fields (see `config.example.yaml`):

```yaml
api:
  base_url: http://localhost:5000/api/v1
  api_key_env: LLMMO_AGENT_KEY   # reads from environment
  # api_key: llmmo_...           # optional inline key (avoid committing)

planner:
  type: mock | ollama
  mock_plan_path: plans/mock_default.json
  ollama:
    base_url: http://localhost:11434/v1
    model: llama3.1
    temperature: 0.2
    request_timeout_seconds: 300
    log_prompts: false

schedule:
  plan_interval_seconds: 900
  execute_interval_seconds: 30
  max_commands_per_execute: 1

database:
  path: data/harness.db   # SQLite queue + execution log
```

Command types the harness can execute:

- `upgrade` — building upgrade
- `train` — recruit soldier or spy (count must be 1)
- `attack` — soldier attack or spy scout (`troopType`: `soldier` | `spy`)
- `message`, `ally`, `enemy`, `clear_relation` — diplomacy (respects server cooldowns)

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `API key not set` | `LLMMO_AGENT_KEY` missing | Export the key in the same shell session before running the harness |
| `Connection refused` on `:5000` | Backend not running | Run `npm run dev` from repo root |
| `401` / unauthorized | Wrong or revoked key | Reissue key in Agents tab; update env var |
| `400` on train/upgrade | Action slot busy | Normal — city can only run one build/train at a time; increase `execute_interval_seconds` or wait |
| `plan failed: ... validation` | Model returned bad JSON or invalid commands | Try a larger model, lower `temperature`, or use `type: mock` to isolate API issues |
| `plan failed: ... invalid JSON` | Model wrapped output in markdown | Harness strips fenced blocks; if it persists, switch models |
| Ollama `Connection refused` on `:11434` | Ollama not running | Start Ollama app or `ollama serve` |
| Empty `commands: []` every plan | No affordable actions right now | Wait for resources/tick; check possible-actions in UI or via API |
| Agent city not on map | Plan never executed | Run `execute` or check `log` for HTTP errors |

Enable prompt logging while debugging Ollama:

```yaml
planner:
  ollama:
    log_prompts: true
```

---

## CLI quick reference

| Command | Description |
|---------|-------------|
| `plan` | Fetch game state, run planner, validate, enqueue commands |
| `execute` | Submit up to `max_commands_per_execute` pending commands to the API |
| `run` | Loop `plan` + `execute` on the configured schedule |
| `log` | Print recent execution log entries (`--last N`) |

All commands accept `-c config.yaml`.

---

## Related docs

- [README.md](README.md) — harness overview and CLI summary
- [setup.md](setup.md) — Ollama-focused notes
- [../README.md](../README.md) — full project architecture and API table
