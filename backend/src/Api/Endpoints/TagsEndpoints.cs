using Persistence;

namespace Api.Endpoints;

public static class TagsEndpoints
{
    public static void MapTagsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tags", GetAllTags).WithName("GetAllTags");
    }

    private static async Task<IResult> GetAllTags()
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var tags = await connection.QueryManyAsync<TagDto>(TagsSql.GetAll(), new { });
        return Results.Ok(tags);
    }
}

public record TagDto(Guid Id, string Name, Guid? ParentId, bool OpenForChildSuggestions);