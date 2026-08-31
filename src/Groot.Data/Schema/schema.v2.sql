-- v2: what a device measured during a session.
--
-- Both tables are children of one session and are replaced with it, the same way sets are. They
-- carry no updated_at and no device_id: a measurement belongs to the session it was taken in,
-- and there is no second device that could be editing the same run's heart rate at the same
-- time. When sync arrives they travel with their session rather than merging on their own.

-- One reading from one monitor. The source is part of the key because two watches on the same
-- body is a case the run screen supports, and both report at the same second constantly.
--
-- elapsed_s is the run screen's own clock, not a wall clock: it stops when the session is paused,
-- so a reading always sits where it belongs on the trace even if the runner stood still for a
-- minute at a crossing.
CREATE TABLE heart_rate_samples (
    session_id TEXT NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    source_id  TEXT NOT NULL,
    elapsed_s  INTEGER NOT NULL CHECK (elapsed_s >= 0),
    bpm        INTEGER NOT NULL CHECK (bpm BETWEEN 25 AND 250),

    PRIMARY KEY (session_id, source_id, elapsed_s)
) STRICT;

-- One position report. Latitude and longitude are stored as whole ten-millionths of a degree,
-- which is the form Android's location APIs already use: about a centimetre of resolution, exact
-- in an INTEGER column, and free of the rounding a TEXT decimal or a REAL would introduce on
-- every round trip. Accuracy is in centimetres for the same reason.
--
-- bpm is denormalised from heart_rate_samples on purpose. The map colours each stroke by the
-- heart rate at that point, and joining a thousand fixes to a thousand samples on nearest-time
-- to draw one line is work that the write already knew the answer to.
CREATE TABLE route_fixes (
    session_id  TEXT NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    elapsed_s   INTEGER NOT NULL CHECK (elapsed_s >= 0),
    lat_e7      INTEGER NOT NULL CHECK (lat_e7 BETWEEN -900000000 AND 900000000),
    lon_e7      INTEGER NOT NULL CHECK (lon_e7 BETWEEN -1800000000 AND 1800000000),
    accuracy_cm INTEGER NOT NULL CHECK (accuracy_cm > 0),
    bpm         INTEGER CHECK (bpm IS NULL OR bpm BETWEEN 25 AND 250),

    PRIMARY KEY (session_id, elapsed_s)
) STRICT;
