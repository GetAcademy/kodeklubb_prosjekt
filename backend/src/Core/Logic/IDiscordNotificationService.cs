namespace Core.Logic;

public interface IDiscordNotificationService
{
    /// <summary>
    /// Sends a direct message to a user via Discord. Requires the bot to
    /// share a server with the user (or the user to have DMs open).
    /// </summary>
    Task SendDirectMessageAsync(string discordUserId, string message);

    /// <summary>
    /// Posts a message directly into a server channel (not a DM). Requires
    /// the bot to have Send Messages permission in that channel.
    /// </summary>
    Task SendChannelMessageAsync(string channelId, string message);
}
