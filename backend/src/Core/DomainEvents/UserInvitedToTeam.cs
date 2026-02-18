namespace Core.DomainEvents;

public record UserInvitedToTeam(
    Guid UserId,
    Guid InvitedByUserId,
    DateTime OccurredAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = OccurredAt;
}