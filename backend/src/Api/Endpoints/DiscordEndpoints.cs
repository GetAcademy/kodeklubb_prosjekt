using System.Net.Http.Headers;
using System.Text.Json;
using Api.Contracts;
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
            var clientId = AppConfig.Configuration["Discord:ClientId"]!;
            var redirectUri = Uri.EscapeDataString(AppConfig.Configuration["Discord:RedirectUri"]!);
            var scope = "identify email";
            
            var url =
                $"https://discord.com/oauth2/authorize" +
                $"?client_id={clientId}" +
                $"&response_type=code" +
                $"&redirect_uri={redirectUri}" +
                $"&scope={scope}";

            return Results.Redirect(url);
        });
        app.MapGet("/auth/discord/callback", async (string? code, string? error, string? error_description, HttpContext context) =>
        {
            Console.WriteLine($"Discord callback received. Code: {!string.IsNullOrWhiteSpace(code)}, Error: {error}");

            var client = new HttpClient();

            if (string.IsNullOrWhiteSpace(code))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"Discord OAuth error: {error} - {error_description}");
                    var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
                    return Results.Redirect($"{frontendRedirect}?error={error}");
                }

                Console.WriteLine("Missing code parameter");
                var frontendErrorUrl = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
                return Results.Redirect($"{frontendErrorUrl}?error=missing_code");
            }

            try
            {
                Console.WriteLine("Exchanging code for token...");
                var tokenResponse = await client.PostAsync(
                    "https://discord.com/api/oauth2/token",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = AppConfig.Configuration["Discord:ClientId"]!,
                        ["client_secret"] = AppConfig.Configuration["Discord:ClientSecret"]!,
                        ["grant_type"] = "authorization_code",
                        ["code"] = code,
                        ["redirect_uri"] = AppConfig.Configuration["Discord:RedirectUri"]!
                    })
                );
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Token exchange failed: {tokenResponse.StatusCode} - {errorContent}");
                    var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
                    return Results.Redirect($"{frontendRedirect}?error=token_exchange_failed");
                }

                var tokenData = await tokenResponse.Content.ReadFromJsonAsync<DiscordTokenResponse>();

                if (tokenData?.AccessToken == null)
                {
                    Console.WriteLine("Failed to parse access token from response");
                    var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
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
                    var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
                    return Results.Redirect($"{frontendRedirect}?error=user_fetch_faield");
                }

                var discordUser = await userResponse.Content.ReadFromJsonAsync<DiscordUserResponse>();

                if (discordUser == null || string.IsNullOrWhiteSpace(discordUser.Id))
                {
                    Console.WriteLine("Failed to parse Discord user data");
                    var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
                    return Results.Redirect($"{frontendRedirect}?error=invalid_user_data");
                }

                await using var connection = await AppConfig.OpenConnectionAsync();

                var existingUser = await connection.QueryOneOrDefaultAsync<UserEntity>(
                    UserSql.GetByDiscordId,
                    new { DiscordId = discordUser.Id });


                UserEntity savedUser;

                if (existingUser == null)
                {
                    var avatarUrl = !string.IsNullOrWhiteSpace(discordUser.Avatar)
                        ? $"https://cdn.discordapp.com/avatars/{discordUser.Id}/{discordUser.Avatar}.png"
                        : "https://cdn.discordapp.com/embed/avatars/0.png";
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
                else
                {
                    savedUser = existingUser;
                    Console.WriteLine($"User already exists: {savedUser.Id} ({savedUser.Username})");
                }

                var frontendRedirectUrl = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
                var redirectUrl =
                    $"{frontendRedirectUrl}?token={Uri.EscapeDataString(tokenData.AccessToken)}&user={Uri.EscapeDataString(JsonSerializer.Serialize(discordUser))}";
                Console.WriteLine("Redirecting to frontend with token and user data");

                return Results.Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Discord callback: {ex.Message}");
                var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
                return Results.Redirect($"{frontendRedirect}?error=exception");
            }
        });
    }
}