namespace Persistence.DbModels;

public class TagEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<TeamTagEntity> TeamTags { get; set; } = new List<TeamTagEntity>();
}
