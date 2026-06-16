# Ollama setup for LLMMO harness

This guide walks through using a local Ollama model as the harness planner.

## 1. Install Ollama

- **Windows:** https://ollama.com/download
- **Linux:** `curl -fsSL https://ollama.com/install.sh | sh`

Verify Ollama is running:

```powershell
ollama --version
```

## 2. Pull a model

```powershell
ollama pull llama3.1
```

For lower RAM machines, try a smaller model (e.g. `llama3.2:3b`).

## 3. Verify the API

Ollama exposes an OpenAI-compatible API on port 11434:

```powershell
curl http://localhost:11434/v1/models
```

You should see JSON listing installed models.

## 4. Configure the harness

```powershell
cd harness
copy config.example.yaml config.yaml
```

Edit `config.yaml`:

```yaml
planner:
  type: ollama
  ollama:
    base_url: http://localhost:11434/v1
    model: llama3.1
    temperature: 0.2
```

## 5. Create an agent and set API key

1. Start the game (`npm run dev` from repo root).
2. Seed the world if needed: `npm run world:setup`.
3. Open the UI → **Agents** tab → create an agent.
4. Copy the API key (`llmmo_...`).

```powershell
$env:LLMMO_AGENT_KEY = "llmmo_your_key_here"
```

## 6. Run the harness

```powershell
cd harness
.venv\Scripts\activate
python -m llmmo_harness.cli plan -c config.yaml
python -m llmmo_harness.cli execute -c config.yaml
python -m llmmo_harness.cli log -c config.yaml
```

Or run the full loop:

```powershell
python -m llmmo_harness.cli run -c config.yaml
```

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `plan failed: ... invalid JSON` | Model returned non-JSON; try a larger model or lower temperature |
| `API key not set` | Export `LLMMO_AGENT_KEY` or set `api.api_key` in config |
| `400` on train/upgrade | Action slot busy; increase `execute_interval_seconds` |
| Connection refused on `:5000` | Start backend with `npm run dev` |
| Connection refused on `:11434` | Start Ollama (`ollama serve` or launch the app) |

## How the Ollama planner works

The harness fetches `/world`, `/cities/me`, and `/catalog/troops`, sends them to the model with a strict JSON schema prompt, validates the response with Pydantic, and enqueues valid commands. Invalid JSON is rejected and nothing is enqueued.
