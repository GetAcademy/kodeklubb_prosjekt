using Api;
using Persistence;
using Persistence.DbModels;
using Npgsql;
using Dapper;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;


namespace Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
               var group = app.MapGroup("/api/users").WithName("Users");

        // --- Current user ---
        group.MapGet("/me", GetCurrentUser).WithName("GetCurrentUser").RequireAuthorization();

        // --- Tags ---
        group.MapGet("/{discordId}/tags", GetUserTags).WithName("GetUserTags");
        group.MapPost("/{discordId}/tags", AddUserTags).WithName("AddUserTags");
        group.MapDelete("/{discordId}/tags/{tagId:guid}", RemoveUserTag).WithName("RemoveUserTag");

        // --- Discord account linking ---
        group.MapPost("/{discordId}/discord/link", LinkDiscordAccount).WithName("LinkDiscordAccount");
        group.MapDelete("/{discordId}/discord/unlink", UnlinkDiscordAccount).WithName("UnlinkDiscordAccount");
        group.MapGet("/{discordId}/discord/status", GetDiscordAccountStatus).WithName("GetDiscordAccountStatus");
    }
    // ========== User CRUD ==========

   
        // ========== Tags ==========
    // SQL lives in Persistence (UserSql, TagsSql) rather than inline here.

        private static async Task<IResult> GetUserTags(string discordId, ClaimsPrincipal principal)
    {
        var denied = await CheckOwnerAsync(principal, discordId);
        if (denied != null) return denied;
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });
        await using var db = await Handlers.DbSession.OpenAsync();
        return Results.Ok(await db.QueryAsync<dynamic>(UserSql.GetUserPredefinedTagsByDiscordId(), new { DiscordId = discordId }));
    }


    private static async Task<IResult>AddUserTags(string discordId, UpdateUserTagsRequest request, ClaimsPrincipal principal)
    {
        var denied = await CheckOwnerAsync(principal, discordId);
        if (denied != null) return denied;
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });
        if (request.Selections == null || request.Selections.Length == 0)
            return Results.BadRequest(new { message = "At least one tag selection is required" });

        await using var db = await Handlers.DbSession.OpenAsync();
        try
        {
            var user = await db.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetByDiscordId(), new { DiscordId = discordId });
            if (user == null) { await db.RollbackAsync(); return Results.NotFound(new { message = "User not found" }); }

            foreach (var selection in request.Selections)
            {
                var tagExists = await db.QuerySingleAsync<bool>(
                    TagsSql.CheckExists(),
                    new { TagId = selection.TagId });

                if (!tagExists)
                    throw new InvalidOperationException($"Tag '{selection.TagId}' does not exist.");

                await db.ExecuteAsync(
                    UserSql.InsertUserPredefinedTag(),
                    new { UserId = user.Id, PredefinedTagId = selection.TagId, LevelTagId = selection.LevelTagId });
            }

            await db.CommitAsync();
            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception) { await db.RollbackAsync(); throw; }
    }

    private static async Task<IResult> RemoveUserTag(string discordId, Guid tagId, ClaimsPrincipal principal)
    {
            var denied = await CheckOwnerAsync(principal, discordId);
    if (denied != null) return denied;
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });

        await using var db = await Handlers.DbSession.OpenAsync();
        try
        {
            await db.ExecuteAsync(UserSql.DeleteUserTag(), new { DiscordId = discordId, TagId = tagId });
            await db.CommitAsync();
            return Results.Ok(new { message = "Tag removed successfully" });
        }
        catch (Exception ex)
        {
            await db.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // ========== Discord Account Linking ==========

    private static async Task<IResult> LinkDiscordAccount(string discordId, ClaimsPrincipal principal)
    {
            var denied = await CheckOwnerAsync(principal, discordId);
    if (denied != null) return denied;
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var connection = await AppConfig.OpenConnectionAsync();
        try
        {
            var user = await connection.QueryOneOrDefaultAsync<UserEntity>(
                UserSql.GetByDiscordId(), new { DiscordId = discordId });
            if (user == null)
                return Results.NotFound(new { message = "User not found" });

            return Results.Ok(new { message = "Discord account is linked", userId = user.Id, discordId = user.DiscordId });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult>UnlinkDiscordAccount(string discordId, ClaimsPrincipal principal)
    {
            var denied = await CheckOwnerAsync(principal, discordId);
    if (denied != null) return denied;
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var db = await Handlers.DbSession.OpenAsync();
        try
        {
            var user = await db.QueryOneOrDefaultAsync<UserEntity>(
                UserSql.GetByDiscordId(), new { DiscordId = discordId });
            if (user == null)
            {
                await db.RollbackAsync();
                return Results.NotFound(new { message = "User not found" });
            }

            await db.ExecuteAsync(UserSql.DeleteAllUserTagsForUser(), new { UserId = user.Id });
            await db.ExecuteAsync(UserSql.DeleteUser(), new { UserId = user.Id });
            
            await db.CommitAsync();

            return Results.Ok(new { message = "Account unlinked. Please log in again." });
        }
        catch (Exception ex)
        {
            await db.RollbackAsync();
            return Results.BadRequest(new { message = $"Error: {ex.Message}" });
        }
    }

    // Who is calling? The answer comes from the validated Auth0 token, never from the request.
    // "sub" = Auth0 identity. "discord_id" = custom claim added by our Auth0 Action.
    private const string ClaimNamespace = "https://kodeklubb.no/";

    private static async Task<IResult> GetCurrentUser(ClaimsPrincipal principal)
    {
        var auth0UserId = principal.FindFirst("sub")?.Value;
        var discordId = principal.FindFirst(ClaimNamespace + "discord_id")?.Value;

        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "The token has no discord_id claim" });

        await using var connection = await AppConfig.OpenConnectionAsync();

        // 1. Normal case: we already know this Auth0 identity
        var user = await connection.QueryOneOrDefaultAsync<UserEntity>(
            UserSql.GetByAuth0UserId(), new { Auth0UserId = auth0UserId });

        // 2. First login for an existing user: find by DiscordId and save the Auth0 id
        if (user == null)
        {
            user = await connection.QueryOneOrDefaultAsync<UserEntity>(
                UserSql.GetByDiscordId(), new { DiscordId = discordId });
            if (user != null)
                user = await connection.QuerySingleAsync<UserEntity>(
                    UserSql.SetAuth0UserId(), new { Id = user.Id, Auth0UserId = auth0UserId });
        }

        // 3. Brand new user: create with both identities
        if (user == null)
        {
            var username = principal.FindFirst(ClaimNamespace + "username")?.Value;
            user = await connection.QuerySingleAsync<UserEntity>(
                UserSql.InsertFromAuth0(),
                new { DiscordId = discordId, Auth0UserId = auth0UserId, Username = string.IsNullOrWhiteSpace(username) ? discordId : username });
        }

        return Results.Ok(new
        {
            userId = user.Id,
            username = user.Username,
            email = user.Email,
            discordId = user.DiscordId,
            auth0UserId = user.Auth0UserId,
            avatarUrl = principal.FindFirst(ClaimNamespace + "picture")?.Value ?? user.AvatarUrl
        });
    }

    private static async Task<IResult?> CheckOwnerAsync(ClaimsPrincipal principal, string discordId)
{
    var caller = await CurrentUserService.GetAsync(principal);
    if (caller is null) return Results.Unauthorized();
    if (caller.DiscordId != discordId)
        return Results.Json(new { message = "You can only change your own account" }, statusCode: 403);
    return null;
}

    private static async Task<IResult> GetDiscordAccountStatus(string discordId, ClaimsPrincipal principal)
    {
            var denied = await CheckOwnerAsync(principal, discordId);
    if (denied != null) return denied;
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        Console.WriteLine($"[STATUS] Checking status for Discord ID: {discordId}");

        await using var connection = await AppConfig.OpenConnectionAsync();
        var user = await connection.QueryOneOrDefaultAsync<UserEntity>(
            UserSql.GetByDiscordId(), new { DiscordId = discordId });
        if (user == null)
        {
            Console.WriteLine($"[STATUS] User not found for Discord ID: {discordId}");
            return Results.NotFound(new { message = "User not found" });
        }

        var isLinked = !string.IsNullOrWhiteSpace(user.DiscordId);
        Console.WriteLine($"[STATUS] User found: {user.Id} ({user.Username}) - discord_id in DB: '{user.DiscordId}' - isLinked: {isLinked}");

        return Results.Ok(new { 
            userId = user.Id, 
            isLinked = isLinked,
            discordId = user.DiscordId,
            username = user.Username
        });
    }
}
public record UpdateUserTagsRequest(TagSelection[] Selections);
