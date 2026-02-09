namespace Persistence.DbModels;

public class TeamMemberEntity
{
    public long Id { get; init; }
    public long TeamId { get; init; }
    public long UserId { get; init; }
    public string Role { get; init; } = "member";
    public string Status { get; init; } = "active";
    public DateTime JoinedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }

    public TeamEntity? Team { get; init; }
    public UserEntity? User { get; init; }
}
