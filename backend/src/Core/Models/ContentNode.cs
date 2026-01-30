namespace Core.Models;

public class ContentNode
{
    /*
     * ContentNode representerer strukturen for globalt læringsinnhold.
     * "Enkel trestruktur basert på parent-referanser".
     * Basically, trestrukturen for Tags fra det jeg kan forstå.
     *
     * Kan hende ikke alt av detta her trenger å værra Guid :-)
     */
    public Guid NodeId { get; init; }
    public string? Title { get; init; }
    public Guid ParentNodeId { get; init; }
    public int SortOrder { get; init; } // Rekkefølge (index) for tags under en parent tag, unødvendig kanskje...
    public string? MarkdownContent { get; init; } = null; //"null betyr at noden er en kategori/meny-node"
    public bool OpenForChildSuggestions { get; init; }
}