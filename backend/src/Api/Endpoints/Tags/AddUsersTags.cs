namespace Api.Endpoints.Tags;

using Dapper;
using Persistence;
using Persistence.DbModels;
using System.Runtime.CompilerServices;

public static class AddUserTagsEndpoint
{
     public static async Task<IResult> AddUserTags(string discordId, UpdateUserTagsRequest request)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return Results.BadRequest(new { message = "Discord ID is required" });
        if (request.TagIds == null || request.TagIds.Length == 0) return Results.BadRequest(new { message = "At least one tag ID is required" });

        await using var connection = await AppConfig.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var user = await connection.QuerySingleOrDefaultAsync<UserEntity>(UserSql.GetByDiscordId(), new { DiscordId = discordId }, transaction);
            if (user == null) { await transaction.RollbackAsync(); return Results.NotFound(new { message = "User not found" }); }
            foreach (var tagId in request.TagIds)
                await connection.ExecuteAsync(UserSql.InsertUserPredefinedTag(), new { UserId = user.Id, PredefinedTagId = tagId }, transaction);
            await transaction.CommitAsync();
            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception) { await transaction.RollbackAsync(); throw; }
    }
}