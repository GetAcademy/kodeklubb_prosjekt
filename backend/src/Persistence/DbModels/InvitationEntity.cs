namespace Persistence.DbModels;

public class InvitationEntity
{
    public long Id { get; init; }
    public long TeamId { get; init; }
    public long InvitedUserId { get; init; }
    public long InvitedBy { get; init; }
    public string Status { get; set; } = "pending"; // 'pending', 'accepted', 'declined', 'expired'
    public DateTime InvitedAt { get; init; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ExpiresAt { get; init; }
    public int Version { get; set; }

    public TeamEntity? Team { get; init; }
    public UserEntity? InvitedUser { get; init; }
    public UserEntity? InvitedByUser { get; init; }
}
