using Microsoft.EntityFrameworkCore;
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


// Register Resend .NET SDK services
var resendApiKey = builder.Configuration["RESEND_API_KEY"] ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");
var resendFrom = builder.Configuration["RESEND_FROM_EMAIL"] ?? "updates@updates.getacademy.no";
builder.Services.AddOptions();
builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.Configure<Resend.ResendClientOptions>(o =>
{
    o.ApiToken = resendApiKey!;
});
builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.AddTransient<Core.Logic.IEmailService>(sp =>
    new Core.Logic.ResendEmailService(
        sp.GetRequiredService<Resend.IResend>(),
        resendFrom
    )
);

var app = builder.Build();

// app.UseHttpsRedirection(); // Disabled for local development

// Configure CORS with environment-specific origins
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",") ?? new[] { "http://localhost:3000" };
app.UseCors(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

// Run database migrations on startup
if (!string.IsNullOrWhiteSpace(connectionString))
{
    var migrator = new DatabaseMigrator(connectionString);
    await migrator.MigrateAsync();
}

 

var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

// If it starts with postgresql://, we need to convert it
if (rawUrl != null && rawUrl.StartsWith("postgres://")) 
{
    var uri = new Uri(rawUrl);
    var userInfo = uri.UserInfo.Split(':');
    rawUrl = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseNpgsql(rawUrl));
// Map user endpoints
app.MapUserEndpoints();
app.MapTeamEndpoints();
app.MapDiscordEndpoints();

app.Run();
string ConvertConnectionString(string databaseUrl)
{
    // If it's already in .NET format, just return it
    if (!databaseUrl.Contains("://")) return databaseUrl;

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}