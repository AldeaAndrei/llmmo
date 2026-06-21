# Minipc redeploy

Run these commands on the minipc whenever you want to pull latest code and restart the full stack (db, backend, frontend, harness).

Uses **docker-compose v1** (`docker-compose`, not `docker compose`). Run everything from the repo root.

Replace `/path/to/llmmo` with your clone path (e.g. `~/llmmo`).

---

## Redeploy (every time)

```bash
# 1. Go to the project
cd /path/to/llmmo

# 2. Pull latest code
git pull

# 3. Stop all four containers
docker-compose --profile agents stop db backend frontend harness

# 4. Remove all four containers
docker-compose --profile agents rm -f db backend frontend harness

# 5. Build and start all four containers
docker-compose --profile agents build db backend frontend harness
docker-compose --profile agents up -d db backend frontend harness

# 6. Follow harness logs
docker-compose --profile agents logs -f harness
```

Press `Ctrl+C` to stop following logs. The harness container keeps running.

---

## Container names

Compose names containers `{project}_{service}_1`. If the project directory is `llmmo`, the names are:

| Service   | Container name        |
|-----------|-----------------------|
| db        | `llmmo_db_1`          |
| backend   | `llmmo_backend_1`     |
| frontend  | `llmmo_frontend_1`    |
| harness   | `llmmo_harness_1`     |

Check with:

```bash
docker ps -a --filter name=llmmo_
```

---

## If compose fails with `ContainerConfig`

Older docker-compose v1 sometimes leaves ghost containers after `rm`. Remove them manually, then run step 5 again:

```bash
docker rm -f llmmo_db_1 llmmo_backend_1 llmmo_frontend_1 llmmo_harness_1
# also remove any hash-prefixed ghosts, e.g.:
# docker rm -f 626f682b6a68_llmmo_backend_1

docker-compose --profile agents up -d db backend frontend harness
```

Avoid `--force-recreate` on the full stack; it can try to recreate `db` unnecessarily.

---

## Notes

- **Data persists**: `db` and harness SQLite live in Docker volumes (`pgdata`, `harness-data`). Removing containers does not wipe the world or agent queue.
- **`.env`**: must exist on the minipc (`cp .env.example .env` on first setup). Not overwritten by `git pull`.
- **Harness needs `LLMMO_AGENT_KEY`** in `.env` (Agents tab in the UI).
- **Ollama** runs outside this stack on the host (`11434`); no container to restart here.
