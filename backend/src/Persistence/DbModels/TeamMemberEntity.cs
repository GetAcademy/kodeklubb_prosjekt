namespace Persistence.DbModels;

public class TeamMemberEntity
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long UserId { get; set; }
    public string Role { get; set; } = "member";
    public string Status { get; set; } = "active";
    public DateTime JoinedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }

    public TeamEntity? Team { get; set; }
    public UserEntity? User { get; set; }
}