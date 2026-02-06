namespace Core.DomainEvents;

public record UserInvitedToTeam(
    Guid UserId,
    Guid InvitedByUserId,
    DateTime OccuredAt
) : IDomainEvent
{
    public DateTime TimeStamp { get; } = OccuredAt;
}