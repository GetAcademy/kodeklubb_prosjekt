namespace Core.Logic;

public interface IDiscordNotificationService
{
    /// <summary>
    /// Sends a direct message to a user via Discord. Requires the bot to
    /// share a server with the user (or the user to have DMs open).
    /// </summary>
    Task SendDirectMessageAsync(string discordUserId, string message);
}
