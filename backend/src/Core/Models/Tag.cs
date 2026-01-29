namespace Core.Models;

public class Tag
{
    public Guid TagId { get; init; }
    public string? Name { get; init; }
    public Guid? ParentTagId { get; init; } = null; //"null for rot-noder" - usikker på betydning
    public bool openForChildSuggestions { get; init; }
}