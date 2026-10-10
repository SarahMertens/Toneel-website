using System.ComponentModel.DataAnnotations;
using Toneel.Core.Reservations;

namespace Toneel.Core.Tests;

public class ReservationServiceTests
{
    private readonly InMemoryReservationStore _store = new();
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _service = new ReservationService(_store, Sample.Production, new FixedClock());
    }

    [Fact]
    public async Task Adding_saves_a_trimmed_name_with_the_location_of_that_date()
    {
        var saved = await _service.AddAsync(Sample.Input("  Lotte ", " Vermeulen  ", seats: 3));

        Assert.Equal("Lotte", saved.FirstName);
        Assert.Equal("Vermeulen", saved.LastName);
        Assert.Equal("Zaal Noord", saved.Location);
        Assert.Equal(3, saved.Seats);
        Assert.Equal(FixedClock.Default, saved.AddedAt);
        Assert.NotEqual("", saved.Id);
        Assert.Single(await _store.GetAllAsync());
    }

    [Fact]
    public async Task A_reservation_without_a_date_has_no_location()
    {
        var saved = await _service.AddAsync(Sample.Input(undated: true));

        Assert.Null(saved.Date);
        Assert.Equal("", saved.Location);
    }

    [Theory]
    [InlineData("", "Vermeulen", 1)]
    [InlineData("   ", "Vermeulen", 1)]
    [InlineData("Lotte", "", 1)]
    [InlineData("Lotte", "Vermeulen", 0)]
    [InlineData("Lotte", "Vermeulen", -2)]
    [InlineData("Lotte", "Vermeulen", 101)]
    public async Task Refuses_invalid_input_and_saves_nothing(string firstName, string lastName, int seats)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.AddAsync(Sample.Input(firstName, lastName, seats: seats)));

        Assert.Empty(await _store.GetAllAsync());
    }

    [Fact]
    public async Task Refuses_a_name_that_is_too_long()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.AddAsync(Sample.Input(firstName: new string('a', 61))));
    }

    [Fact]
    public async Task Refuses_a_date_without_a_performance()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.AddAsync(Sample.Input(date: new DateOnly(2030, 5, 5))));
    }

    [Fact]
    public void Finds_the_same_person_on_the_same_date_ignoring_case_and_spaces()
    {
        var existing = new[] { Sample.Reservation("Lotte", "Vermeulen", id: "a") };

        var duplicate = ReservationService.FindDuplicate(existing, Sample.Input(" lotte", "VERMEULEN "));

        Assert.Equal("a", duplicate?.Id);
    }

    [Fact]
    public void The_same_person_on_another_date_is_not_a_duplicate()
    {
        var existing = new[] { Sample.Reservation(date: Sample.Saturday, id: "a") };

        Assert.Null(ReservationService.FindDuplicate(existing, Sample.Input(date: Sample.Sunday)));
    }

    [Fact]
    public void Reservations_without_a_date_are_never_duplicates()
    {
        var existing = new[] { Sample.Reservation(undated: true, id: "a") };

        Assert.Null(ReservationService.FindDuplicate(existing, Sample.Input(undated: true)));
    }

    [Fact]
    public void A_reservation_being_edited_does_not_duplicate_itself()
    {
        var existing = new[] { Sample.Reservation(id: "a") };

        Assert.Null(ReservationService.FindDuplicate(existing, Sample.Input(), ignoreId: "a"));
    }

    [Fact]
    public async Task Merging_adds_the_seats_to_the_existing_reservation()
    {
        var existing = await _store.AddAsync(Sample.Reservation(seats: 2));

        var merged = await _service.MergeAsync(existing, extraSeats: 3);

        Assert.Equal(5, merged.Seats);
        Assert.Equal(5, Assert.Single(await _store.GetAllAsync()).Seats);
    }

    [Fact]
    public async Task Merging_never_goes_above_the_maximum()
    {
        var existing = await _store.AddAsync(Sample.Reservation(seats: 98));

        var merged = await _service.MergeAsync(existing, extraSeats: 10);

        Assert.Equal(100, merged.Seats);
    }

    [Fact]
    public async Task Merging_an_edited_reservation_removes_the_one_it_replaces()
    {
        var target = await _store.AddAsync(Sample.Reservation("Lotte", seats: 2));
        var edited = await _store.AddAsync(Sample.Reservation("Lote", seats: 4));

        await _service.MergeAsync(target, extraSeats: 4, replaces: edited);

        var remaining = Assert.Single(await _store.GetAllAsync());
        Assert.Equal(target.Id, remaining.Id);
        Assert.Equal(6, remaining.Seats);
    }

    [Fact]
    public async Task Updating_changes_the_fields_but_keeps_id_and_creation_time()
    {
        var original = await _store.AddAsync(Sample.Reservation(addedMinutesAgo: 90));

        var updated = await _service.UpdateAsync(
            original,
            Sample.Input("Lotte", "Vermeulen-Claes", Sample.Sunday, seats: 4));

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.AddedAt, updated.AddedAt);
        Assert.Equal("Vermeulen-Claes", updated.LastName);
        Assert.Equal("Zaal Zuid", updated.Location);
        Assert.Equal(updated, Assert.Single(await _store.GetAllAsync()));
    }

    [Fact]
    public async Task Clearing_the_date_also_clears_the_location()
    {
        var original = await _store.AddAsync(Sample.Reservation());

        var updated = await _service.UpdateAsync(original, Sample.Input(undated: true));

        Assert.Null(updated.Date);
        Assert.Equal("", updated.Location);
    }

    [Fact]
    public async Task Deleting_removes_the_reservation()
    {
        var keep = await _store.AddAsync(Sample.Reservation("Jef"));
        var remove = await _store.AddAsync(Sample.Reservation("Nora"));

        await _service.DeleteAsync(remove);

        Assert.Equal(keep.Id, Assert.Single(await _store.GetAllAsync()).Id);
    }
}
