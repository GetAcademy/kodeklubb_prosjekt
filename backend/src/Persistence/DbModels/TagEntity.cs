namespace Persistence.DbModels;

public class TagEntity
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }

    public ICollection<TeamTagEntity> TeamTags { get; init; } = new List<TeamTagEntity>();
}
