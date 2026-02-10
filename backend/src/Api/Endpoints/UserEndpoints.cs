using Api;
using Persistence;
using Persistence.DbModels;
using Npgsql;
using Dapper;

namespace Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithName("Users");

        group.MapGet("/", GetAllUsers).WithName("GetAllUsers");
        group.MapGet("/{id}", GetUserById).WithName("GetUserById");
        group.MapPost("/", CreateUser).WithName("CreateUser");
    }

    private static async Task<IResult> GetAllUsers()
    {
        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        
        var sql = SqlLoader.Load("Queries/Users_GetAll.sql");
        var users = await connection.QueryAsync<UserEntity>(sql);
        
        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserById(Guid id)
    {
        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        
        var sql = SqlLoader.Load("Queries/Users_GetById.sql");
        var user = await connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { Id = id });
        
        if (user is null)
            return Results.NotFound();

        return Results.Ok(user);
    }

    private static async Task<IResult> CreateUser(CreateUserRequest request)
    {
        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        
        var sql = SqlLoader.Load("Commands/Users_Insert.sql");
        var createdUser = await connection.QuerySingleAsync<UserEntity>(
            sql,
            new
            {
                DiscordId = request.DiscordId ?? string.Empty,
                Username = request.Username ?? string.Empty,
                Email = request.Email,
                AvatarUrl = request.AvatarUrl,
                PreferencesJson = request.PreferencesJson
            });

        return Results.Created($"/api/users/{createdUser.Id}", createdUser);
    }
}

public record CreateUserRequest(string? DiscordId, string? Email, string? Username, string? AvatarUrl, string? PreferencesJson);

