using BadCourt.SharedKernel;
using Shouldly;
using Xunit;

namespace BadCourt.UnitTests.SharedKernel;

public class AggregateRootTests
{
    [Fact]
    public void A_new_aggregate_has_raised_nothing()
    {
        Booking booking = new();

        booking.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Raising_records_the_event()
    {
        Booking booking = new();

        booking.Confirm();

        booking.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<BookingConfirmed>()
            .BookingId.ShouldBe(booking.Id);
    }

    [Fact]
    public void Clearing_discards_the_events()
    {
        Booking booking = new();
        booking.Confirm();

        booking.ClearDomainEvents();

        booking.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Events_already_taken_survive_a_clear()
    {
        Booking booking = new();
        booking.Confirm();

        // This is the order the dispatcher will work in: take the events, clear them, then
        // publish. A live view instead of a snapshot would publish nothing.
        IReadOnlyCollection<IDomainEvent> taken = booking.DomainEvents;
        booking.ClearDomainEvents();

        taken.ShouldHaveSingleItem();
        booking.DomainEvents.ShouldBeEmpty();
    }

    private sealed record BookingConfirmed(Guid BookingId) : IDomainEvent;

    private sealed class Booking() : AggregateRoot(Guid.CreateVersion7())
    {
        public void Confirm() => Raise(new BookingConfirmed(Id));
    }
}
