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
        
        // Discord account linking endpoints
        group.MapPost("/{discordId}/discord/link", (string discordId, HttpContext context) => LinkDiscordAccount(discordId, context)).WithName("LinkDiscordAccount");
        group.MapDelete("/{discordId}/discord/unlink", (string discordId, IServiceProvider sp) => UnlinkDiscordAccount(discordId, sp)).WithName("UnlinkDiscordAccount");
        group.MapGet("/{discordId}/discord/status", (string discordId) => GetDiscordAccountStatus(discordId)).WithName("GetDiscordAccountStatus");
        group.MapGet("/me", (HttpContext context) => GetCurrentUser(context)).WithName("GetCurrentUser");

        group.MapPost("/send-test-email", async (IServiceProvider sp, string toEmail) =>
        {
            var emailService = sp.GetRequiredService<Core.Logic.IEmailService>();
            await emailService.SendEmailAsync(toEmail, "Test Email from Kodeklubb", "<h1>This is a test email sent via Resend!</h1>");
            return Results.Ok(new { message = $"Test email sent to {toEmail}" });
        }).WithName("SendTestEmail");
        
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
        if ((request.TagIds == null || request.TagIds.Length == 0) && (request.TagPaths == null || request.TagPaths.Length == 0))
            return Results.BadRequest(new { message = "At least one tag ID or tag path is required" });

        await using var db = await Handlers.DbSession.OpenAsync();
        try
        {
            var user = await db.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetByDiscordId(), new { DiscordId = discordId });
            if (user == null) { await db.Tx.RollbackAsync(); return Results.NotFound(new { message = "User not found" }); }
            if (request.TagIds != null)
            {
                foreach (var tagId in request.TagIds)
                    await db.ExecuteAsync(UserSql.InsertUserPredefinedTag(), new { UserId = user.Id, PredefinedTagId = tagId });
            }
            if (request.TagPaths != null)
            {
                foreach (var tagPath in request.TagPaths)
                {
                    await db.ExecuteAsync("INSERT INTO usertags_hierarchical (userid, tagpath) VALUES (@UserId, @TagPath)", new { UserId = user.Id, TagPath = tagPath });
                }
            }
            await db.CommitAsync();
            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception) { await db.Tx.RollbackAsync(); throw; }
    }

    private static async Task<IResult> LinkDiscordAccount(string discordId, HttpContext context)
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

            // User is already linked if they have a Discord ID
            return Results.Ok(new { message = "Discord account is linked", userId = user.Id, discordId = user.DiscordId });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UnlinkDiscordAccount(string discordId, IServiceProvider sp)
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
            await db.Tx.RollbackAsync();
            return Results.NotFound(new { message = "User not found" });
        }

        // Delete user record - they must log in again to create new account
        await db.ExecuteAsync("DELETE FROM usertags_hierarchical WHERE userid = @UserId", new { UserId = user.Id });
        await db.ExecuteAsync("DELETE FROM user_tags WHERE user_id = @UserId", new { UserId = user.Id });
        await db.ExecuteAsync("DELETE FROM users WHERE id = @UserId", new { UserId = user.Id });
        
        await db.CommitAsync();

        return Results.Ok(new { message = "Account unlinked. Please log in again." });
    }
    catch (Exception ex)
    {
        await db.Tx.RollbackAsync();
        return Results.BadRequest(new { message = $"Error: {ex.Message}" });
    }
}
    private static async Task<IResult> GetCurrentUser(HttpContext context)
{
    // Get the Discord ID from claims or headers
    var discordId = context.User.FindFirst("sub")?.Value 
                   ?? context.Request.Headers["X-Discord-ID"].FirstOrDefault();
    
    if (string.IsNullOrWhiteSpace(discordId))
        return Results.BadRequest(new { message = "Discord ID not found in request" });

    await using var connection = await AppConfig.OpenConnectionAsync();
    
    // Search by username or email instead of discord_id
    var user = await connection.QueryOneOrDefaultAsync<UserEntity>(
        "SELECT * FROM users WHERE discord_id = @DiscordId OR username LIKE @DiscordId",
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
public record UpdateUserTagsRequest(Guid[] TagIds, string[]? TagPaths);