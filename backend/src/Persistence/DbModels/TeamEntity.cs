namespace Persistence.DbModels;

public class TeamEntity
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Location { get; init; }
    public string? DiscordLink { get; init; }
    public string? DiscordServerId { get; init; }
    public string? DiscordChannelId { get; init; }
    public string? DiscordRoleId { get; init; }
    public string? MeetingSchedule { get; init; }
    public bool IsOpenToJoinRequests { get; init; } = true;
    public Guid CreatedBy { get; init; }
    public Guid TeamAdminId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }
}