namespace Core.Models;

public enum InvitationStatus
{
    Accepted,
    Pending,
    Rejected
}

public class Invitation
{
    public Guid InvitationId { get; init; }
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    // initiatedBy
    public InvitationStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ResolvedAt { get; init; }
}