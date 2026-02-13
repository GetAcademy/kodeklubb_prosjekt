namespace Persistence.DbModels;

public record InvitationEntity
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid InvitedUserId { get; init; }
    public Guid InvitedBy { get; init; }
    public string Status { get; set; } = "pending";
    public DateTime InvitedAt { get; init; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ExpiresAt { get; init; }
    public int Version { get; set; }
}
