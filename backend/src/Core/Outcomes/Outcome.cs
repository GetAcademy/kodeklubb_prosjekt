namespace Core.Outcomes;

public enum OutcomeStatus
{
    Accepted,
    Rejected
}

public readonly record struct Outcome(
    OutcomeStatus Status,
    string? Message = null
)
{
    public static Outcome Accepted()
    {
        return new Outcome(OutcomeStatus.Accepted);
    }

    public static Outcome Rejected(string message)
    {
        return new Outcome(OutcomeStatus.Rejected, message);
    }
}