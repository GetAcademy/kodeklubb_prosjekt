namespace Core.State;

public record UserState(
    Guid UserId,
    int DiscordId,
    string? Email,
    string? Username,
    string? AvatarUrl
    );