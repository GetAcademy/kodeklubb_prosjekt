using Microsoft.Extensions.Hosting;
using Npgsql;
using Api;
using DotNetEnv;
using Persistence;
using Api.Endpoints;
using Api.Contracts;
using Persistence.DbModels;
using Worker;
using System.IO;
using Dapper;

// Load local .env values from an ancestor directory
var envPath = Directory.GetCurrentDirectory();
while (envPath != null)
{
    var candidate = Path.Combine(envPath, ".env");
    if (File.Exists(candidate))
    {
        DotNetEnv.Env.Load(candidate);
        Console.WriteLine($"Loaded .env from {candidate}");
        break;
    }

    var parentDir = Directory.GetParent(envPath);
    envPath = parentDir?.FullName;
}

if (envPath == null)
{
    Console.WriteLine($"WARNING: .env file not found in current or ancestor directories starting at {Directory.GetCurrentDirectory()}.");
}

var builder = WebApplication.CreateBuilder(args);

// --- 1. DATABASE CONFIGURATION ---
var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
             ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(rawUrl))
{
    throw new InvalidOperationException("No database connection string found in Environment or Configuration.");
}

var connectionString = rawUrl.Contains("://")
    ? ConvertConnectionString(rawUrl)
    : rawUrl;



AppConfig.Initialize(builder.Configuration);
AppConfig.ConnectionString = connectionString;

builder.Services.AddNpgsqlDataSource(connectionString);

// --- 2. OTHER SERVICES ---
builder.Services.AddCors();

// --- 2b. AUTH0 (validate access tokens) ---
// Authority = who made the token (our Auth0 tenant). Audience = who the token is for (our API).
// The middleware checks signature, issuer, audience and expiry. We write no token code ourselves.
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;
        options.MapInboundClaims = false; // keep claim names like "sub" as they are
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// --- 3. RESEND EMAIL ---
var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
// --- 3. RESEND EMAIL ---

Console.WriteLine($"[DEBUG] RESEND_API_KEY length: {resendApiKey?.Length ?? -1}");

//var resendFrom = "updates@updates.getacademy.no";
var resendFrom = "onboarding@resend.dev";
builder.Services.AddOptions();
builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.Configure<Resend.ResendClientOptions>(o => { o.ApiToken = resendApiKey!; });
builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.AddTransient<Core.Logic.IEmailService>(sp =>
    new Core.Logic.ResendEmailService(sp.GetRequiredService<Resend.IResend>(), resendFrom));
builder.Services.AddHttpClient<Core.Logic.IDiscordNotificationService, Core.Logic.DiscordNotificationService>();

// --- 4. OUTBOX WORKER ---
builder.Services.AddSingleton<IHostedService>(sp =>
    new OutboxWorker(connectionString, sp.GetRequiredService<IServiceScopeFactory>()));

Console.WriteLine("Outbox worker registered as hosted service.");

// --- 5. PORT CONFIGURATION ---
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

// --- 6. MIDDLEWARE & CORS ---
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",")
    ?? new[]
    {
        "https://kodeklubbprosjekt-production-8ee8.up.railway.app",
        "http://localhost:5173",
        "http://localhost:3000"
    };
app.UseCors(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

// Order matters: CORS first, then authentication, then authorization
app.UseAuthentication();
app.UseAuthorization();

// --- 7. RUN MIGRATIONS ---
try
{
    Console.WriteLine("Railway: Starting Database Migrations...");
    await Task.Delay(2000);
    var migrator = new DatabaseMigrator(connectionString);
    await migrator.MigrateAsync();
    Console.WriteLine("Railway: Migrations successful.");
}
catch (Exception ex)
{
    Console.WriteLine($"Migration Error: {ex.Message}");
}

// --- 8. MAP ENDPOINTS ---
app.MapUserEndpoints();
app.MapTeamEndpoints();
app.MapTagsEndpoints();
app.MapDiscordEndpoints();
app.MapGet("/", () => "API is online!");

app.Run();

// --- HELPER FUNCTION ---
string ConvertConnectionString(string url)
{
    if (string.IsNullOrEmpty(url)) return "";
    if (!url.Contains("://")) return url;

    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var pass = userInfo.Length > 1 ? userInfo[1] : "";

    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
}
