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

        group.MapGet("/", () => GetAllUsers()).WithName("GetAllUsers");
        group.MapGet("/{id}", (Guid id) => GetUserById(id)).WithName("GetUserById");
        group.MapPost("/", (CreateUserRequest request) => CreateUser(request)).WithName("CreateUser");
        group.MapGet("/tags/predefined", () => GetPredefinedTags()).WithName("GetPredefinedTags");
        group.MapGet("/{discordId}/tags", (string discordId) => GetUserTags(discordId)).WithName("GetUserTags");
        group.MapPost("/{discordId}/tags", (string discordId, UpdateUserTagsRequest request) => AddUserTags(discordId, request)).WithName("AddUserTags");
    }


    private static async Task<IResult> GetAllUsers()
    {
        await using var db = await Handlers.DbSession.OpenAsync();
        return Results.Ok(await db.QueryAsync<UserEntity>(UserSql.GetAll()));
    }


    private static async Task<IResult> GetUserById(Guid id)
    {
        await using var db = await Handlers.DbSession.OpenAsync();
        var user = await db.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetById(), new { Id = id });
        return user is null ? Results.NotFound() : Results.Ok(user);
    }


    private static async Task<IResult> CreateUser(CreateUserRequest request)
    {
        await using var db = await Handlers.DbSession.OpenAsync();
        var createdUser = await db.QueryOneAsync<UserEntity>(UserSql.Insert(), new { DiscordId = request.DiscordId ?? string.Empty, Username = request.Username ?? string.Empty, Email = request.Email, AvatarUrl = request.AvatarUrl, PreferencesJson = request.PreferencesJson });
        await db.CommitAsync();
        return Results.Created($"/api/users/{createdUser.Id}", createdUser);
    }


    private static async Task<IResult> GetPredefinedTags()
    {
        await using var db = await Handlers.DbSession.OpenAsync();
        return Results.Ok(await db.QueryAsync<dynamic>(UserSql.GetPredefinedTags()));
    }


    private static async Task<IResult> GetUserTags(string discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });
        await using var db = await Handlers.DbSession.OpenAsync();
        return Results.Ok(await db.QueryAsync<dynamic>(UserSql.GetUserPredefinedTagsByDiscordId(), new { DiscordId = discordId }));
    }


    private static async Task<IResult> AddUserTags(string discordId, UpdateUserTagsRequest request)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });
        if (request.TagIds == null || request.TagIds.Length == 0) return Results.BadRequest(new { message = "At least one tag ID is required" });

        await using var db = await Handlers.DbSession.OpenAsync();
        try
        {
            var user = await db.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetByDiscordId(), new { DiscordId = discordId });
            if (user == null) { await db.Tx.RollbackAsync(); return Results.NotFound(new { message = "User not found" }); }
            foreach (var tagId in request.TagIds)
                await db.ExecuteAsync(UserSql.InsertUserPredefinedTag(), new { UserId = user.Id, PredefinedTagId = tagId });
            await db.CommitAsync();
            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception) { await db.Tx.RollbackAsync(); throw; }
    }
}

public record CreateUserRequest(string? DiscordId, string? Email, string? Username, string? AvatarUrl, string? PreferencesJson);
public record UpdateUserTagsRequest(Guid[] TagIds);