namespace Persistence.DbModels;

public class ContentEntity
{
    public long Id { get; init; }
    public long TeamId { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public string ContentType { get; init; }
    public string ContentUrl { get; init; }
    public int OrderIndex { get; init; }
    public bool IsPublished { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }
}