import json
import sqlite3
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

from llmmo_harness.schema import CommandPlan


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


@dataclass
class QueuedCommand:
    id: int
    plan_id: int
    seq: int
    command_json: str
    status: str
    city_id: str | None


class CommandQueue:
    def __init__(self, db_path: Path) -> None:
        self.db_path = db_path
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._init_db()

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self.db_path)
        connection.row_factory = sqlite3.Row
        return connection

    def _init_db(self) -> None:
        with self._connect() as connection:
            connection.executescript(
                """
                CREATE TABLE IF NOT EXISTS plans (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    created_at TEXT NOT NULL,
                    observed_at_tick INTEGER NOT NULL,
                    planner_type TEXT NOT NULL,
                    raw_json TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS command_queue (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    plan_id INTEGER NOT NULL,
                    seq INTEGER NOT NULL,
                    command_json TEXT NOT NULL,
                    status TEXT NOT NULL,
                    city_id TEXT,
                    FOREIGN KEY (plan_id) REFERENCES plans(id)
                );

                CREATE TABLE IF NOT EXISTS execution_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    queue_id INTEGER,
                    executed_at TEXT NOT NULL,
                    ok INTEGER NOT NULL,
                    http_status INTEGER,
                    request_json TEXT NOT NULL,
                    response_json TEXT,
                    error_message TEXT,
                    FOREIGN KEY (queue_id) REFERENCES command_queue(id)
                );
                """
            )

    def enqueue_plan(
        self,
        plan: CommandPlan,
        planner_type: str,
        city_id: str | None,
    ) -> int:
        raw_json = plan.model_dump_json()
        with self._connect() as connection:
            connection.execute(
                "DELETE FROM command_queue WHERE status = 'pending'"
            )
            cursor = connection.execute(
                """
                INSERT INTO plans (created_at, observed_at_tick, planner_type, raw_json)
                VALUES (?, ?, ?, ?)
                """,
                (_utc_now(), plan.observedAtTick, planner_type, raw_json),
            )
            plan_id = cursor.lastrowid
            for seq, command in enumerate(plan.commands):
                connection.execute(
                    """
                    INSERT INTO command_queue (plan_id, seq, command_json, status, city_id)
                    VALUES (?, ?, ?, 'pending', ?)
                    """,
                    (plan_id, seq, command.model_dump_json(), city_id),
                )
            connection.commit()
            return plan_id

    def pop_pending(self, limit: int = 1) -> list[QueuedCommand]:
        with self._connect() as connection:
            rows = connection.execute(
                """
                SELECT id, plan_id, seq, command_json, status, city_id
                FROM command_queue
                WHERE status = 'pending'
                ORDER BY plan_id ASC, seq ASC
                LIMIT ?
                """,
                (limit,),
            ).fetchall()
            return [
                QueuedCommand(
                    id=row["id"],
                    plan_id=row["plan_id"],
                    seq=row["seq"],
                    command_json=row["command_json"],
                    status=row["status"],
                    city_id=row["city_id"],
                )
                for row in rows
            ]

    def mark_status(self, queue_id: int, status: str) -> None:
        with self._connect() as connection:
            connection.execute(
                "UPDATE command_queue SET status = ? WHERE id = ?",
                (status, queue_id),
            )
            connection.commit()

    def log_execution(
        self,
        queue_id: int | None,
        ok: bool,
        http_status: int | None,
        request: dict,
        response: dict | str | None,
        error_message: str | None = None,
    ) -> None:
        response_json = None
        if response is not None:
            response_json = (
                json.dumps(response) if isinstance(response, dict) else str(response)
            )
        with self._connect() as connection:
            connection.execute(
                """
                INSERT INTO execution_log
                    (queue_id, executed_at, ok, http_status, request_json, response_json, error_message)
                VALUES (?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    queue_id,
                    _utc_now(),
                    1 if ok else 0,
                    http_status,
                    json.dumps(request),
                    response_json,
                    error_message,
                ),
            )
            connection.commit()

    def recent_logs(self, limit: int = 20) -> list[sqlite3.Row]:
        with self._connect() as connection:
            return connection.execute(
                """
                SELECT id, queue_id, executed_at, ok, http_status,
                       request_json, response_json, error_message
                FROM execution_log
                ORDER BY id DESC
                LIMIT ?
                """,
                (limit,),
            ).fetchall()

    def pending_count(self) -> int:
        with self._connect() as connection:
            row = connection.execute(
                "SELECT COUNT(*) AS count FROM command_queue WHERE status = 'pending'"
            ).fetchone()
            return int(row["count"])
