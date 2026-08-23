using Dapper;
using Groot.Core.Equipment;

namespace Groot.Data.Equipment;

/// <summary>
/// The lifter's equipment: a bar and the plates that go on it. Stored as one aggregate, because
/// plates are meaningless apart from the bar they load, and edited rarely enough that replacing
/// the whole profile on save costs nothing.
/// </summary>
public sealed class EquipmentStore(GrootDatabase database)
{
    private const string Columns =
        "id, user_id, name, kind, unit, actual_kg, counts_as_kg, declared_loads, updated_at, device_id, deleted";

    private const string SaveEquipment = $"""
        INSERT INTO equipment ({Columns})
        VALUES (@Id, @UserId, @Name, @Kind, @Unit, @ActualKg, @CountsAsKg, @DeclaredLoads,
                @UpdatedAt, @DeviceId, @Deleted)
        ON CONFLICT(user_id, id) DO UPDATE SET
            name           = excluded.name,
            kind           = excluded.kind,
            unit           = excluded.unit,
            actual_kg      = excluded.actual_kg,
            counts_as_kg   = excluded.counts_as_kg,
            declared_loads = excluded.declared_loads,
            updated_at     = excluded.updated_at,
            device_id      = excluded.device_id,
            deleted        = excluded.deleted
        """;

    private const string MergeEquipment = $"""
        {SaveEquipment}
        WHERE (excluded.updated_at, excluded.device_id) > (equipment.updated_at, equipment.device_id)
        """;

    private const string DeletePlates =
        "DELETE FROM plates WHERE user_id = @userId AND equipment_id = @equipmentId";

    private const string InsertPlate =
        "INSERT INTO plates (user_id, equipment_id, kg_g, pairs) VALUES (@UserId, @EquipmentId, @KgG, @Pairs)";

    private const string SelectEquipment =
        $"SELECT {Columns} FROM equipment WHERE user_id = @userId AND id = @id AND deleted = 0";

    private const string SelectPlates =
        "SELECT kg_g, pairs FROM plates WHERE user_id = @userId AND equipment_id = @equipmentId ORDER BY kg_g DESC";

    /// <summary>
    /// Writes the bar and its plates as an edit made on this device, replacing whatever plate set
    /// was stored before. Unconditional: the person changing their rack just did this.
    /// </summary>
    public Task Save(Guid userId, EquipmentProfile profile, long updatedAt, string deviceId, CancellationToken cancellationToken = default) =>
        Write(SaveEquipment, userId, profile, updatedAt, deviceId, cancellationToken);

    /// <summary>
    /// Applies a profile that arrived from the server. Older than what is stored, and nothing
    /// changes, plates included.
    /// </summary>
    public Task Merge(Guid userId, EquipmentProfile profile, long updatedAt, string deviceId, CancellationToken cancellationToken = default) =>
        Write(MergeEquipment, userId, profile, updatedAt, deviceId, cancellationToken);

    private async Task Write(string statement, Guid userId, EquipmentProfile profile, long updatedAt, string deviceId, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();
        using var transaction = connection.BeginTransaction();

        var bar = profile.Bar;
        var applied = await connection.ExecuteAsync(new CommandDefinition(statement, new
        {
            bar.Id,
            UserId = SqliteValues.FromGuid(userId),
            bar.Name,
            Kind = SqliteValues.FromEnum(bar.Kind),
            Unit = SqliteValues.FromEnum(bar.Unit),
            ActualKg = SqliteValues.FromDecimal(bar.ActualKg),
            CountsAsKg = SqliteValues.FromDecimal(bar.CountsAsKg),
            DeclaredLoads = bar.DeclaredLoads is { Count: > 0 } loads
                ? string.Join(',', loads.Select(SqliteValues.FromDecimal))
                : null,
            UpdatedAt = updatedAt,
            DeviceId = deviceId,
            Deleted = 0L,
        }, transaction, cancellationToken: cancellationToken));

        if (applied > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                DeletePlates,
                new { userId = SqliteValues.FromGuid(userId), equipmentId = bar.Id },
                transaction,
                cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                InsertPlate,
                profile.Plates.Select(plate => new
                {
                    UserId = SqliteValues.FromGuid(userId),
                    EquipmentId = bar.Id,
                    KgG = SqliteValues.FromKilograms(plate.Kg),
                    plate.Pairs,
                }).ToArray(),
                transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    /// <summary>
    /// The stored profile for one account's bar, or null when nothing has been saved for it. The
    /// account is a parameter because an equipment id is a slug like "atx", not a GUID: without
    /// it this would hand back whichever account happened to save that slug last.
    /// </summary>
    public async Task<EquipmentProfile?> Find(Guid userId, string equipmentId, CancellationToken cancellationToken = default)
    {
        using var connection = database.Open();
        var owner = new { userId = SqliteValues.FromGuid(userId), equipmentId };

        var row = await connection.QuerySingleOrDefaultAsync<EquipmentRow>(new CommandDefinition(
            SelectEquipment,
            new { userId = owner.userId, id = equipmentId },
            cancellationToken: cancellationToken));
        if (row is null) return null;

        var plates = await connection.QueryAsync<PlateRow>(new CommandDefinition(
            SelectPlates, owner, cancellationToken: cancellationToken));

        var bar = new Core.Equipment.Equipment(
            row.Id,
            row.Name,
            SqliteValues.ToEnum<EquipmentKind>(row.Kind),
            SqliteValues.ToEnum<WeightUnit>(row.Unit),
            SqliteValues.ToDecimalOrNull(row.ActualKg),
            SqliteValues.ToDecimalOrNull(row.CountsAsKg),
            row.DeclaredLoads?.Split(',').Select(SqliteValues.ToDecimal).ToArray());

        return new EquipmentProfile(
            bar,
            plates.Select(p => new PlatePair(SqliteValues.ToKilograms(p.KgG), (int)p.Pairs)).ToArray());
    }

    private sealed record EquipmentRow(
        string Id,
        string UserId,
        string Name,
        string Kind,
        string Unit,
        string? ActualKg,
        string? CountsAsKg,
        string? DeclaredLoads,
        long UpdatedAt,
        string DeviceId,
        long Deleted);

    private sealed record PlateRow(long KgG, long Pairs);
}
