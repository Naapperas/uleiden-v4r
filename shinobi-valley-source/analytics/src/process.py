# pylint: disable=missing-class-docstring,missing-function-docstring
"""
"""

from pathlib import Path
from functools import reduce
from itertools import combinations
from operator import add
import math
from typing import Sequence, Optional
from abc import ABC

from sqlalchemy import create_engine, select
from sqlalchemy.orm import Session

from models import Userdata, Timeseries

current_working_directory = Path.cwd()

db_file_path = f"sqlite:///{str(current_working_directory.parent.joinpath('server-assets', 'db.db'))}"

sqlite_engine = create_engine(db_file_path, echo=False)


class TimeseriesLog(ABC):
    def __init__(self, user_id: int):
        self.user_id = user_id


class PositionLogging(TimeseriesLog):
    def __init__(self, _user_id: int, logline: str):
        super().__init__(_user_id)
        self.process_logline(logline)

    def process_logline(self, logline: str):
        parts = logline.split("_")

        position = parts[0]
        rotation = parts[1]
        rotation_euler = parts[2]
        pos_delta = parts[3]
        rot_delta = parts[4]
        path_distance = parts[5]
        jumping = parts[6]
        running = parts[7]

        # some of the logs have a ',' instead of a '.' for floating point values, fix it
        def fix_floats(value):
            return value.replace(",", ".")

        self.position = tuple(map(float, position[1:-1].split(", ")))
        self.rotation = tuple(map(float, rotation[1:-1].split(", ")))
        self.rotation_euler = tuple(rotation_euler.split(","))
        self.pos_delta = float(fix_floats(pos_delta.split(":")[1]))
        self.rot_delta = float(fix_floats(rot_delta.split(":")[1]))
        self.path_distance = float(fix_floats(path_distance.split(":")[1]))
        self.jumping = bool(jumping)
        self.running = bool(running)


class BananaPickup(TimeseriesLog):
    def __init__(self, user_id: int, banana_id: int):
        super().__init__(user_id)

        self.banana_id = banana_id


class Banana:

    INDEX: int = 1
    ALLOWED_DISTANCE_SQUARED: float = math.inf

    BASE_POSITION: tuple[float, float, float] = (-20.03064, 36.94436, -6.575068)

    def __init__(self, pos: tuple[float, float, float]):
        self.index = Banana.INDEX
        Banana.INDEX += 1

        self.pos = tuple(
            map(lambda pair: add(pair[0], pair[1]), zip(Banana.BASE_POSITION, pos))
        )

    def __str__(self):
        return f"Banana {self.index} at {self.pos}"

    def __repr__(self):
        return f"Banana {self.index} at {self.pos}"

    def dist_squared(self, other_pos: tuple[float, float, float]) -> float:
        return reduce(
            add,
            map(
                lambda coordPair: (coordPair[0] - coordPair[1]) ** 2,
                zip(self.pos, other_pos),
            ),
            0,
        )

    def close(self, other_pos: tuple[float, float, float]) -> bool:

        _dist_squared = self.dist_squared(other_pos)

        return _dist_squared <= Banana.ALLOWED_DISTANCE_SQUARED


bananas: dict[int, Banana] = {
    banana.index: banana
    for banana in [
        Banana((87.01, -5.39, 43.25)),
        Banana((50.45, 1.81, 18.4)),
        Banana((46.93, 0.69, -90.28)),
        Banana((102, 29, -20.21)),
        Banana((27.65, -8.286, 79.57)),
        Banana((-32.08, -1.49, 63.04)),
        Banana((-20.13, -8.96, -0.81)),
        Banana((-53.55, 34.85, 98.91)),
        Banana((-66.786, -4.873, 23.983)),
    ]
}

for pair in combinations(bananas.values(), 2):

    banana1, banana2 = pair

    dist_squared = banana1.dist_squared(banana2.pos)

    Banana.ALLOWED_DISTANCE_SQUARED = min(dist_squared, Banana.ALLOWED_DISTANCE_SQUARED)

# equivalent to halving the "normal" distance, since we are always working with squared distances
Banana.ALLOWED_DISTANCE_SQUARED *= 0.25


def parse_userdata(
    users: Sequence[Userdata],
) -> dict[int, tuple[Userdata, list[TimeseriesLog]]]:
    data: dict[int, tuple[Userdata, list[TimeseriesLog]]] = {}

    for user in users:
        timeseries_data: Sequence[Timeseries] = user.timeseries_logs

        user_id = user.id

        timeseries_logs: list[TimeseriesLog] = []
        last_position_log: Optional[PositionLogging] = None
        for timeseries_log in timeseries_data:
            match timeseries_log.logtype:
                case "POSLOG":

                    log = PositionLogging(user_id, timeseries_log.logline)

                    last_position_log = log
                    timeseries_logs.append(log)
                case "TRIGGER_ROI_ENTER":
                    match timeseries_log.logline:
                        case "Foraging_Banana":

                            last_logged_position = last_position_log.position

                            for banana in bananas.values():
                                if banana.close(tuple(last_logged_position)):

                                    timeseries_logs.append(
                                        BananaPickup(user_id, banana.index)
                                    )
                                    break

                        case _:
                            pass
                case _:
                    pass

        data[user_id] = (user, timeseries_logs)

    return data


with Session(sqlite_engine) as session, session.begin():

    survey_usernames = [
        "4798594B",
        "4BB5EA24",
        "2A9639A5",
        "696C2597",
        "D19BDA5A",
        "CCD8974B",
        "4D68BDE",
        "7588E46C",
        "6503A955",
        "BF48A639",
        "8E2BF216",
        "3D5671FC",
        "DCEE048E",
        "A6CE844F",
        "452A633D",
        "F2959E00",
        "8778A580",
        "EDAC146",
        "4DE88833",
        "F1555EEE",
        "275FC505",
        "B39628C",
        "8FB73E2",
        "850F2ABB",
        "E85F2688",
        "816CBFD0",
        "F0867DFE",
        "E53E0FF7",
        "4B7ADA93",
        "778FC0DD",
        "C22A0327",
        "7243788C",
    ]

    data: dict[int, tuple[Userdata, list[TimeseriesLog]]] = {}

    users = session.execute(select(Userdata).where(Userdata.user.in_(survey_usernames)))

    data = parse_userdata(map(lambda r: r.Userdata, users))

    print(f"Results for {len(data)} users:")

    print("\tBanana pickups:")
    for user_id, user_data in data.items():

        banana_pickups: list[id] = []

        user, user_logs = user_data

        for log in user_logs:
            match log:
                case BananaPickup() as banana_pickup:
                    banana_pickups.append(banana_pickup.banana_id)
                case _:
                    pass

        print(f"\t\tUser {user_id} picked bananas {banana_pickups}")
