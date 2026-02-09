namespace Persistence.DbModels;

public class UserEntity
{
    public long Id { get; init; }
    public string DiscordId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }

    public ICollection<TeamMemberEntity> TeamMembers { get; init; } = new List<TeamMemberEntity>();
}
