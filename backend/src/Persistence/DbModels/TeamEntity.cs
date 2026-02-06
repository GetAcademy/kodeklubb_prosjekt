namespace Persistence.DbModels;

public class TeamEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long CreatedBy { get; set; }
    public long TeamAdminId { get; set; } // Non-nullable team admin user ID
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }

    public ICollection<TeamTagEntity> TeamTags { get; set; } = new List<TeamTagEntity>();
    public ICollection<TeamMemberEntity> TeamMembers { get; set; } = new List<TeamMemberEntity>();
}