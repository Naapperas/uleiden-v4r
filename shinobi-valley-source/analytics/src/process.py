# process_logs.py

import datetime
import math
from abc import ABC
from collections import Counter
from functools import reduce
from itertools import combinations
from operator import add
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Tuple

import matplotlib
import pandas as pd
import seaborn as sns

# Use 'Agg' backend for non-interactive environments
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from sqlalchemy import create_engine, select
from sqlalchemy.orm import Session

from models import (  # Ensure models.py is in the same directory
    Timeseries,
    Userdata,
)

# Define constants
USER_TIMESTAMP_FORMAT = "%Y-%m-%d %H:%M:%S"

# Define the current working directory and database file path
current_working_directory = Path.cwd()
db_file_path = f"sqlite:///{str(current_working_directory.parent.joinpath('server-assets', 'db.db'))}"

# Create the SQLAlchemy engine
sqlite_engine = create_engine(db_file_path, echo=False)

# Define the timestamp format
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

        # Some of the logs have a ',' instead of a '.' for floating point values, fix it
        def fix_floats(value):
            return value.replace(",", ".")

        self.position = tuple(map(float, position[1:-1].split(", ")))
        self.rotation = tuple(map(float, rotation[1:-1].split(", ")))

        rotation_euler_parts = rotation_euler.split(",")
        if len(rotation_euler_parts) > 2:  # multiple commas
            rotation_euler_parts = [
                f"{rotation_euler_parts[0]}.{rotation_euler_parts[1]}",
                f"{rotation_euler_parts[2]}.{rotation_euler_parts[3]}",
            ]  # TODO: remove this if this might blow up

        self.rotation_euler = tuple(map(float, rotation_euler_parts))

        self.pos_delta = float(fix_floats(pos_delta.split(":")[1]))
        self.rot_delta = float(fix_floats(rot_delta.split(":")[1]))
        self.path_distance = float(fix_floats(path_distance.split(":")[1]))
        self.jumping = bool(int(jumping.split(":")[1]))
        self.running = bool(int(running.split(":")[1]))


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


