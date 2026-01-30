namespace Core.Models;

public class Membership
{
    public Guid MembershipId { get; init; }
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    public DateTime JoinedAt { get; init; }
}