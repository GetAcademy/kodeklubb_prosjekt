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

    private static async Task<IResult> GetPredefinedTags()
    {
        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT id, name, description, category
FROM predefined_tags
ORDER BY category, name;
";
        var tags = await connection.QueryAsync<dynamic>(sql);

        return Results.Ok(tags);
    }

    private static async Task<IResult> GetUserTags(string discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return Results.BadRequest(new { message = "Discord ID is required" });
        }

        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT pt.id, pt.name, pt.description, pt.category
FROM user_tags ut
INNER JOIN users u ON u.id = ut.user_id
INNER JOIN predefined_tags pt ON pt.id = ut.predefined_tag_id
WHERE u.discord_id = @DiscordId
ORDER BY pt.name;
";
        var tags = await connection.QueryAsync<dynamic>(sql, new { DiscordId = discordId });

        return Results.Ok(tags);
    }

    private static async Task<IResult> AddUserTags(string discordId, UpdateUserTagsRequest request)
    {
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return Results.BadRequest(new { message = "Discord ID is required" });
        }

        if (request.TagIds == null || request.TagIds.Length == 0)
        {
            return Results.BadRequest(new { message = "At least one tag ID is required" });
        }

        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var getUserSql = SqlLoader.Load("Queries/Users_GetByDiscordId.sql");
            var user = await connection.QuerySingleOrDefaultAsync<UserEntity>(
                getUserSql,
                new { DiscordId = discordId },
                transaction);

            if (user == null)
            {
                await transaction.RollbackAsync();
                return Results.NotFound(new { message = "User not found" });
            }

            const string insertUserTagSql = @"
INSERT INTO user_tags (user_id, predefined_tag_id, created_at)
VALUES (@UserId, @PredefinedTagId, NOW())
ON CONFLICT (user_id, predefined_tag_id) DO NOTHING;
";

            foreach (var tagId in request.TagIds)
            {
                await connection.ExecuteAsync(
                    insertUserTagSql,
                    new { UserId = user.Id, PredefinedTagId = tagId },
                    transaction);
            }

            await transaction.CommitAsync();

            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

public record CreateUserRequest(string? DiscordId, string? Email, string? Username, string? AvatarUrl, string? PreferencesJson);
public record UpdateUserTagsRequest(Guid[] TagIds);

