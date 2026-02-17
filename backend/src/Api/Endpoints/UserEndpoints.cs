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
        group.MapGet("/tags/predefined", GetPredefinedTags).WithName("GetPredefinedTags");
        group.MapGet("/{discordId}/tags", GetUserTags).WithName("GetUserTags");
        group.MapPost("/{discordId}/tags", AddUserTags).WithName("AddUserTags");
    }

    private static async Task<IResult> GetAllUsers()
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        return Results.Ok(await connection.QueryManyAsync<UserEntity>(UserSql.GetAll));
    }

    private static async Task<IResult> GetUserById(Guid id)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var user = await connection.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetById, new { Id = id });
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> CreateUser(CreateUserRequest request)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var createdUser = await connection.QueryOneAsync<UserEntity>(UserSql.Insert, new { DiscordId = request.DiscordId ?? string.Empty, Username = request.Username ?? string.Empty, Email = request.Email, AvatarUrl = request.AvatarUrl, PreferencesJson = request.PreferencesJson });
        return Results.Created($"/api/users/{createdUser.Id}", createdUser);
    }

    private static async Task<IResult> GetPredefinedTags()
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        return Results.Ok(await connection.QueryManyAsync(UserSql.GetPredefinedTags));
    }

    private static async Task<IResult> GetUserTags(string discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });

        await using var connection = await AppConfig.OpenConnectionAsync();
        return Results.Ok(await connection.QueryManyAsync(UserSql.GetUserPredefinedTagsByDiscordId, new { DiscordId = discordId }));
    }

    private static async Task<IResult> AddUserTags(string discordId, UpdateUserTagsRequest request)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });
        if (request.TagIds == null || request.TagIds.Length == 0) return Results.BadRequest(new { message = "At least one tag ID is required" });

        await using var connection = await AppConfig.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var user = await connection.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetByDiscordId, new { DiscordId = discordId }, transaction);
            if (user == null) { await transaction.RollbackAsync(); return Results.NotFound(new { message = "User not found" }); }
            foreach (var tagId in request.TagIds)
                await connection.ExecuteCommandAsync(UserSql.InsertUserPredefinedTag, new { UserId = user.Id, PredefinedTagId = tagId }, transaction);
            await transaction.CommitAsync();
            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception) { await transaction.RollbackAsync(); throw; }
    }
}

public record CreateUserRequest(string? DiscordId, string? Email, string? Username, string? AvatarUrl, string? PreferencesJson);
public record UpdateUserTagsRequest(Guid[] TagIds);