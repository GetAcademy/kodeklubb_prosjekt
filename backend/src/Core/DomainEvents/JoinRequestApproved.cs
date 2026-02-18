namespace Core.DomainEvents;

public record JoinRequestApproved(
    Guid TeamId,
    Guid RequestId,
    Guid UserId,
    Guid ApprovedBy,
    DateTime OccurredAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = OccurredAt;
}
