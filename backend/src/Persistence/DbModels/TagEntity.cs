namespace Persistence.DbModels;

public class TagEntity
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}
