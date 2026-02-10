namespace Core.DomainEvents;

public record TeamCreated(
    Guid TeamId,
    string Name,
    string? Description,
    Guid AdminUserId,
    DateTime OccurredAt
) : IDomainEvent
{
    public DateTime TimeStamp { get; } = OccurredAt;
}
