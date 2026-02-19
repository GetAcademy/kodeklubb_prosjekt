
using Persistence;
using Persistence.DbModels;
namespace Api.Endpoints.Teams;
  public static class GetTeamDetailsEndpoint
    {
        public static async Task<IResult> GetTeamDetails(Guid teamId, string? discordId)
        {
            await using var connection = await AppConfig.OpenConnectionAsync();

            var team = await connection.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById, new { TeamId = teamId });

            if (team == null)
            {
                return Results.NotFound(new { message = "Team not found" });
            }

            if (string.IsNullOrWhiteSpace(discordId))
            {
                return Results.Ok(team);
            }

            var isMember = await connection.QueryOneAsync<bool>(
                TeamSql.IsUserMemberByDiscordId,
                new { TeamId = teamId, DiscordId = discordId });

            return Results.Ok(new { team, isMember });
        }
    }
