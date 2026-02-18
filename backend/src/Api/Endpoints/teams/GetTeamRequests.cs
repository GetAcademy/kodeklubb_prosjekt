
using Persistence;
namespace Api.Endpoints.Teams
{
  

    public static class GetTeamRequestsEndpoint
    {
        public static async Task<IResult> GetTeamRequests(Guid teamId)
        {
            await using var connection = await AppConfig.OpenConnectionAsync();

            var requests = await connection.QueryManyAsync(InvitationSql.GetPendingByTeam, new { TeamId = teamId });

            return Results.Ok(requests);
        }
    }
}