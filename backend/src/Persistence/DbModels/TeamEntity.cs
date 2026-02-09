namespace Persistence.DbModels;

public class TeamEntity
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long CreatedBy { get; init; }
    public long TeamAdminId { get; init; } // Non-nullable team admin user ID
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }

    public ICollection<TeamTagEntity> TeamTags { get; init; } = new List<TeamTagEntity>();
    public ICollection<TeamMemberEntity> TeamMembers { get; init; } = new List<TeamMemberEntity>();
}
