namespace Core.DomainEvents;

public record UserRequestedToJoinTeam(
    Guid TeamId,
    Guid UserId,
    DateTime OccurredAt
) : IDomainEvent
{
    public DateTime TimeStamp { get; } = OccurredAt;
}
