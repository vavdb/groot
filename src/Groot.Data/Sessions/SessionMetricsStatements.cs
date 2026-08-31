namespace Groot.Data.Sessions;

/// <summary>Every statement the session metrics store runs.</summary>
internal static class SessionMetricsStatements
{
    public const string DeleteHeartRateOfSource =
        "DELETE FROM heart_rate_samples WHERE session_id = @sessionId AND source_id = @sourceId";

    public const string InsertHeartRate = """
        INSERT INTO heart_rate_samples (session_id, source_id, elapsed_s, bpm)
        VALUES (@SessionId, @SourceId, @ElapsedS, @Bpm)
        """;

    public const string SelectHeartRateOfSource = """
        SELECT elapsed_s, bpm FROM heart_rate_samples
        WHERE session_id = @sessionId AND source_id = @sourceId
        ORDER BY elapsed_s
        """;

    public const string SelectHeartRateSources = """
        SELECT DISTINCT source_id FROM heart_rate_samples
        WHERE session_id = @sessionId
        ORDER BY source_id
        """;

    public const string DeleteRoute = "DELETE FROM route_fixes WHERE session_id = @sessionId";

    public const string InsertRouteFix = """
        INSERT INTO route_fixes (session_id, elapsed_s, lat_e7, lon_e7, accuracy_cm, bpm)
        VALUES (@SessionId, @ElapsedS, @LatE7, @LonE7, @AccuracyCm, @Bpm)
        """;

    public const string SelectRoute = """
        SELECT elapsed_s, lat_e7, lon_e7, accuracy_cm, bpm FROM route_fixes
        WHERE session_id = @sessionId
        ORDER BY elapsed_s
        """;
}
