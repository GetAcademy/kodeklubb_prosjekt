namespace Core.Models;

public record Team(
    Guid TeamId,
    string? Name,
    string? Description,
    string? MeetingTimeDescription,
    string? DiscordInviteLink,
    string[] TagsIds,
    bool IsVisible
    );
    
    // public int Id { get; init; }
    // public string? DiscordId { get; init; }
    // public string? Email { get; init; }
    // public string? Username { get; init; }
    // public string? AvatarUrl { get; init; }
    // public DateTime CreatedAt { get; init; }
    // public DateTime UpdatedAt { get; init; }
    
    // teamId
    //     name
    // description
    //     meetingTimeDescription
    // discordInviteLink
    // tagsIds[]
    // isVisible (default: true)
