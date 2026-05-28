using Api;

namespace Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tags").WithName("Tags").WithOpenApi();
        
        group.MapGet("/hierarchy", GetTagHierarchy).WithName("GetTagHierarchy");
    }

    private static async Task<IResult> GetTagHierarchy()
    {
        try
        {
            // Try multiple possible paths
            var possiblePaths = new[]
            {
                Path.Combine("src", "Persistence", "tag_hierarchy.json"),
                Path.Combine("Persistence", "tag_hierarchy.json"),
                "tag_hierarchy.json",
                Path.Combine(AppContext.BaseDirectory, "tag_hierarchy.json")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    var json = await File.ReadAllTextAsync(path);
                    return Results.Content(json, "application/json");
                }
            }

            return Results.NotFound(new { message = "Tag hierarchy file not found" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading tag hierarchy: {ex.Message}", statusCode: 500);
        }
    }
}