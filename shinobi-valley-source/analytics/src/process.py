from sqlalchemy import create_engine, select, func
from sqlalchemy.orm import Session

from models import Userdata, Timeseries

from pathlib import Path

current_working_directory = Path.cwd()
sqlite_engine = create_engine(f"sqlite:///{str(current_working_directory.parent.joinpath('server-assets', 'db.db'))}", echo=False)

def perspective_distribution(_session: Session):
    
    statement = select(Userdata.perspective, func.count().label("num_participants"))\
        .group_by(Userdata.perspective)

    return {perspective: count for (perspective, count) in (_session.execute(statement))}

def positions_for_user(_session: Session, user_id):
    statement = select(Timeseries.logline).where((Timeseries.userdata_id == user_id), (Timeseries.logtype == "POSLOG"))

    return _session.scalars(statement).all()

def collected_items_per_user(_session: Session):
    statement = select(Timeseries.userdata_id.label("user_id"), func.count(Timeseries.logline).label("num_items"))\
        .where(Timeseries.logtype == "ITEMPICKUP")\
        .group_by(Timeseries.userdata_id)

    return {user_id: count for (user_id, count) in (_session.execute(statement))}


with Session(sqlite_engine) as session, session.begin():
    
    print(perspective_distribution(session))
    print(positions_for_user(session, 1))
    print(collected_items_per_user(session))
