# Minipc redeploy

Run these commands on the minipc whenever you want to pull latest code and restart the full stack (db, backend, frontend, harness).

Uses **docker-compose v1** (`docker-compose`, not `docker compose`). Run everything from the repo root.

On this host, Docker requires **`sudo`** (user `minipcuser` is not in the `docker` group).

---

## Redeploy (every time)

```bash
# 1. Go to the project
cd ~/projects/llmmo

# 2. Pull latest code
git pull

# 3. Stop all four containers
sudo docker-compose --profile agents stop db backend frontend harness

# 4. Remove all four containers
sudo docker-compose --profile agents rm -f db backend frontend harness

# 5. Build and start all four containers
sudo docker-compose --profile agents build db backend frontend harness
sudo docker-compose --profile agents up -d db backend frontend harness

# 6. Follow harness logs
sudo docker-compose --profile agents logs -f harness
```

Press `Ctrl+C` to stop following logs. The harness container keeps running.

---

## Container names

Compose names containers `{project}_{service}_1`. For this project:

| Service   | Container name        |
|-----------|-----------------------|
| db        | `llmmo_db_1`          |
| backend   | `llmmo_backend_1`     |
| frontend  | `llmmo_frontend_1`    |
| harness   | `llmmo_harness_1`     |

Check with:

```bash
sudo docker ps -a --filter name=llmmo_
```

---

## If compose fails with `ContainerConfig`

Older docker-compose v1 sometimes leaves ghost containers after `rm`. Remove them manually, then run step 5 again:

```bash
sudo docker rm -f llmmo_db_1 llmmo_backend_1 llmmo_frontend_1 llmmo_harness_1
# also remove any hash-prefixed ghosts, e.g.:
# sudo docker rm -f 626f682b6a68_llmmo_backend_1

sudo docker-compose --profile agents up -d db backend frontend harness
```

Avoid `--force-recreate` on the full stack; it can try to recreate `db` unnecessarily.

---

## Harness: `Connection refused`

Both `plan` and `execute` talk to the backend at `http://backend:8080/api/v1` inside Docker. Ollama is reachable by container name at `http://ollama:11434` on the shared Docker network.

**1. Check backend is up and healthy**

```bash
sudo docker-compose ps
sudo docker-compose logs --tail=80 backend
curl -s http://localhost:5080/health
```

Backend should show `healthy`. If `curl` fails or logs show errors, fix backend first (often missing `.env` values like `POSTGRES_PASSWORD` or `JWT_SECRET`).

**2. Check Ollama from the harness container** (plan only; execute does not need it)

```bash
sudo docker exec llmmo_harness_1 python -c "import httpx; print(httpx.get('http://ollama:11434/api/tags', timeout=10).status_code)"
```

If that fails, the `ollama` container is down or not on the shared Docker network.

**3. Rebuild harness** after config changes (Ollama URL is baked into the image)

```bash
sudo docker-compose --profile agents build harness
sudo docker-compose --profile agents up -d harness
sudo docker-compose --profile agents logs -f harness
```

You should see `API ready at http://backend:8080/api/v1` before the first plan/execute cycle.

---

## Permission denied on Docker socket

If you see `PermissionError: [Errno 13] Permission denied` when running `docker-compose` without sudo, use `sudo` as shown above.

Optional one-time fix (log out and back in afterward):

```bash
sudo usermod -aG docker $USER
```

---

## Notes

- **Data persists**: `db` and harness SQLite live in Docker volumes (`pgdata`, `harness-data`). Removing containers does not wipe the world or agent queue.
- **`.env`**: must exist on the minipc (`cp .env.example .env` on first setup). Not overwritten by `git pull`.
- **Harness needs `LLMMO_AGENT_KEY`** in `.env` (Agents tab in the UI).
- **Ollama** runs outside this stack on the host (`11434`); no container to restart here.
