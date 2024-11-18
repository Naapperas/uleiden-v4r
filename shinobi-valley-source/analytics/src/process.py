"""
"""

from typing import Callable, Any, Protocol, Concatenate
from functools import wraps
from pathlib import Path

from sqlalchemy import create_engine, select, func
from sqlalchemy.orm import Session

from models import Userdata, Timeseries

current_working_directory = Path.cwd()

db_file_path = f"sqlite:///{str(current_working_directory.parent.joinpath('server-assets', 'db.db'))}"

sqlite_engine = create_engine(db_file_path, echo=False)

HeaderInfo = dict[str, str]
MetricValues = dict[str, Any]

MetricResult = tuple[HeaderInfo, MetricValues]

class MetricCollector(Protocol):
    ''''''
    def __call__(self, _session: Session, *args: Any) -> MetricResult: ...

class MetricFile:
    def __init__(self, metric_name: str):
        self.metric_name = metric_name
        self.handle = open(str(current_working_directory.joinpath('data', f"{self.metric_name}.csv")), "w", encoding="utf-8")

    def __enter__(self):
        return self.handle

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.handle.close()

# def with_session[**P, R](f: Callable[[Session], R]) -> Callable[P, R]:
#     '''A type-safe decorator which provides a session.'''
# 
#     @wraps(f)
#     def inner(*args: P.args, **kwargs: P.kwargs) -> R:
#         # Provide the session as the first argument.
#         return f(session, *args, **kwargs)
#     
#     return inner

def perspective_distribution(_session: Session):
    
    statement = select(Userdata.perspective, func.count().label("num_participants"))\
        .group_by(Userdata.perspective)

    return {perspective: count for (perspective, count) in (_session.execute(statement))}

def positions_for_users(_session: Session):

    statement = select(Timeseries.userdata_id.label("user_id"), Timeseries.logline).where(Timeseries.logtype == "POSLOG")

    results = {}

    def process_logline(logline: str):
        parts = logline.split('_')

        position = parts[0]

        position = position[1:-1]

        return (dict(zip(['x', 'y', 'z'], map(float, position.split(', ')))),)


    for (user_id, logline) in (_session.execute(statement)):
        if user_id not in results:
            results[user_id] = []
    
        results[user_id].append(process_logline(logline))

    return results

def collected_items_per_user(_session: Session):
    statement = select(Timeseries.userdata_id.label("user_id"), func.count(Timeseries.logline).label("num_items"))\
        .where(Timeseries.logtype == "ITEMPICKUP")\
        .group_by(Timeseries.userdata_id)

    return {user_id: count for (user_id, count) in (_session.execute(statement))}


METRICS = {
    "perspective_distribution": perspective_distribution,
    "positions_for_users": positions_for_users,
    "collected_items_per_user": collected_items_per_user,
}

with Session(sqlite_engine) as session, session.begin():
    
    for metric_name, metric_collector in METRICS.items():
        with MetricFile(metric_name) as metric_file:
            metric_data = metric_collector(session)

            metric_file.write(str(metric_data))

    # print(perspective_distribution(session))
    # print(positions_for_users(session))
    # print(collected_items_per_user(session))
