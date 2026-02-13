namespace Persistence.DbModels;

public class ContentEntity
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string? ContentUrl { get; init; }
    public int OrderIndex { get; init; }
    public bool IsPublished { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }
}