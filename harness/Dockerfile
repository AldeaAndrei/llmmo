# syntax=docker/dockerfile:1

FROM python:3.11-slim AS runtime
WORKDIR /app

ENV PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1

# Install the package first for better layer caching.
# README.md is referenced by pyproject.toml and required at build time.
COPY pyproject.toml README.md ./
COPY llmmo_harness ./llmmo_harness
RUN pip install --no-cache-dir .

# Plans (used by the mock planner) and the container config.
COPY plans ./plans
COPY config.docker.yaml ./config.docker.yaml

# SQLite queue lives on a mounted volume.
RUN mkdir -p /data

CMD ["python", "-m", "llmmo_harness.cli", "run", "-c", "config.docker.yaml"]
