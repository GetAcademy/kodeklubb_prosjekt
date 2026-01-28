using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

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
app.UseHttpsRedirection();

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

app.MapGet("/auth/discord/callback", async (string? code, string? error, string? error_description, IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();

    if (string.IsNullOrWhiteSpace(code))
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Results.BadRequest($"Discord OAuth error: {error}. {error_description}");
        }

        return Results.BadRequest("Missing code query parameter");
    }

    
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
        return Results.BadRequest("Token exchange failed");
    }

    var tokenData = await tokenResponse.Content.ReadFromJsonAsync<DiscordTokenResponse>();
    
    if (tokenData?.AccessToken == null)
    {
        return Results.BadRequest("Failed to get access token");
    }

    
    client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
    
    var userResponse = await client.GetAsync("https://discord.com/api/users/@me");
    
    if (!userResponse.IsSuccessStatusCode)
    {
        return Results.BadRequest("Failed to get user information");
    }

    var userData = await userResponse.Content.ReadFromJsonAsync<object>();
    
    return Results.Redirect($"{config["Discord:RedirectUri"]}?token={tokenData.AccessToken}&user={Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(userData))}");
});


app.MapGet("/dashboard", () =>
{
    
});
   

app.Run();