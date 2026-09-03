using System.Net.Http.Headers;
using System.Text.Json;
using Api.Contracts;
using Dapper;
using Npgsql;
using Persistence;
using Persistence.DbModels;

namespace Api.Endpoints;

public static class DiscordEndpoints
{
    public static void MapDiscordEndpoints(this WebApplication app)
    {
        app.MapGet("/login", () => "It works!");
        app.MapGet("/auth/discord/login", () =>
        {
            var clientId = GetDiscordConfig("ClientId");
            var redirectUri = Uri.EscapeDataString(GetDiscordConfig("RedirectUri"));
            var scope = Uri.EscapeDataString("identify email");
            
            Console.WriteLine($"Discord login redirect: clientId={clientId}, redirectUri={redirectUri}, scope={scope}");

            var url =
                $"https://discord.com/oauth2/authorize" +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                $"&response_type=code" +
                $"&redirect_uri={redirectUri}" +
                $"&scope={scope}";

            return Results.Redirect(url);
        });
        app.MapGet("/auth/discord/callback", async (string? code, string? error, string? error_description) =>
        {
            Console.WriteLine($"Discord callback received. Code: {!string.IsNullOrWhiteSpace(code)}, Error: {error}");

            var client = new HttpClient();

            if (string.IsNullOrWhiteSpace(code))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"Discord OAuth error: {error} - {error_description}");
                    var frontendRedirect = GetDiscordConfig("FrontendRedirectUri");
                    return Results.Redirect($"{frontendRedirect}?error={error}");
                }

                Console.WriteLine("Missing code parameter");
                var frontendErrorUrl = GetDiscordConfig("FrontendRedirectUri");
                return Results.Redirect($"{frontendErrorUrl}?error=missing_code");
            }

            try
            {
                Console.WriteLine("Exchanging code for token...");
                var tokenResponse = await client.PostAsync(
                    "https://discord.com/api/oauth2/token",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = GetDiscordConfig("ClientId"),
                        ["client_secret"] = GetDiscordConfig("ClientSecret"),
                        ["grant_type"] = "authorization_code",
                        ["code"] = code,
                        ["redirect_uri"] = GetDiscordConfig("RedirectUri")
                    })
                );
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Token exchange failed: {tokenResponse.StatusCode} - {errorContent}");
                    var frontendRedirect = GetDiscordConfig("FrontendRedirectUri");
                    return Results.Redirect($"{frontendRedirect}?error=token_exchange_failed");
                }

                var tokenData = await tokenResponse.Content.ReadFromJsonAsync<DiscordTokenResponse>();

                if (tokenData?.AccessToken == null)
                {
                    Console.WriteLine("Failed to parse access token from response");
                    var frontendRedirect = GetDiscordConfig("FrontendRedirectUri");
                    return Results.Redirect($"{frontendRedirect}?error=no_access_token");
                }

                Console.WriteLine("Token received, fetching user data...");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);

                var userResponse = await client.GetAsync("https://discord.com/api/users/@me");

                if (!userResponse.IsSuccessStatusCode)
                {
                    var errorContent = await userResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to get user information: {userResponse.StatusCode} - {errorContent}");
                    var frontendRedirect = GetDiscordConfig("FrontendRedirectUri");
                    return Results.Redirect($"{frontendRedirect}?error=user_fetch_failed");
                }

                var discordUser = await userResponse.Content.ReadFromJsonAsync<DiscordUserResponse>();

                if (discordUser == null || string.IsNullOrWhiteSpace(discordUser.Id))
                {
                    Console.WriteLine("Failed to parse Discord user data");
                    var frontendRedirect = GetDiscordConfig("FrontendRedirectUri");
                    return Results.Redirect($"{frontendRedirect}?error=user_data_failed");
                }

                await using var connection = await AppConfig.OpenConnectionAsync();

                Console.WriteLine($"Checking if user with Discord ID {discordUser.Id} exists...");
                var existingUser = await connection.QueryOneOrDefaultAsync<UserEntity>(
                    UserSql.GetByDiscordId,
                    new { DiscordId = discordUser.Id });

                UserEntity savedUser;

                if (existingUser == null)
                {
                    Console.WriteLine($"User does not exist, creating new user for Discord ID: {discordUser.Id}");
                    var avatarUrl = !string.IsNullOrWhiteSpace(discordUser.Avatar)
                        ? $"https://cdn.discordapp.com/avatars/{discordUser.Id}/{discordUser.Avatar}.png"
                        : "https://cdn.discordapp.com/embed/avatars/0.png";
                    try
                    {
                        savedUser = await connection.QueryOneAsync<UserEntity>(
                            UserSql.Insert,
                            new
                            {
                                DiscordId = discordUser.Id,
                                Username = discordUser.Username,
                                Email = discordUser.Email,
                                AvatarUrl = avatarUrl,
                                PreferencesJson = (string?)null
                            });
                        Console.WriteLine($"Created new user: {savedUser.Id} ({savedUser.Username})");
                    }
                    catch (PostgresException pgEx) when (pgEx.SqlState == "23505") // Unique constraint violation
                    {
                        Console.WriteLine($"Duplicate user detected (race condition): {pgEx.Message}");
                        // Re-query to get the user that was just created by another request
                        var retryUser = await connection.QueryOneOrDefaultAsync<UserEntity>(
                            UserSql.GetByDiscordId,
                            new { DiscordId = discordUser.Id });
                        if (retryUser != null)
                        {
                            savedUser = retryUser;
                            Console.WriteLine($"Retrieved existing user after race condition: {savedUser.Id} ({savedUser.Username})");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    savedUser = existingUser;
                    Console.WriteLine($"User already exists: {savedUser.Id} ({savedUser.Username})");
                }

                var frontendRedirectUrl = GetDiscordConfig("FrontendRedirectUri");
                var redirectUrl =
                    $"{frontendRedirectUrl}?token={Uri.EscapeDataString(tokenData.AccessToken)}&user={Uri.EscapeDataString(JsonSerializer.Serialize(discordUser))}";
                Console.WriteLine("Redirecting to frontend with token and user data");

                return Results.Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Discord callback: {ex.Message}");
                var frontendRedirect = GetDiscordConfig("FrontendRedirectUri");
                return Results.Redirect($"{frontendRedirect}?error=exception");
            }
        });
    }

    private static string GetDiscordConfig(string key)
    {
        var envKey = $"Discord__{key}";
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(envValue)) return envValue;

        var configValue = AppConfig.Configuration[$"Discord:{key}"];
        if (!string.IsNullOrWhiteSpace(configValue)) return configValue;

        throw new InvalidOperationException($"Discord configuration '{key}' is missing. Set {envKey} or Discord:{key}.");
    }
}
