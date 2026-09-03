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

              // --- User CRUD ---
        group.MapGet("/", GetAllUsers).WithName("GetAllUsers");
        group.MapGet("/{id}", GetUserById).WithName("GetUserById");
        group.MapPost("/", CreateUser).WithName("CreateUser");
        group.MapGet("/me", GetCurrentUser).WithName("GetCurrentUser");

        // --- Tags ---
        group.MapGet("/{discordId}/tags", GetUserTags).WithName("GetUserTags");
        group.MapPost("/{discordId}/tags", AddUserTags).WithName("AddUserTags");
        group.MapDelete("/{discordId}/tags/{tagId:guid}", RemoveUserTag).WithName("RemoveUserTag");

        // --- Discord account linking ---
        group.MapPost("/{discordId}/discord/link", LinkDiscordAccount).WithName("LinkDiscordAccount");
        group.MapDelete("/{discordId}/discord/unlink", UnlinkDiscordAccount).WithName("UnlinkDiscordAccount");
        group.MapGet("/{discordId}/discord/status", GetDiscordAccountStatus).WithName("GetDiscordAccountStatus");
        // --- Misc / dev utilities ---
        group.MapPost("/send-test-email", async (Core.Logic.IEmailService emailService, string toEmail) =>
        {
            await emailService.SendEmailAsync(toEmail, "Test Email from Kodeklubb", "<h1>This is a test email sent via Resend!</h1>");
            return Results.Ok(new { message = $"Test email sent to {toEmail}" });
        }).WithName("SendTestEmail");
        
    }


    // ========== User CRUD ==========

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


    // ========== Tags ==========
    // SQL lives in Persistence (UserSql, TagsSql) rather than inline here.

    private static async Task<IResult> GetUserTags(string discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });
        await using var db = await Handlers.DbSession.OpenAsync();
        return Results.Ok(await db.QueryAsync<dynamic>(UserSql.GetUserPredefinedTagsByDiscordId(), new { DiscordId = discordId }));
    }


    private static async Task<IResult> AddUserTags(string discordId, UpdateUserTagsRequest request)
    {
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

    private static async Task<IResult> RemoveUserTag(string discordId, Guid tagId)
    {
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

    private static async Task<IResult> LinkDiscordAccount(string discordId)
    {
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

    private static async Task<IResult> UnlinkDiscordAccount(string discordId)
    {
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

        private static async Task<IResult> GetCurrentUser(ClaimsPrincipal principal, [FromHeader(Name = "X-Discord-ID")] string? discordIdHeader)
{
    var discordId = principal.FindFirst("sub")?.Value ?? discordIdHeader;

        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID not found in request" });

        await using var connection = await AppConfig.OpenConnectionAsync();

        var user = await connection.QueryOneOrDefaultAsync<UserEntity>(
            UserSql.GetByDiscordIdOrUsername(),
            new { DiscordId = discordId });

        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        return Results.Ok(new {
            userId = user.Id,
            username = user.Username,
            email = user.Email,
            discordId = user.DiscordId,
            isLinked = !string.IsNullOrWhiteSpace(user.DiscordId)
        });
    }

    private static async Task<IResult> GetDiscordAccountStatus(string discordId)
    {
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

public record CreateUserRequest(string? DiscordId, string? Email, string? Username, string? AvatarUrl, string? PreferencesJson);
public record UpdateUserTagsRequest(TagSelection[] Selections);
