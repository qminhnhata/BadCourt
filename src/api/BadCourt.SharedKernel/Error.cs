namespace BadCourt.SharedKernel;

/// <summary>
/// The failure value carried by a <see cref="Result"/>. A code identifies the failure for
/// callers and for the eventual HTTP mapping; the description is for humans reading logs.
/// </summary>
public sealed record Error(string Code, string Description)
{
    /// <summary>
    /// The absence of a failure. Every successful <see cref="Result"/> carries this, so
    /// "has an error" and "is a failure" can never disagree.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}
