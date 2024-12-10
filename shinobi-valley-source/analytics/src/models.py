from typing import List, Optional

from sqlalchemy import ForeignKey
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, relationship


class Base(DeclarativeBase):
    pass


class Userdata(Base):

    __tablename__ = "userdata"

    id: Mapped[int] = mapped_column(primary_key=True)

    user: Mapped[str]
    perspective: Mapped[str]
    ipaddr: Mapped[str]
    starttime: Mapped[str]
    endtime: Mapped[Optional[str]]
    params: Mapped[str]

    timeseries_logs: Mapped[List["Timeseries"]] = relationship(
        back_populates="userdata", cascade="all, delete-orphan"
    )


class Timeseries(Base):

    __tablename__ = "timeseries"

    id: Mapped[int] = mapped_column(primary_key=True)

    timestamp: Mapped[str]
    logtype: Mapped[str]
    logline: Mapped[str]

    userdata_id: Mapped[int] = mapped_column(ForeignKey("userdata.id"))

    userdata: Mapped["Userdata"] = relationship(back_populates="timeseries_logs")
