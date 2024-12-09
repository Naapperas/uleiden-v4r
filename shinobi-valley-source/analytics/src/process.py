# pylint: disable=missing-class-docstring,missing-function-docstring
"""
"""

from pathlib import Path
from functools import reduce
from itertools import combinations
from collections import Counter
from operator import add
import datetime
import math
from typing import Sequence, Optional
from abc import ABC

from sqlalchemy import create_engine, select
from sqlalchemy.orm import Session

from models import Userdata, Timeseries

current_working_directory = Path.cwd()

db_file_path = f"sqlite:///{str(current_working_directory.parent.joinpath('server-assets', 'db.db'))}"

sqlite_engine = create_engine(db_file_path, echo=False)

USER_TIMESTAMP_FORMAT = "%Y-%m-%d %H:%M:%S"


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
    _users: Sequence[Userdata], usernames_with_endtimes: dict[str, datetime.datetime]
) -> dict[int, tuple[Userdata, list[TimeseriesLog]]]:
    _data: dict[int, tuple[Userdata, list[TimeseriesLog]]] = {}

    for _user in _users:

        if _user.endtime is None:

            possible_endtime = usernames_with_endtimes.get(_user.user, None)

            if possible_endtime is not None:
                _user.endtime = possible_endtime.strftime(USER_TIMESTAMP_FORMAT)

        timeseries_data: Sequence[Timeseries] = _user.timeseries_logs

        _user_id = _user.id

        timeseries_logs: list[TimeseriesLog] = []
        last_position_log: Optional[PositionLogging] = None
        for timeseries_log in timeseries_data:
            match timeseries_log.logtype:
                case "POSLOG":

                    _log = PositionLogging(_user_id, timeseries_log.logline)

                    last_position_log = _log
                    timeseries_logs.append(_log)
                case "TRIGGER_ROI_ENTER":
                    match timeseries_log.logline:
                        case "Foraging_Banana":

                            last_logged_position = last_position_log.position

                            for banana in bananas.values():
                                if banana.close(tuple(last_logged_position)):

                                    timeseries_logs.append(
                                        BananaPickup(_user_id, banana.index)
                                    )
                                    break

                        case _:
                            pass
                case _:
                    pass

        _data[_user_id] = (_user, timeseries_logs)

    return _data


with Session(sqlite_engine) as session, session.begin():

    survey_usernames_with_endtimes = {
        "4798594B": None,
        "4BB5EA24": datetime.datetime(2024, 12, 8, 16, 29),
        "2A9639A5": datetime.datetime(2024, 12, 8, 16, 29),
        "696C2597": datetime.datetime(2024, 12, 8, 14, 47),
        "D19BDA5A": datetime.datetime(2024, 12, 7, 11, 20),
        "CCD8974B": None,
        "4D68BDE": datetime.datetime(2024, 12, 6, 21, 10),
        "7588E46C": datetime.datetime(2024, 12, 6, 21, 5),
        "6503A955": datetime.datetime(2024, 12, 6, 21, 10),
        "BF48A639": None,
        "8E2BF216": None,
        "3D5671FC": None,
        "DCEE048E": None,
        "A6CE844F": datetime.datetime(2024, 12, 5, 17, 46),
        "452A633D": None,
        "F2959E00": datetime.datetime(2024, 12, 5, 12, 7),
        "8778A580": datetime.datetime(2024, 12, 5, 11, 52),
        "EDAC146": datetime.datetime(2024, 12, 5, 11, 41),
        "4DE88833": datetime.datetime(2024, 12, 5, 11, 35),
        "F1555EEE": datetime.datetime(2024, 12, 5, 11, 38),
        "275FC505": datetime.datetime(2024, 12, 5, 11, 36),
        "B39628C": datetime.datetime(2024, 12, 5, 11, 34),
        "8FB73E2": datetime.datetime(2024, 12, 5, 11, 17),
        "850F2ABB": datetime.datetime(2024, 12, 5, 11, 24),
        "E85F2688": datetime.datetime(2024, 12, 5, 11, 16),
        "816CBFD0": datetime.datetime(2024, 12, 5, 11, 17),
        "F0867DFE": datetime.datetime(2024, 12, 5, 11, 10),
        "E53E0FF7": None,
        "4B7ADA93": datetime.datetime(2024, 12, 4, 12, 1),
        "778FC0DD": datetime.datetime(2024, 12, 3, 21, 53),
        "C22A0327": None,
        "7243788C": datetime.datetime(2024, 12, 3, 15, 18),
        "7F281BBB": datetime.datetime(2024, 12, 5, 11, 19),
    }

    data: dict[int, tuple[Userdata, list[TimeseriesLog]]] = {}

    users = session.execute(
        select(Userdata).where(Userdata.user.in_(survey_usernames_with_endtimes.keys()))
    )

    data = parse_userdata(
        map(lambda r: r.Userdata, users), survey_usernames_with_endtimes
    )

    print(f"Results for {len(data)} users:")

    banana_pickup_counter = Counter()

    for user_id, user_data in data.items():

        banana_pickups: list[id] = []

        user, user_logs = user_data

        for log in user_logs:
            match log:
                case BananaPickup() as banana_pickup:
                    banana_pickups.append(banana_pickup.banana_id)
                    banana_pickup_counter[banana_pickup.banana_id] += 1
                case _:
                    pass

        print(f"\n\tUser {user.user} picked bananas {banana_pickups}")

        if user.endtime is not None:

            delta = datetime.datetime.strptime(
                user.endtime, USER_TIMESTAMP_FORMAT
            ) - datetime.datetime.strptime(user.starttime, USER_TIMESTAMP_FORMAT)
            seconds = delta.total_seconds()
            seconds %= (
                60 * 60
            )  # remove hours since we know for a fact no-one played for over an hour
            delta = datetime.timedelta(seconds=seconds)

            print(
                f"\tUser {user.user} played from {user.starttime} until {user.endtime}, totaling {delta}"
            )

            if len(banana_pickups) > 0:
                average_bananas_per_second = seconds // len(banana_pickups)
                delta = delta = datetime.timedelta(seconds=average_bananas_per_second)

                print(f"\tUser {user.user} picked on average one banana every {delta}")
            else:
                print(f"\tUser {user.user} didn't pick any bananas")

        else:
            print(f"\tCannot calculate game duration for user {user.user}")
            print(f"\tCannot calculate average banana pick rate for user {user.user}")

    print(f"\nBanana pick counts: {banana_pickup_counter}")
