namespace Core.DomainEvents;

public record JoinRequestDeclined(
    Guid TeamId,
    Guid RequestId,
    Guid UserId,
    Guid DeclinedBy,
    DateTime OccurredAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = OccurredAt;
}
