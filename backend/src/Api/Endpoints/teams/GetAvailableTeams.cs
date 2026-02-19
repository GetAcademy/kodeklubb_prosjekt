    using Persistence;
    using Persistence.DbModels;
using static Api.Endpoints.TeamEndpoints;
namespace Api.Endpoints.Teams
    {


        public static class GetAvailableTeamsEndpoint
        {
            public static async Task<IResult> GetAvailableTeams(string? discordId)
            {
                await using var connection = await AppConfig.OpenConnectionAsync();

                var teams = await connection.QueryManyAsync<TeamEntity>(TeamSql.GetAvailable, new { DiscordId = discordId });

                var results = teams.Select(team => new TeamListItem
                (
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