# Initialize bananas
bananas: Dict[int, Banana] = {
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

# Determine the minimum allowed distance squared between any two bananas
for pair in combinations(bananas.values(), 2):
    banana1, banana2 = pair
    dist_squared = banana1.dist_squared(banana2.pos)
    Banana.ALLOWED_DISTANCE_SQUARED = min(dist_squared, Banana.ALLOWED_DISTANCE_SQUARED)

# Equivalent to halving the "normal" distance, since we are always working with squared distances
Banana.ALLOWED_DISTANCE_SQUARED *= 0.25


def get_banana_positions(bananas: Dict[int, Banana]) -> pd.DataFrame:
    """Extracts banana positions into a DataFrame."""
    banana_data = []
    for banana in bananas.values():
        banana_data.append(
            {
                "banana_id": banana.index,
                "x": banana.pos[0],
                "y": banana.pos[1],
                "z": banana.pos[2],
            }
        )
    return pd.DataFrame(banana_data)


def parse_userdata(
    _users: Sequence[Userdata],
    usernames_with_endtimes: Dict[str, Optional[datetime.datetime]],
) -> Dict[int, Tuple[Userdata, List[TimeseriesLog]]]:
    _data: Dict[int, Tuple[Userdata, List[TimeseriesLog]]] = {}

    for _user in _users:
        if _user.endtime is None:
            possible_endtime = usernames_with_endtimes.get(_user.user, None)
            if possible_endtime is not None:
                _user.endtime = possible_endtime.strftime(USER_TIMESTAMP_FORMAT)

        timeseries_data: Sequence[Timeseries] = _user.timeseries_logs
        _user_id = _user.id
        timeseries_logs: List[TimeseriesLog] = []
        last_position_log: Optional[PositionLogging] = None

        for timeseries_log in timeseries_data:
            logtype = timeseries_log.logtype
            logline = timeseries_log.logline

            try:
                if logtype == "POSLOG":
                    _log = PositionLogging(_user_id, logline)
                    last_position_log = _log
                    timeseries_logs.append(_log)

                elif logtype == "TRIGGER_ROI_ENTER":
                    if logline == "Foraging_Banana" and last_position_log:
                        last_logged_position = last_position_log.position
                        for banana in bananas.values():
                            if banana.close(tuple(last_logged_position)):
                                timeseries_logs.append(
                                    BananaPickup(_user_id, banana.index)
                                )
                                break

                # Handle other log types if necessary

            except ValueError as e:
                print(f"Error processing {logtype} for user {_user.user}: {e}")

        _data[_user_id] = (_user, timeseries_logs)

    return _data


def extract_event_data(
    session: Session, user_ids: List[int]
) -> Tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    """
    Extract POSLOG entries from the Timeseries table
    for specific user_ids.
    Returns DataFrame: poslog_df
    """
    poslog_entries = (
        session.query(Timeseries)
        .filter(Timeseries.logtype == "POSLOG", Timeseries.userdata_id.in_(user_ids))
        .all()
    )

    poslog_data = []
    for entry in poslog_entries:
        try:
            pos_log = PositionLogging(entry.userdata_id, entry.logline)
            poslog_data.append(
                {
                    "user_id": pos_log.user_id,
                    "x": pos_log.position[0],
                    "y": pos_log.position[1],
                    "z": pos_log.position[2],
                    "timestamp": entry.timestamp,
                }
            )
        except ValueError as e:
            print(f"Error parsing POSLOG entry ID {entry.id}: {e}")

    # Create DataFrames
    poslog_df = pd.DataFrame(poslog_data)

    # Ensure 'user_id' is of type int
    poslog_df["user_id"] = pd.to_numeric(poslog_df["user_id"], errors="coerce")

    return poslog_df


def associate_perspectives(
    poslog_df: pd.DataFrame, userdata_df: pd.DataFrame
) -> pd.DataFrame:
    """Associate POSLOG entries with user perspectives."""
    # Ensure 'id' in userdata_df is numeric
    userdata_df["id"] = pd.to_numeric(userdata_df["id"], errors="coerce")

    # Drop entries where 'id' could not be converted to int
    initial_userdata_count = len(userdata_df)
    userdata_df.dropna(subset=["id"], inplace=True)
    final_userdata_count = len(userdata_df)
    dropped_userdata = initial_userdata_count - final_userdata_count
    if dropped_userdata > 0:
        print(f"Dropped {dropped_userdata} userdata entries due to invalid id.")

    userdata_df["id"] = userdata_df["id"].astype(int)

    # Merge POSLOG with userdata on 'user_id' and 'id'
    merged_df = poslog_df.merge(
        userdata_df, left_on="user_id", right_on="id", how="left"
    )

    # Drop entries without perspective
    merged_df.dropna(subset=["perspective"], inplace=True)

    # Ensure perspective column is uppercase
    merged_df["perspective"] = merged_df["perspective"].str.upper()

    return merged_df


def generate_heatmaps(
    merged_df: pd.DataFrame, banana_df: pd.DataFrame, total_users: int
):
    """Generate and save heatmaps for First Person and Third Person perspectives."""
    # Filter for First Person and Third Person
    first_person_df = merged_df[merged_df["perspective"] == "FIRSTPERSON"]
    third_person_df = merged_df[merged_df["perspective"] == "THIRDPERSON"]

    # Set the aesthetic style of the plots
    sns.set(style="white")

    # Define a function to plot heatmap and overlay event markers and bananas
    def plot_heatmap(df, title, ax, bananas=None):
        if df.empty:
            ax.set_title(f"{title} (No Data)")
            ax.set_xlabel("X Coordinate")
            ax.set_ylabel("Z Coordinate")
            return
        sns.kdeplot(
            x=df["x"],
            y=df["z"],
            cmap="Reds",
            fill=True,
            thresh=0.05,
            bw_adjust=0.5,
            ax=ax,
        )
        ax.set_title(title)
        ax.set_xlabel("X Coordinate")
        ax.set_ylabel("Z Coordinate")

        if bananas is not None and not bananas.empty:
            ax.scatter(
                bananas["x"],
                bananas["z"],
                c="yellow",
                marker="*",
                s=200,
                label="Banana",
                edgecolors="black",
            )
        ax.legend()

    # Create subplots
    fig, axes = plt.subplots(1, 2, figsize=(20, 10))

    # Plot First Person heatmap with events and bananas
    plot_heatmap(
        first_person_df, "First Person Perspective", axes[0], bananas=banana_df
    )

    # Plot Third Person heatmap with events and bananas
    plot_heatmap(
        third_person_df, "Third Person Perspective", axes[1], bananas=banana_df
    )

    plt.tight_layout()

    # Save the figure
    plt.savefig("heatmaps_with_bananas.png")
    plt.close()
    print("Heatmaps with banana locations have been saved.")


def main():
    # Create database tables if they don't exist (optional)
    # Base.metadata.create_all(sqlite_engine)

    # Create a new session
    with Session(sqlite_engine) as session:
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

        survey_user_ids = [
            1,
            9,
            11,
            12,
            18,
            19,
            21,
            23,
            24,
            22,
            25,
            26,
            28,
            42,
            54,
            7,
            14,
            16,
            15,
            27,
            37,
            38,
            41,
            56,
            62,
            68,
            70,
            71,
            72,
            78,
        ]

        # Initialize an empty dictionary to hold user data
        data: dict[int, tuple[Userdata, list[TimeseriesLog]]] = {}

        # Fetch users whose usernames are in survey_usernames_with_endtimes
        users_query = select(Userdata).where(
            Userdata.id.in_(survey_user_ids)
        )
        users_result = session.execute(users_query)

        users = users_result.scalars().all()

        # Parse userdata and associated logs
        data = parse_userdata(users, survey_usernames_with_endtimes)
        total_users = len(data)
        print(f"Results for {total_users}/{len(survey_usernames_with_endtimes)} users:")

        banana_pickup_counter = Counter()

        # Process each user's logs
        for user_data in data.values():

            banana_pickups: list[int] = []

            user, user_logs = user_data

            for log in user_logs:
                if isinstance(log, BananaPickup):
                    banana_pickups.append(log.banana_id)
                    banana_pickup_counter[log.banana_id] += 1

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
                    average_bananas_per_second = seconds / len(banana_pickups)
                    delta_avg = datetime.timedelta(seconds=average_bananas_per_second)

                    print(
                        f"\tUser {user.user} picked on average one banana every {delta_avg}"
                    )
                else:
                    print(f"\tUser {user.user} didn't pick any bananas")

            else:
                print(f"\tCannot calculate game duration for user {user.user}")
                print(
                    f"\tCannot calculate average banana pick rate for user {user.user}"
                )

        print(f"\nBanana pick counts: {banana_pickup_counter}")

        # **Collect user_ids for survey users**
        user_ids = list(data.keys())

        # Extract event data for these user_ids
        poslog_df = extract_event_data(session, user_ids)

        # Fetch all userdata for merging
        userdata_query = select(Userdata).where(Userdata.id.in_(user_ids))
        userdata_df = pd.read_sql(userdata_query, sqlite_engine)

        # Associate perspectives
        merged_df = associate_perspectives(poslog_df, userdata_df)
        print(merged_df)
        print(merged_df.columns)

        # Extract banana positions into a DataFrame
        banana_df = get_banana_positions(bananas)
        print(banana_df)

        # Generate and save Heatmaps with Events and Bananas
        generate_heatmaps(merged_df, banana_df, total_users)


if __name__ == "__main__":
    main()
