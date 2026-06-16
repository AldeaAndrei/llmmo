from typing import Annotated, Literal, Union

from pydantic import BaseModel, Field, field_validator

BUILDING_TYPES = frozenset(
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

TROOP_TYPES = frozenset({"soldier", "spy"})

COMMAND_TYPES = frozenset({"upgrade", "train"})


class UpgradeCommand(BaseModel):
    type: Literal["upgrade"] = "upgrade"
    buildingType: str

    @field_validator("buildingType")
    @classmethod
    def validate_building(cls, value: str) -> str:
        if value not in BUILDING_TYPES:
            raise ValueError(f"Invalid buildingType: {value}")
        return value


class TrainCommand(BaseModel):
    type: Literal["train"] = "train"
    troopType: str
    count: int = 1

    @field_validator("troopType")
    @classmethod
    def validate_troop(cls, value: str) -> str:
        if value not in TROOP_TYPES:
            raise ValueError(f"Invalid troopType: {value}")
        return value

    @field_validator("count")
    @classmethod
    def validate_count(cls, value: int) -> int:
        if value <= 0:
            raise ValueError("count must be greater than zero")
        return value


Command = Annotated[
    Union[UpgradeCommand, TrainCommand],
    Field(discriminator="type"),
]


class CommandPlan(BaseModel):
    schemaVersion: int = 1
    observedAtTick: int = 0
    commands: list[UpgradeCommand | TrainCommand] = Field(default_factory=list)

    @field_validator("schemaVersion")
    @classmethod
    def validate_schema_version(cls, value: int) -> int:
        if value != 1:
            raise ValueError("Only schemaVersion 1 is supported")
        return value
