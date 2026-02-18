using Api;
using Persistence;
using Persistence.DbModels;
using Npgsql;
using Dapper;
using Endpoints.Users;

namespace Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithName("Users");

        group.MapGet("/", Endpoints.Users.GetAllUsers).WithName("GetAllUsers");
        group.MapGet("/{id}", Endpoints.Users.GetUserById).WithName("GetUserById");
        group.MapPost("/", Endpoints.Users.CreateUser).WithName("CreateUser");
        group.MapGet("/tags/predefined", Endpoints.Users.GetPredefinedTags).WithName("GetPredefinedTags");
        group.MapGet("/{discordId}/tags", Endpoints.Users.GetUserTags).WithName("GetUserTags");
        group.MapPost("/{discordId}/tags", Endpoints.Users.AddUserTags).WithName("AddUserTags");
    }

    private static async Task<IResult> GetPredefinedTags()
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        return Results.Ok(await connection.QueryManyAsync(UserSql.GetPredefinedTags));
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