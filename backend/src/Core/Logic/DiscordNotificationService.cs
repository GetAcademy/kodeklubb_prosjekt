using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Core.Logic;

public class DiscordNotificationService : IDiscordNotificationService
{
    private readonly HttpClient _httpClient;

    public DiscordNotificationService(HttpClient httpClient)
    {
        var botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(botToken))
            throw new InvalidOperationException("DISCORD_BOT_TOKEN is not configured.");

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://discord.com/api/v10/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", botToken);
    }

    public async Task SendDirectMessageAsync(string discordUserId, string message)
    {
        if (string.IsNullOrWhiteSpace(discordUserId))
            throw new ArgumentException("discordUserId is required.", nameof(discordUserId));

        // Step 1: open (or reuse) a DM channel with this user.
        var channelResponse = await _httpClient.PostAsJsonAsync(
            "users/@me/channels",
            new { recipient_id = discordUserId });

        if (!channelResponse.IsSuccessStatusCode)
        {
            var error = await channelResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to open Discord DM channel ({channelResponse.StatusCode}): {error}");
        }

        var channel = await channelResponse.Content.ReadFromJsonAsync<DiscordChannel>();
        if (channel?.Id == null)
            throw new InvalidOperationException("Discord did not return a DM channel id.");

        // Step 2: send the message into that channel.
        var messageResponse = await _httpClient.PostAsJsonAsync(
            $"channels/{channel.Id}/messages",
            new { content = message });

        if (!messageResponse.IsSuccessStatusCode)
        {
            var error = await messageResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to send Discord DM ({messageResponse.StatusCode}): {error}");
        }
    }

    public async Task SendChannelMessageAsync(string channelId, string message)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentException("channelId is required.", nameof(channelId));

        var response = await _httpClient.PostAsJsonAsync(
            $"channels/{channelId}/messages",
            new { content = message });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to post Discord channel message ({response.StatusCode}): {error}");
        }
    }
}

public record DiscordChannel(string? Id);
