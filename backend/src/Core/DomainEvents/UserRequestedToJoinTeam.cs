namespace Core.DomainEvents;

public record UserRequestedToJoinTeam(
    Guid TeamId,
    Guid UserId,
    DateTime OccurredAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = OccurredAt;
}
