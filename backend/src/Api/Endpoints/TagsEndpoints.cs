using System.Text.Json;
using Persistence;

namespace Api.Endpoints;

public static class TagsEndpoints
{
    // Scoped to just this endpoint's response so the app-wide JSON config
    // (PropertyNamingPolicy = null, i.e. PascalCase) used by every other
    // endpoint stays untouched.
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void MapTagsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tags", GetAllTags).WithName("GetAllTags");
    }

    private static async Task<IResult> GetAllTags()
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var tags = await connection.QueryManyAsync<TagDto>(TagsSql.GetAll(), new { });
        return Results.Json(tags, CamelCaseOptions);
    }
}

public record TagDto(Guid Id, string Name, Guid? ParentId, bool OpenForChildSuggestions);