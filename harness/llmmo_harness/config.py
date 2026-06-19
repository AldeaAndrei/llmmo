import os
from dataclasses import dataclass, field
from pathlib import Path

import yaml


@dataclass
class ApiConfig:
    base_url: str = "http://localhost:5000/api/v1"
    api_key_env: str = "LLMMO_AGENT_KEY"
    api_key: str | None = None


@dataclass
class OllamaConfig:
    base_url: str = "http://localhost:11434/v1"
    model: str = "llama3.1"
    temperature: float = 0.2
    request_timeout_seconds: float = 300.0
    log_prompts: bool = False


@dataclass
class PlannerConfig:
    type: str = "mock"
    mock_plan_path: str = "plans/mock_default.json"
    ollama: OllamaConfig = field(default_factory=OllamaConfig)


@dataclass
class ScheduleConfig:
    plan_interval_seconds: int = 120
    execute_interval_seconds: int = 10
    max_commands_per_execute: int = 1


@dataclass
class DatabaseConfig:
    path: str = "data/harness.db"


@dataclass
class HarnessConfig:
    api: ApiConfig = field(default_factory=ApiConfig)
    planner: PlannerConfig = field(default_factory=PlannerConfig)
    schedule: ScheduleConfig = field(default_factory=ScheduleConfig)
    database: DatabaseConfig = field(default_factory=DatabaseConfig)
    config_dir: Path = field(default_factory=Path.cwd)

    def resolve_api_key(self) -> str:
        if self.api.api_key:
            return self.api.api_key
        key = os.environ.get(self.api.api_key_env, "").strip()
        if not key:
            raise ValueError(
                f"API key not set. Export {self.api.api_key_env} or set api.api_key in config."
            )
        return key

    def resolve_path(self, relative: str) -> Path:
        path = Path(relative)
        if path.is_absolute():
            return path
        return self.config_dir / path


def load_config(path: Path) -> HarnessConfig:
    path = path.resolve()
    with path.open(encoding="utf-8") as handle:
        raw = yaml.safe_load(handle) or {}

    api_raw = raw.get("api") or {}
    planner_raw = raw.get("planner") or {}
    ollama_raw = planner_raw.get("ollama") or {}
    schedule_raw = raw.get("schedule") or {}
    database_raw = raw.get("database") or {}

    return HarnessConfig(
        api=ApiConfig(
            base_url=api_raw.get("base_url", ApiConfig.base_url),
            api_key_env=api_raw.get("api_key_env", ApiConfig.api_key_env),
            api_key=api_raw.get("api_key"),
        ),
        planner=PlannerConfig(
            type=planner_raw.get("type", "mock"),
            mock_plan_path=planner_raw.get("mock_plan_path", "plans/mock_default.json"),
            ollama=OllamaConfig(
                base_url=ollama_raw.get("base_url", OllamaConfig.base_url),
                model=ollama_raw.get("model", OllamaConfig.model),
                temperature=float(ollama_raw.get("temperature", 0.2)),
                request_timeout_seconds=float(
                    ollama_raw.get(
                        "request_timeout_seconds",
                        OllamaConfig.request_timeout_seconds,
                    )
                ),
                log_prompts=bool(ollama_raw.get("log_prompts", False)),
            ),
        ),
        schedule=ScheduleConfig(
            plan_interval_seconds=int(
                schedule_raw.get("plan_interval_seconds", 120)
            ),
            execute_interval_seconds=int(
                schedule_raw.get("execute_interval_seconds", 10)
            ),
            max_commands_per_execute=int(
                schedule_raw.get("max_commands_per_execute", 1)
            ),
        ),
        database=DatabaseConfig(path=database_raw.get("path", "data/harness.db")),
        config_dir=path.parent,
    )
