PRAGMA FOREIGN_KEYS = ON;

DROP TABLE IF EXISTS "userdata";
DROP TABLE IF EXISTS "timeseries";

-- user session data
CREATE TABLE "userdata" (
    "id" INTEGER NOT NULL,
    "user" TEXT NOT NULL,
    "perspective" TEXT NOT NULL, -- Add as separate datum so we can do operations directly dependent on this.
    "ipaddr" TEXT NOT NULL,
    "starttime" TEXT NOT NULL,
    "endtime" TEXT,
    "params" TEXT NOT NULL,
    PRIMARY KEY("id")
);

-- generic timeseries data
CREATE TABLE "timeseries" (
    "id" INTEGER NOT NULL,
    "userdata_id" TEXT NOT NULL,
    "timestamp" TEXT NOT NULL,
    "logtype" TEXT NOT NULL,
    "logline" TEXT NOT NULL,
    PRIMARY KEY("id")
);