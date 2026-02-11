using Api;
using DotNetEnv;
using Persistence;
using Api.Endpoints;
using Api.Contracts;
using Persistence.DbModels;
using Npgsql;
using Dapper;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional:false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional:true)
    .AddEnvironmentVariables();

// Store configuration for static access
AppConfig.Initialize(builder.Configuration);

// Add PostgreSQL database connection factory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
}

// Store connection string for static access
AppConfig.ConnectionString = connectionString;

// Register CORS service (required by CORS middleware)
builder.Services.AddCors();

var app = builder.Build();

// app.UseHttpsRedirection(); // Disabled for local development

app.UseCors(policy => policy
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

// Run database migrations on startup
if (!string.IsNullOrWhiteSpace(connectionString))
{
    var migrator = new DatabaseMigrator(connectionString);
    await migrator.MigrateAsync();
}

// Map user endpoints
app.MapUserEndpoints();
app.MapTeamEndpoints();

app.MapGet("/login", () => "It works");
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
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
        
        var userResponse = await client.GetAsync("https://discord.com/api/users/@me");
        
        if (!userResponse.IsSuccessStatusCode)
        {
            var errorContent = await userResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Failed to get user information: {userResponse.StatusCode} - {errorContent}");
            var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
            return Results.Redirect($"{frontendRedirect}?error=user_fetch_failed");
        }

        var discordUser = await userResponse.Content.ReadFromJsonAsync<DiscordUserResponse>();
        
        if (discordUser == null || string.IsNullOrWhiteSpace(discordUser.Id))
        {
            Console.WriteLine("Failed to parse Discord user data");
            var frontendRedirect = AppConfig.Configuration["Discord:FrontendRedirectUri"]!;
            return Results.Redirect($"{frontendRedirect}?error=invalid_user_data");
        }

        // Save or update user in database
        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        
        var getUserSql = SqlLoader.Load("Queries/Users_GetByDiscordId.sql");
        var existingUser = await connection.QueryFirstOrDefaultAsync<UserEntity>(
            getUserSql,
            new { DiscordId = discordUser.Id });
        
        UserEntity savedUser;
        
        if (existingUser == null)
        {
            // Create new user
            var avatarUrl = !string.IsNullOrWhiteSpace(discordUser.Avatar) 
                ? $"https://cdn.discordapp.com/avatars/{discordUser.Id}/{discordUser.Avatar}.png"
                : "https://cdn.discordapp.com/embed/avatars/0.png";
            
            var insertUserSql = SqlLoader.Load("Commands/Users_Insert.sql");
            savedUser = await connection.QuerySingleAsync<UserEntity>(
                insertUserSql,
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
        var redirectUrl = $"{frontendRedirectUrl}?token={Uri.EscapeDataString(tokenData.AccessToken)}&user={Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(discordUser))}";
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


app.MapGet("/dashboard", () =>
{
    
});
   

app.Run();