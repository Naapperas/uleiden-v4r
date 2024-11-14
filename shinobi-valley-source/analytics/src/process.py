from sqlalchemy import create_engine, select, func
from sqlalchemy.orm import Session

from models import Userdata, Timeseries

from pathlib import Path

current_working_directory = Path.cwd()
print(f"sqlite://{str(current_working_directory.parent.joinpath('server-assets', 'db.db'))}")
sqlite_engine = create_engine(f"sqlite://{str(current_working_directory.parent.joinpath('server-assets', 'db.db'))}", echo=True)

def perspective_distribution(_session: Session):
    
    statement = select(Userdata.perspective, func.count().label("num_participants")).group_by(Userdata.perspective)

    print(session.scalars(statement).all())

with Session(sqlite_engine) as session, session.begin():
    
    perspective_distribution(session)