namespace Api.Endpoints.Teams
{
    public static class GetTeamContentEndpoint
    {
        public static Task<IResult> GetTeamContent(Guid teamId)
        {
            // TODO: Refactor to use Dapper + Core pattern
            return Task.FromResult(Results.StatusCode(501)); // return new Result
        }
    }
}