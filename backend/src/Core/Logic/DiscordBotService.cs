using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Core.Logic
{
    public interface IDiscordBotService
    {
        Task AddUserToGuildAsync(string guildId, string discordUserId, string userOAuthToken);
        Task SendDirectMessageAsync(string discordUserId, string message);
    }

    public class DiscordBotService : IDiscordBotService
    {
        private readonly HttpClient _http;
        private readonly string _botToken;

        public DiscordBotService(string botToken)
        {
            _botToken = botToken;
            _http = new HttpClient();
        }

        public async Task AddUserToGuildAsync(string guildId, string discordUserId, string userOAuthToken)
        {
            var payload = JsonSerializer.Serialize(new { access_token = userOAuthToken });
            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"https://discord.com/api/v10/guilds/{guildId}/members/{discordUserId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);

            // 201 = added, 204 = already a member — both are fine
            if (response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Failed to add user {discordUserId} to guild {guildId}: {response.StatusCode} - {err}");
            }
        }

        public async Task SendDirectMessageAsync(string discordUserId, string message)
        {
            var createReq = new HttpRequestMessage(
                HttpMethod.Post, "https://discord.com/api/v10/users/@me/channels");
            createReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
            createReq.Content = new StringContent(
                JsonSerializer.Serialize(new { recipient_id = discordUserId }),
                Encoding.UTF8, "application/json");

            var dmResp = await _http.SendAsync(createReq);
            if (!dmResp.IsSuccessStatusCode)
            {
                var err = await dmResp.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Failed to create DM channel for {discordUserId}: {dmResp.StatusCode} - {err}");
            }

            var dmJson = await dmResp.Content.ReadAsStringAsync();
            using var dmDoc = JsonDocument.Parse(dmJson);
            var channelId = dmDoc.RootElement.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("DM channel ID missing.");

            var msgReq = new HttpRequestMessage(
                HttpMethod.Post, $"https://discord.com/api/v10/channels/{channelId}/messages");
            msgReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
            msgReq.Content = new StringContent(
                JsonSerializer.Serialize(new { content = message }),
                Encoding.UTF8, "application/json");

            var msgResp = await _http.SendAsync(msgReq);
            if (!msgResp.IsSuccessStatusCode)
            {
                var err = await msgResp.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Failed to send DM to {channelId}: {msgResp.StatusCode} - {err}");
            }
        }
    }

    public class NoOpDiscordBotService : IDiscordBotService
    {
        public Task AddUserToGuildAsync(string guildId, string discordUserId, string userOAuthToken)
        {
            Console.WriteLine($"[DISCORD BOT] No bot token configured. Skipping guild join for {discordUserId} → {guildId}.");
            return Task.CompletedTask;
        }

        public Task SendDirectMessageAsync(string discordUserId, string message)
        {
            Console.WriteLine($"[DISCORD BOT] No bot token configured. Skipping DM to {discordUserId}.");
            return Task.CompletedTask;
        }
    }
}