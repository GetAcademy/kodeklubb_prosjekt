using Persistence;

namespace Api.Endpoints.Tags;

public static class GetPredefinedTagsEndpoint
{
    public static void MapGetPredefinedTags(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/tags/predefined", GetPredefinedTags);
    }

    private static async Task<IResult> GetPredefinedTags()
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        return Results.Ok(await connection.QueryManyAsync(UserSql.GetPredefinedTags));
    }
}