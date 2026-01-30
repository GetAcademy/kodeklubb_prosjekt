using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using Api.Endpoints;
using Worker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
// builder.Services.AddHostedService<OutboxWorker>();

// Add PostgreSQL database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("DevCors");
// app.UseHttpsRedirection(); // Disabled for local development

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
// Map user endpoints
app.MapUserEndpoints();

app.MapGet("/login", () => "It works");
app.MapGet("/auth/discord/login", (IConfiguration config) =>
{
    var clientId = config["Discord:ClientId"]!;
    var redirectUri = Uri.EscapeDataString(config["Discord:RedirectUri"]!);
    var scope = "identify email";

    var url =
        $"https://discord.com/oauth2/authorize" +
        $"?client_id={clientId}" +
        $"&response_type=code" +
        $"&redirect_uri={redirectUri}" +
        $"&scope={scope}";

    return Results.Redirect(url);
});

app.MapGet("/auth/discord/callback", async (string? code, string? error, string? error_description, IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    logger.LogInformation("Discord callback received. Code: {HasCode}, Error: {Error}", !string.IsNullOrWhiteSpace(code), error);
    
    var client = httpClientFactory.CreateClient();

    if (string.IsNullOrWhiteSpace(code))
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            logger.LogError("Discord OAuth error: {Error} - {Description}", error, error_description);
            var frontendRedirect = config["Discord:FrontendRedirectUri"]!;
            return Results.Redirect($"{frontendRedirect}?error={error}");
        }

        logger.LogError("Missing code parameter");
        var frontendErrorUrl = config["Discord:FrontendRedirectUri"]!;
        return Results.Redirect($"{frontendErrorUrl}?error=missing_code");
    }

    try
    {
        logger.LogInformation("Exchanging code for token...");
        var tokenResponse = await client.PostAsync(
            "https://discord.com/api/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = config["Discord:ClientId"]!,
                ["client_secret"] = config["Discord:ClientSecret"]!,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = config["Discord:RedirectUri"]!
            })
        );

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            logger.LogError("Token exchange failed: {StatusCode} - {Content}", tokenResponse.StatusCode, errorContent);
            var frontendRedirect = config["Discord:FrontendRedirectUri"]!;
            return Results.Redirect($"{frontendRedirect}?error=token_exchange_failed");
        }

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<DiscordTokenResponse>();
        
        if (tokenData?.AccessToken == null)
        {
            logger.LogError("Failed to parse access token from response");
            var frontendRedirect = config["Discord:FrontendRedirectUri"]!;
            return Results.Redirect($"{frontendRedirect}?error=no_access_token");
        }

        logger.LogInformation("Token received, fetching user data...");
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
        
        var userResponse = await client.GetAsync("https://discord.com/api/users/@me");
        
        if (!userResponse.IsSuccessStatusCode)
        {
            var errorContent = await userResponse.Content.ReadAsStringAsync();
            logger.LogError("Failed to get user information: {StatusCode} - {Content}", userResponse.StatusCode, errorContent);
            var frontendRedirect = config["Discord:FrontendRedirectUri"]!;
            return Results.Redirect($"{frontendRedirect}?error=user_fetch_failed");
        }

        var userData = await userResponse.Content.ReadFromJsonAsync<object>();
        
        var frontendRedirectUrl = config["Discord:FrontendRedirectUri"]!;
        var redirectUrl = $"{frontendRedirectUrl}?token={Uri.EscapeDataString(tokenData.AccessToken)}&user={Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(userData))}";
        logger.LogInformation("Redirecting to frontend with token and user data");
        
        return Results.Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Exception in Discord callback");
        var frontendRedirect = config["Discord:FrontendRedirectUri"]!;
        return Results.Redirect($"{frontendRedirect}?error=exception");
    }
});


app.MapGet("/dashboard", () =>
{
    
});
   

app.Run();