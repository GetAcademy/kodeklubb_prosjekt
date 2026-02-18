namespace Endpoints
{
    using Api;
    using Api.Endpoints;
    using Persistence;
    using Persistence.DbModels;

    public static partial class Users
    {
        public static async Task<IResult> GetUserTeams(string? discordId)
        {
            if (string.IsNullOrWhiteSpace(discordId))
            {
                return Results.BadRequest(new { message = "Discord ID is required" });
            }

            await using var connection = await AppConfig.OpenConnectionAsync();

            var teams = await connection.QueryManyAsync<TeamEntity>(TeamSql.GetUserTeams, new { DiscordId = discordId });

            var results = teams.Select(team => new TeamListItem(
                team.Id,
                team.Name,
                team.Description,
                team.IsOpenToJoinRequests,
                team.CreatedBy,
                team.CreatedAt,
                new string[0]
            ));

            return Results.Ok(results);
        }
    }
}