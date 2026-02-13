namespace Persistence.DbModels;

public class TeamMemberEntity
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    public string DiscordId { get; init; }
    public string Role { get; init; } = "member";
    public string Status { get; init; } = "active";
    public DateTime JoinedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }
}
