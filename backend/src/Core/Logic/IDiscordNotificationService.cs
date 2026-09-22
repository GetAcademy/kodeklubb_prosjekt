namespace Core.Logic;

public enum DiscordChannelType
{
    Text = 0,
    Voice = 2,
}

public interface IDiscordNotificationService
{
    Task SendDirectMessageAsync(string discordUserId, string message);

    Task SendChannelMessageAsync(string channelId, string message);

    Task<string> CreateRoleAsync(string guildId, string roleName);

    Task<string> CreateRestrictedChannelAsync(string guildId, string channelName, DiscordChannelType channelType, string roleId);

    Task AddRoleToMemberAsync(string guildId, string userId, string roleId);
}
