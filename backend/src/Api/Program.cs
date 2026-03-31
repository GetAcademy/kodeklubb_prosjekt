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

// --- DATABASE CONFIGURATION START ---

// 1. Get the Railway URL (or fallback to Local)
var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
             ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(rawUrl))
{
    throw new InvalidOperationException("No database connection string found.");
}

// 2. Convert to Npgsql format using our helper
var connectionString = ConvertConnectionString(rawUrl);

// 3. Store for static access (as your code expects)
AppConfig.Initialize(builder.Configuration);
AppConfig.ConnectionString = connectionString;

// 4. Register Npgsql services
builder.Services.AddNpgsqlDataSource(connectionString);

// --- DATABASE CONFIGURATION END ---

builder.Services.AddCors();

// Resend Service Configuration
var resendApiKey = builder.Configuration["RESEND_API_KEY"] ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");
var resendFrom = builder.Configuration["RESEND_FROM_EMAIL"] ?? "updates@updates.getacademy.no";

builder.Services.AddOptions();
builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.Configure<Resend.ResendClientOptions>(o => { o.ApiToken = resendApiKey!; });
builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.AddTransient<Core.Logic.IEmailService>(sp =>
    new Core.Logic.ResendEmailService(sp.GetRequiredService<Resend.IResend>(), resendFrom));

var app = builder.Build();

// Configure CORS
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",") ?? new[] { "http://localhost:3000" };
app.UseCors(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

// --- RUN MIGRATIONS ---
// Wrap in a try-catch with a small delay for Railway's network to wake up
try 
{
    Console.WriteLine("Starting Database Migrations...");
    await Task.Delay(2000); // Give Railway 2 seconds to stabilize
    var migrator = new DatabaseMigrator(connectionString);
    await migrator.MigrateAsync();
    Console.WriteLine("Migrations completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Migration failed: {ex.Message}");
    // Don't crash the whole app if migrations fail once, 
    // or let it crash if migrations are critical.
}

// Map endpoints
app.MapUserEndpoints();
app.MapTeamEndpoints();
app.MapDiscordEndpoints();

app.Run();

// --- THE HELPER FUNCTION ---
string ConvertConnectionString(string databaseUrl)
{
    if (string.IsNullOrEmpty(databaseUrl)) return "";
    
    // If it's already in Key=Value format, return as is
    if (!databaseUrl.Contains("://")) return databaseUrl;

    // Convert postgres://user:pass@host:port/db to Npgsql format
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";

    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}