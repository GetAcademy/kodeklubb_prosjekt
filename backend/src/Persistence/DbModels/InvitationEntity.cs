namespace Persistence.DbModels;

public class InvitationEntity
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long InvitedUserId { get; set; }
    public long InvitedBy { get; set; }
    public string Status { get; set; } = "pending"; // 'pending', 'accepted', 'declined', 'expired'
    public DateTime InvitedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int Version { get; set; }

    public TeamEntity? Team { get; set; }
    public UserEntity? InvitedUser { get; set; }
    public UserEntity? InvitedByUser { get; set; }
}