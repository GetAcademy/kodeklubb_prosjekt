namespace Persistence.DbModels;

public class UserEntity
{
    public long Id { get; set; }
    public string DiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }

    public ICollection<TeamMemberEntity> TeamMembers { get; set; } = new List<TeamMemberEntity>();
}
