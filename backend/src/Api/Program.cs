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
app.MapDiscordEndpoints();

app.Run();