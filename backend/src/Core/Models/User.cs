namespace Core.Models;

public class User
{
    public Guid UserId { get; private set; }
    public int Id { get; init; }
    public string? DiscordId { get; init; }
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Email { get; init; } = null; //frivillig
    public int? Phone { get; init; } = null; //frivillig
    public string? Description { get; init; } = null; //frivillig
    public List<Guid> TagsIds { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public User()
    {
        UserId = Guid.NewGuid();
        TagsIds = new List<Guid>();
    }
}
