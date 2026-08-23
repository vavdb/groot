using Groot.Core.Equipment;
using Groot.Data.Settings;

namespace Groot.Data.Tests;

/// <summary>
/// The two stores the session tests only ever wrote to. Equipment is what turns a target weight
/// into plates, and settings hold the week start the contract maths keys off, so neither can be
/// left proven only by "it did not throw on save".
/// </summary>
public sealed class EquipmentAndSettingsStoreTests
{
    private const string Device = "phone";

    [Fact]
    public async Task The_rack_comes_back_with_its_bar_and_every_plate_pair()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 10, Device);
        var read = await temp.Equipment.Find(userId, EquipmentProfile.Rack.Bar.Id);

        Assert.NotNull(read);
        Assert.Equal(EquipmentProfile.Rack.Bar, read.Bar);
        Assert.Equal(EquipmentProfile.Rack.Plates, read.Plates);
    }

    [Fact]
    public async Task A_bar_keeps_the_difference_between_what_it_weighs_and_what_it_counts_as()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 10, Device);
        var bar = (await temp.Equipment.Find(userId, EquipmentProfile.Rack.Bar.Id))!.Bar;

        // The owner's ATX weighs 11 kg and is loaded as 10. Collapsing the two would put every
        // logged total a kilo off.
        Assert.Equal(11m, bar.ActualKg);
        Assert.Equal(10m, bar.CountsAsKg);
        Assert.Equal(10m, bar.EffectiveBarKg);
    }

    [Fact]
    public async Task Fixed_loads_survive_the_round_trip_and_stay_in_the_equipments_own_unit()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var powerBlock = new Core.Equipment.Equipment(
            "powerblock", "PowerBlock", EquipmentKind.AdjustableDumbbell, WeightUnit.Lb,
            DeclaredLoads: [10m, 17.5m, 25m]);

        await temp.Equipment.Save(userId, new EquipmentProfile(powerBlock, []), updatedAt: 10, Device);
        var read = await temp.Equipment.Find(userId, "powerblock");

        Assert.NotNull(read);
        // DeclaredLoads is compared on its own: a record compares a list by reference, so folding
        // it into the record assertion would check the identity of the array and nothing in it.
        Assert.Equal(powerBlock with { DeclaredLoads = null }, read.Bar with { DeclaredLoads = null });
        Assert.Equal(powerBlock.DeclaredLoads, read.Bar.DeclaredLoads);
        Assert.Empty(read.Plates);
    }

    [Fact]
    public async Task Saving_a_profile_again_replaces_its_plates_rather_than_adding_to_them()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 10, Device);

        var lighter = EquipmentProfile.Rack with { Plates = [new(20m, 2), new(10m, 2)] };
        await temp.Equipment.Save(userId, lighter, updatedAt: 20, Device);

        var read = await temp.Equipment.Find(userId, EquipmentProfile.Rack.Bar.Id);
        Assert.Equal(lighter.Plates, read!.Plates);
    }

    [Fact]
    public async Task Two_accounts_that_own_the_same_bar_keep_their_own_copy_of_it()
    {
        // An equipment id is a slug, not a GUID: "atx" is what both racks are called. Keyed on
        // the slug alone, the second account's save would rewrite the first account's bar weight
        // and, through the plate maths, every weight it was ever prescribed.
        using var temp = new TemporaryDatabase();
        var alice = await temp.CreateUser("alice");
        var bob = await temp.CreateUser("bob");

        await temp.Equipment.Save(alice, EquipmentProfile.Rack, updatedAt: 10, Device);
        var bobsBar = EquipmentProfile.Rack with
        {
            Bar = EquipmentProfile.Rack.Bar with { Name = "Bob's bar", ActualKg = 20m, CountsAsKg = 20m },
            Plates = [new(25m, 2)],
        };
        await temp.Equipment.Save(bob, bobsBar, updatedAt: 20, Device);

        var alices = await temp.Equipment.Find(alice, "atx");
        Assert.Equal(EquipmentProfile.Rack.Bar, alices!.Bar);
        Assert.Equal(EquipmentProfile.Rack.Plates, alices.Plates);
        Assert.Equal("Bob's bar", (await temp.Equipment.Find(bob, "atx"))!.Bar.Name);
    }

    [Fact]
    public async Task A_plate_written_as_five_point_zero_is_the_same_plate_as_five()
    {
        // Plate weight is part of the key. As a TEXT decimal, '5' and '5.0' are two rows for one
        // plate, and the solver would then think the rack has four fives when it has two.
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var bar = EquipmentProfile.Rack.Bar;

        await temp.Equipment.Save(userId, new EquipmentProfile(bar, [new(5m, 2)]), updatedAt: 10, Device);
        await temp.Equipment.Save(userId, new EquipmentProfile(bar, [new(5.0m, 2), new(20m, 1)]), updatedAt: 20, Device);

        var plates = (await temp.Equipment.Find(userId, bar.Id))!.Plates;
        Assert.Equal([new PlatePair(20m, 1), new PlatePair(5m, 2)], plates);
    }

    [Fact]
    public async Task A_rack_can_lose_its_last_plate()
    {
        // The delete-then-insert only runs when the upsert applied, and an empty list inserts
        // nothing, so the transition to no plates is the one that could silently keep the old set.
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var bar = EquipmentProfile.Rack.Bar;

        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 10, Device);
        await temp.Equipment.Save(userId, new EquipmentProfile(bar, []), updatedAt: 20, Device);

        Assert.Empty((await temp.Equipment.Find(userId, bar.Id))!.Plates);
    }

    [Fact]
    public async Task Unknown_equipment_reads_as_null_rather_than_as_an_empty_profile()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        Assert.Null(await temp.Equipment.Find(userId, "no-such-bar"));
    }

    [Fact]
    public async Task Settings_round_trip_the_week_start_the_contract_maths_keys_off()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var settings = new UserSettings(JokersPerWeek: 3, WeekStartDay: DayOfWeek.Sunday);

        await temp.Settings.Save(userId, settings, updatedAt: 10, Device);

        Assert.Equal(settings, await temp.Settings.Find(userId));
    }

    [Fact]
    public async Task An_account_with_no_saved_settings_reads_as_null_rather_than_as_defaults()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        // Null is the caller's cue to apply UserSettings.Default for the device locale; a store
        // that invented Monday would hide the fact that nobody has chosen yet.
        Assert.Null(await temp.Settings.Find(userId));
    }

    [Fact]
    public async Task A_local_equipment_save_wins_in_the_same_millisecond_as_the_last_one()
    {
        // Same rule as sessions: a local write is unconditional. An onboarding wizard that saves
        // the profile and then corrects one field must not lose the correction.
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 100, Device);
        var corrected = EquipmentProfile.Rack with { Bar = EquipmentProfile.Rack.Bar with { Name = "ATX, remeasured" } };
        await temp.Equipment.Save(userId, corrected, updatedAt: 100, Device);

        Assert.Equal("ATX, remeasured", (await temp.Equipment.Find(userId, corrected.Bar.Id))!.Bar.Name);
    }

    [Fact]
    public async Task An_older_equipment_profile_arriving_from_another_device_is_ignored()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Equipment.Save(userId, EquipmentProfile.Rack, updatedAt: 200, Device);
        var stale = EquipmentProfile.Rack with { Plates = [] };
        await temp.Equipment.Merge(userId, stale, updatedAt: 100, "tablet");

        Assert.Equal(EquipmentProfile.Rack.Plates, (await temp.Equipment.Find(userId, EquipmentProfile.Rack.Bar.Id))!.Plates);
    }

    [Fact]
    public async Task A_local_settings_save_wins_in_the_same_millisecond_as_the_last_one()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Settings.Save(userId, new UserSettings(2, DayOfWeek.Monday), updatedAt: 100, Device);
        await temp.Settings.Save(userId, new UserSettings(3, DayOfWeek.Sunday), updatedAt: 100, Device);

        Assert.Equal(new UserSettings(3, DayOfWeek.Sunday), await temp.Settings.Find(userId));
    }

    [Fact]
    public async Task Older_settings_arriving_from_another_device_are_ignored()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();

        await temp.Settings.Save(userId, new UserSettings(2, DayOfWeek.Monday), updatedAt: 200, Device);
        await temp.Settings.Merge(userId, new UserSettings(9, DayOfWeek.Sunday), updatedAt: 100, "tablet");

        Assert.Equal(new UserSettings(2, DayOfWeek.Monday), await temp.Settings.Find(userId));
    }

    [Fact]
    public async Task Settings_survive_closing_and_reopening_the_database()
    {
        using var temp = new TemporaryDatabase();
        var userId = await temp.CreateUser();
        var settings = new UserSettings(2, DayOfWeek.Monday);

        await temp.Settings.Save(userId, settings, updatedAt: 10, Device);

        var afterRestart = new SettingsStore(temp.Reopen());
        Assert.Equal(settings, await afterRestart.Find(userId));
    }
}
