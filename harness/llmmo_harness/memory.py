import json
import sqlite3
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


# Keep enough history that each specialist agent can still see its own
# last couple of decisions even when other agents act on the same tick.
RECENT_RETENTION = 10


@dataclass
class DecisionRecord:
    tick: int
    action: dict
    reason: str


class DecisionMemory:
    def __init__(self, db_path: Path) -> None:
        self.db_path = db_path
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._init_db()

    def _connect(self) -> sqlite3.Connection:
        db_str = str(self.db_path)
        uri = db_str.startswith("file:")
        connection = sqlite3.connect(db_str, uri=uri)
        connection.row_factory = sqlite3.Row
        return connection

    def _init_db(self) -> None:
        with self._connect() as connection:
            connection.executescript(
                """
                CREATE TABLE IF NOT EXISTS decision_memory (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    city_id TEXT NOT NULL,
                    tick INTEGER NOT NULL,
                    action_json TEXT NOT NULL,
                    reason TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_decision_memory_city_id
                    ON decision_memory(city_id, id DESC);
                """
            )

    def record(self, city_id: str, tick: int, action: dict, reason: str) -> None:
        action_json = json.dumps(action, separators=(",", ":"))
        with self._connect() as connection:
            connection.execute(
                """
                INSERT INTO decision_memory (city_id, tick, action_json, reason, created_at)
                VALUES (?, ?, ?, ?, ?)
                """,
                (city_id, tick, action_json, reason, _utc_now()),
            )
            connection.execute(
                """
                DELETE FROM decision_memory
                WHERE city_id = ?
                  AND id NOT IN (
                    SELECT id FROM decision_memory
                    WHERE city_id = ?
                    ORDER BY id DESC
                    LIMIT ?
                  )
                """,
                (city_id, city_id, RECENT_RETENTION),
            )

    def get_recent(self, city_id: str, limit: int = 2) -> list[DecisionRecord]:
        with self._connect() as connection:
            rows = connection.execute(
                """
                SELECT tick, action_json, reason
                FROM decision_memory
                WHERE city_id = ?
                ORDER BY id DESC
                LIMIT ?
                """,
                (city_id, limit),
            ).fetchall()

        records: list[DecisionRecord] = []
        for row in rows:
            records.append(
                DecisionRecord(
                    tick=row["tick"],
                    action=json.loads(row["action_json"]),
                    reason=row["reason"],
                )
            )
        return records

    def get_recent_by_types(
        self,
        city_id: str,
        types: set[str] | frozenset[str],
        limit: int = 2,
    ) -> list[DecisionRecord]:
        """Most recent decisions whose action type is in `types` (newest first)."""
        wanted = {t.lower() for t in types}
        with self._connect() as connection:
            rows = connection.execute(
                """
                SELECT tick, action_json, reason
                FROM decision_memory
                WHERE city_id = ?
                ORDER BY id DESC
                LIMIT ?
                """,
                (city_id, RECENT_RETENTION),
            ).fetchall()

        records: list[DecisionRecord] = []
        for row in rows:
            action = json.loads(row["action_json"])
            if str(action.get("type", "")).lower() not in wanted:
                continue
            records.append(
                DecisionRecord(
                    tick=row["tick"],
                    action=action,
                    reason=row["reason"],
                )
            )
            if len(records) >= limit:
                break
        return records
