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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

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

app.UseHttpsRedirection();

// Map user endpoints
app.MapUserEndpoints();

app.MapGet("/auth/discord/login", (IConfiguration config) =>
{
    var clientId = config["Discord:ClientId"];
    var redirectUri = Uri.EscapeDataString(config["Discord:RedirectUri"]);
    var scope = "identify email";

    var url =
        $"https://discord.com/oauth2/authorize" +
        $"?client_id={clientId}" +
        $"&response_type=code" +
        $"&redirect_uri={redirectUri}" +
        $"&scope={scope}";

    return Results.Redirect(url);
});

app.MapGet("/auth/discord/callback", async (string code, IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();

    var tokenResponse = await client.PostAsync(
        "https://discord.com/api/oauth2/token",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = config["Discord:ClientId"]!,
            ["client_secret"] = config["Discord:ClientSecret"],
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = config["Discord:RedirectUri"]
        })
    );

    if (!tokenResponse.IsSuccessStatusCode)
    {
        return Results.BadRequest("Token exchange failed");
    }

    var rawJson = await tokenResponse.Content.ReadAsStringAsync();
    return Results.Ok(rawJson);
});

app.MapGet("/login", () =>
{
    //login
});

app.MapGet("/dashboard", () =>
{
    
});
   

app.Run();