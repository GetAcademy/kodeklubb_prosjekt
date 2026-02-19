using Api;
using Persistence;
using Persistence.DbModels;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints.Tags
{
    public static class GetUserTagsEndpoint
    {
        public static async Task<IResult> MapGetUserTags(string discordId)
        {
            if (string.IsNullOrWhiteSpace(discordId))
            {
                return Results.BadRequest(new { message = "Discord ID is required" });
            }

            await using var connection = await AppConfig.OpenConnectionAsync();

            // Example SQL: Get all tags for a user by DiscordId
            var tags = await connection.QueryManyAsync<string>(
                "SELECT t.Name FROM UserPredefinedTags upt JOIN PredefinedTags t ON upt.PredefinedTagId = t.Id JOIN Users u ON upt.UserId = u.Id WHERE u.DiscordId = @DiscordId",
                new { DiscordId = discordId });

            return Results.Ok(tags);
        }
    }
}
