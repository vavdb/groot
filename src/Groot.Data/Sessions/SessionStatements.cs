namespace Groot.Data.Sessions;

/// <summary>
/// Every statement the session store runs. Written out rather than composed: nine tables and
/// about twenty queries is a size where plain SQL on the page beats a generator, and a mistake
/// here shows up in the round-trip test rather than in a string that assembled wrong.
/// </summary>
internal static class SessionStatements
{
    public const string SessionColumns =
        "id, user_id, date, kind, program_id, day_key, interval_week, interval_day, " +
        "duration_s, notes, updated_at, device_id, deleted";

    public const string SetColumns =
        "id, session_id, exercise_id, set_order, weight_kg, reps, entry_mode, entry_weight, " +
        "entry_unit, equipment_id, is_warmup, notes";

    /// <summary>
    /// A local save. Unconditional, because the person holding the phone just did this and it is
    /// by definition the newest thing that happened here. Applying the merge rule to local writes
    /// silently drops the second of two saves in the same millisecond.
    /// </summary>
    public const string SaveSession = $"""
        INSERT INTO sessions ({SessionColumns})
        VALUES (@Id, @UserId, @Date, @Kind, @ProgramId, @DayKey, @IntervalWeek, @IntervalDay,
                @DurationS, @Notes, @UpdatedAt, @DeviceId, @Deleted)
        ON CONFLICT(id) DO UPDATE SET
            date          = excluded.date,
            kind          = excluded.kind,
            program_id    = excluded.program_id,
            day_key       = excluded.day_key,
            interval_week = excluded.interval_week,
            interval_day  = excluded.interval_day,
            duration_s    = excluded.duration_s,
            notes         = excluded.notes,
            updated_at    = excluded.updated_at,
            device_id     = excluded.device_id,
            deleted       = excluded.deleted
        """;

    /// <summary>
    /// A row arriving from the server. Last write wins, and <c>device_id</c> breaks a tie on an
    /// equal timestamp: the winner is arbitrary but identical on every device, which is what
    /// matters — without it two devices each keep their own version and never converge.
    /// </summary>
    public const string MergeSession = $"""
        {SaveSession}
        WHERE sessions.user_id = excluded.user_id
          AND (excluded.updated_at, excluded.device_id) > (sessions.updated_at, sessions.device_id)
        """;

    public const string DeleteSetsOfSession = "DELETE FROM sets WHERE session_id = @sessionId";

    public const string InsertSet = $"""
        INSERT INTO sets ({SetColumns})
        VALUES (@Id, @SessionId, @ExerciseId, @SetOrder, @WeightKg, @Reps, @EntryMode,
                @EntryWeight, @EntryUnit, @EquipmentId, @IsWarmup, @Notes)
        """;

    public const string SelectSessionById =
        $"SELECT {SessionColumns} FROM sessions WHERE user_id = @userId AND id = @id AND deleted = 0";

    public const string SelectSessionsBetween = $"""
        SELECT {SessionColumns} FROM sessions
        WHERE user_id = @userId AND deleted = 0 AND date >= @from AND date <= @to
        ORDER BY date, updated_at
        """;

    public const string SelectSetsOfSessions =
        $"SELECT {SetColumns} FROM sets WHERE session_id IN @sessionIds ORDER BY session_id, set_order";

    /// <summary>
    /// Dates only; the grouping happens in C#. An aggregate column has no declared type, and
    /// with zero matching rows there is no value for the reader to infer one from, so COUNT(*)
    /// comes back as a BLOB and materialisation fails on an empty history — the one case a first
    /// run always hits. A history window is a few hundred rows, so counting them here is free.
    /// </summary>
    public const string SelectSessionDates = """
        SELECT date FROM sessions
        WHERE user_id = @userId AND deleted = 0 AND date >= @from AND date <= @to
        """;

    /// <summary>
    /// The newest session of one kind in a program. Ordered by date and then by <c>updated_at</c>
    /// so two sessions on the same day resolve to the one saved last, which is the one whose
    /// place in the rotation the next session follows.
    /// </summary>
    public const string SelectLatestOfKind = $"""
        SELECT {SessionColumns} FROM sessions
        WHERE user_id = @userId AND program_id = @programId AND kind = @kind AND deleted = 0
        ORDER BY date DESC, updated_at DESC
        LIMIT 1
        """;

    /// <summary>
    /// A local delete, and unconditional for the same reason a local save is: the user just did
    /// it. A deletion arriving from the server is an ordinary <see cref="MergeSession"/> of a row
    /// whose <c>deleted</c> is set, so it goes through the last-write-wins path instead.
    /// <para>
    /// The note goes with it. Sync needs the fact of the deletion, not the sentence the person
    /// typed, and a tombstone that keeps free text keeps it on every device forever.
    /// </para>
    /// </summary>
    public const string MarkSessionDeleted = """
        UPDATE sessions SET deleted = 1, notes = NULL, updated_at = @updatedAt, device_id = @deviceId
        WHERE user_id = @userId AND id = @id
        """;
}
