from typing import Annotated, Literal, Union

from pydantic import BaseModel, Field, field_validator

DEFAULT_BUILDING_TYPES = frozenset(
    {
        "gold_mine",
        "stone_mine",
        "timber_station",
        "bakery",
        "storage_shed",
        "barracks",
        "spy_academy",
        "wall",
    }
)

BUILDING_TYPES = DEFAULT_BUILDING_TYPES

TROOP_TYPES = frozenset({"soldier", "spy"})

COMMAND_TYPES = frozenset({
    "upgrade",
    "train",
    "attack",
    "message",
    "ally",
    "enemy",
    "clear_relation",
})


def set_building_types(types: set[str] | frozenset[str]) -> None:
    global BUILDING_TYPES
    BUILDING_TYPES = frozenset(types)


class UpgradeCommand(BaseModel):
    type: Literal["upgrade"] = "upgrade"
    buildingType: str
    reason: str = Field(min_length=1, max_length=500)

    @field_validator("buildingType")
    @classmethod
    def validate_building(cls, value: str) -> str:
        if value not in BUILDING_TYPES:
            raise ValueError(f"Invalid buildingType: {value}")
        return value


class TrainCommand(BaseModel):
    type: Literal["train"] = "train"
    troopType: str
    count: int = Field(default=1, ge=1)
    reason: str = Field(min_length=1, max_length=500)

    @field_validator("troopType")
    @classmethod
    def validate_troop(cls, value: str) -> str:
        if value not in TROOP_TYPES:
            raise ValueError(f"Invalid troopType: {value}")
        return value


class AttackCommand(BaseModel):
    type: Literal["attack"] = "attack"
    targetCityId: str
    troopType: str = "soldier"
    count: int = 1
    reason: str = Field(min_length=1, max_length=500)

    @field_validator("troopType")
    @classmethod
    def validate_troop(cls, value: str) -> str:
        if value not in TROOP_TYPES:
            raise ValueError(f"Invalid troopType: {value}")
        return value

    @field_validator("count")
    @classmethod
    def validate_count(cls, value: int) -> int:
        if value != 1:
            raise ValueError("count must be 1 for attack commands")
        return value


class MessageCommand(BaseModel):
    type: Literal["message"] = "message"
    toPlayerId: str
    subject: str = Field(min_length=1, max_length=100)
    body: str = Field(min_length=1, max_length=500)
    reason: str = Field(min_length=1, max_length=500)


class AllyCommand(BaseModel):
    type: Literal["ally"] = "ally"
    toPlayerId: str
    reason: str = Field(min_length=1, max_length=500)


class EnemyCommand(BaseModel):
    type: Literal["enemy"] = "enemy"
    toPlayerId: str
    reason: str = Field(min_length=1, max_length=500)


class ClearRelationCommand(BaseModel):
    type: Literal["clear_relation"] = "clear_relation"
    toPlayerId: str
    reason: str = Field(min_length=1, max_length=500)


Command = Annotated[
    Union[
        UpgradeCommand,
        TrainCommand,
        AttackCommand,
        MessageCommand,
        AllyCommand,
        EnemyCommand,
        ClearRelationCommand,
    ],
    Field(discriminator="type"),
]


class CommandPlan(BaseModel):
    schemaVersion: int = 1
    observedAtTick: int = 0
    commands: list[Command] = Field(default_factory=list)

    @field_validator("schemaVersion")
    @classmethod
    def validate_schema_version(cls, value: int) -> int:
        if value != 1:
            raise ValueError("Only schemaVersion 1 is supported")
        return value
