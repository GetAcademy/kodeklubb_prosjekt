using Npgsql;
using Api;
using DotNetEnv;
using Persistence;
using Api.Endpoints;
using Api.Contracts;
using Persistence.DbModels;

using Dapper;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// --- 1. DATABASE CONFIGURATION ---
// Priority 1: Railway Environment Variable
// Priority 2: Local appsettings.json
var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
             ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(rawUrl))
{
    throw new InvalidOperationException("No database connection string found in Environment or Configuration.");
}

// Convert the URL (postgres://...) to Npgsql format (Host=...)
var connectionString = ConvertConnectionString(rawUrl);

// Store for your existing AppConfig static class
AppConfig.Initialize(builder.Configuration);
AppConfig.ConnectionString = connectionString;

// Register the Npgsql DataSource for Dapper
builder.Services.AddNpgsqlDataSource(connectionString);

// --- 2. OTHER SERVICES ---
builder.Services.AddCors();

var resendApiKey = builder.Configuration["RESEND_API_KEY"] ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");
var resendFrom = builder.Configuration["RESEND_FROM_EMAIL"] ?? "updates@updates.getacademy.no";

builder.Services.AddOptions();
builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.Configure<Resend.ResendClientOptions>(o => { o.ApiToken = resendApiKey!; });
builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.AddTransient<Core.Logic.IEmailService>(sp =>
    new Core.Logic.ResendEmailService(sp.GetRequiredService<Resend.IResend>(), resendFrom));

var app = builder.Build();

// --- 3. MIDDLEWARE & CORS ---
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",") ?? new[] { "https://kodeklubbprosjekt-production-8ee8.up.railway.app/" };
app.UseCors(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

// --- 4. RUN MIGRATIONS ---
try 
{
    Console.WriteLine("Railway: Starting Database Migrations...");
    // Wait 2 seconds to ensure Railway's internal network is fully resolved
    await Task.Delay(2000); 
    var migrator = new DatabaseMigrator(connectionString);
    await migrator.MigrateAsync();
    Console.WriteLine("Railway: Migrations successful.");
}
catch (Exception ex)
{
    Console.WriteLine($"Migration Error: {ex.Message}");
}

// Map Endpoints
app.MapUserEndpoints();
app.MapTeamEndpoints();
app.MapDiscordEndpoints();
app.MapGet("/", () => "API is online!");
app.Run();

// --- HELPER FUNCTION (at the very bottom) ---
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