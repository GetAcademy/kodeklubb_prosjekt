using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Core.Logic;

public class DiscordNotificationService : IDiscordNotificationService
{
    private readonly HttpClient _httpClient;
    private string? _cachedBotUserId;

    public DiscordNotificationService(HttpClient httpClient)
    {
        var botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(botToken))
            throw new InvalidOperationException("DISCORD_BOT_TOKEN is not configured.");

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://discord.com/api/v10/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", botToken);
    }

    private async Task<string> GetBotUserIdAsync()
    {
        if (_cachedBotUserId != null)
            return _cachedBotUserId;

        var response = await _httpClient.GetAsync("users/@me");
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to look up bot's own user id ({response.StatusCode}): {error}");
        }

        var self = await response.Content.ReadFromJsonAsync<DiscordSelfUser>();
        if (self?.Id == null)
            throw new InvalidOperationException("Discord did not return the bot's own user id.");

        _cachedBotUserId = self.Id;
        return _cachedBotUserId;
    }

    public async Task SendDirectMessageAsync(string discordUserId, string message)
    {
        if (string.IsNullOrWhiteSpace(discordUserId))
            throw new ArgumentException("discordUserId is required.", nameof(discordUserId));

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

    public async Task<string> CreateRoleAsync(string guildId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(guildId))
            throw new ArgumentException("guildId is required.", nameof(guildId));

        var response = await _httpClient.PostAsJsonAsync(
            $"guilds/{guildId}/roles",
            new { name = roleName, mentionable = true });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to create Discord role ({response.StatusCode}): {error}");
        }

        var role = await response.Content.ReadFromJsonAsync<DiscordRole>();
        if (role?.Id == null)
            throw new InvalidOperationException("Discord did not return a role id.");

        return role.Id;
    }

    public async Task<string> CreateRestrictedChannelAsync(string guildId, string channelName, DiscordChannelType channelType, string roleId)
    {
        if (string.IsNullOrWhiteSpace(guildId))
            throw new ArgumentException("guildId is required.", nameof(guildId));

        const long ViewChannel = 1024;
        const long SendMessages = 2048;
        const long Connect = 1048576;

        var allowBitsForRole = channelType == DiscordChannelType.Voice
            ? (ViewChannel | Connect)
            : (ViewChannel | SendMessages);

        var botUserId = await GetBotUserIdAsync();

        var payload = new
        {
            name = channelName,
            type = (int)channelType,
            permission_overwrites = new object[]
            {
                // Deny @everyone from seeing the channel at all.
                new { id = guildId, type = 0, deny = ViewChannel.ToString() },
                // Grant the new team role access to view (and send/connect).
                new { id = roleId, type = 0, allow = allowBitsForRole.ToString() },
                // The bot itself is subject to these same overwrites unless it
                // has Administrator, so it must be explicitly granted access
                // too -- otherwise it creates a channel it can't post in.
                new { id = botUserId, type = 1, allow = (ViewChannel | SendMessages).ToString() }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"guilds/{guildId}/channels", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to create Discord channel ({response.StatusCode}): {error}");
        }

        var channel = await response.Content.ReadFromJsonAsync<DiscordChannel>();
        if (channel?.Id == null)
            throw new InvalidOperationException("Discord did not return a channel id.");

        return channel.Id;
    }

    public async Task AddRoleToMemberAsync(string guildId, string userId, string roleId)
    {
        if (string.IsNullOrWhiteSpace(guildId))
            throw new ArgumentException("guildId is required.", nameof(guildId));

        var response = await _httpClient.PutAsync(
            $"guilds/{guildId}/members/{userId}/roles/{roleId}",
            null);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to add Discord role to member ({response.StatusCode}): {error}");
        }
    }
}

public record DiscordChannel(string? Id);
public record DiscordRole(string? Id);
public record DiscordSelfUser(string? Id);
