using BadCourt.SharedKernel;
using Shouldly;
using Xunit;

namespace BadCourt.UnitTests.SharedKernel;

public class ResultTests
{
    private static readonly Error SampleError = new("court.unavailable", "The court is already booked.");

    [Fact]
    public void A_success_carries_no_error()
    {
        Result result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void A_failure_carries_its_error()
    {
        Result result = Result.Failure(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void A_failure_must_name_a_cause()
    {
        Should.Throw<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void A_success_cannot_carry_an_error()
    {
        Should.Throw<ArgumentException>(() => new DerivedResult(isSuccess: true, SampleError));
    }

    [Fact]
    public void A_success_exposes_its_value()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void The_value_of_a_failure_cannot_be_read()
    {
        Result<int> result = Result.Failure<int>(SampleError);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void A_value_converts_implicitly_into_a_success()
    {
        Result<string> result = "booked";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("booked");
    }

    /// <summary>
    /// The success-with-an-error branch of the guard is unreachable through the public
    /// factories, which is the point of them. A subclass reaches the protected constructor
    /// so the invariant is still covered for the types later phases will derive.
    /// </summary>
    private sealed class DerivedResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
