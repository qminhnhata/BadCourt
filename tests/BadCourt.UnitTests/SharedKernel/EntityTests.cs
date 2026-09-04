using BadCourt.SharedKernel;
using Shouldly;
using Xunit;

namespace BadCourt.UnitTests.SharedKernel;

public class EntityTests
{
    private static readonly Guid SharedId = Guid.Parse("0199a6f4-1c5b-7c2e-9a3d-6f1b8c4d5e2a");

    [Fact]
    public void The_same_type_and_id_is_the_same_entity()
    {
        Court left = new(SharedId);
        Court right = new(SharedId);

        left.Equals(right).ShouldBeTrue();
        (left == right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Different_ids_are_different_entities()
    {
        Court left = new(SharedId);
        Court right = new(Guid.CreateVersion7());

        left.Equals(right).ShouldBeFalse();
        (left != right).ShouldBeTrue();
    }

    [Fact]
    public void Different_types_sharing_an_id_are_not_the_same_entity()
    {
        Court court = new(SharedId);
        Facility facility = new(SharedId);

        court.Equals(facility).ShouldBeFalse();
        court.GetHashCode().ShouldNotBe(facility.GetHashCode());
    }

    [Fact]
    public void An_entity_is_never_equal_to_null()
    {
        Court court = new(SharedId);

        court.Equals(null).ShouldBeFalse();
        (court == null).ShouldBeFalse();
        (null == court).ShouldBeFalse();
    }

    [Fact]
    public void An_entity_must_have_an_identity()
    {
        Should.Throw<ArgumentException>(() => new Court(Guid.Empty));
    }

    private sealed class Court(Guid id) : Entity(id);

    private sealed class Facility(Guid id) : Entity(id);
}
