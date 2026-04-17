using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class TagEndpoints
{
    // Temporary in-memory store for user tags
    private static readonly Dictionary<Guid, List<string>> UserTags = new();

    public static void MapTagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tags").WithName("Tags");

        group.MapGet("/tree", GetTagTree).WithName("GetTagTree");
        group.MapGet("/users/{userId}/tags", GetUserTags).WithName("GetUserTags");
        group.MapPost("/users/{userId}/tags", AddUserTag).WithName("AddUserTag");
    }

    // ---------------------------------------------------------
    // 1. TAG TREE (root → children → grandchildren)
    // ---------------------------------------------------------
    private static IResult GetTagTree()
    {
        var tags = new[]
        {
            // ROOT TAG
            new { tagId = "student", navn = "Student", parentTagId = (string?)null },

            // CHILDREN OF STUDENT
            new { tagId = "student-beginner", navn = "Beginner", parentTagId = "student" },
            new { tagId = "student-intermediate", navn = "Intermediate", parentTagId = "student" },
            new { tagId = "student-advanced", navn = "Advanced", parentTagId = "student" },

            // GRANDCHILDREN (example)
            new { tagId = "js-beginner", navn = "JavaScript Beginner", parentTagId = "student-beginner" },
            new { tagId = "python-beginner", navn = "Python Beginner", parentTagId = "student-beginner" }
        };

        return Results.Ok(tags);
    }

    // ---------------------------------------------------------
    // 2. GET USER TAGS
    // ---------------------------------------------------------
    private static IResult GetUserTags(Guid userId)
    {
        if (!UserTags.ContainsKey(userId))
            return Results.Ok(new { tags = Array.Empty<string>() });

        return Results.Ok(new { tags = UserTags[userId] });
    }

    // ---------------------------------------------------------
    // 3. ADD USER TAG
    // ---------------------------------------------------------
    private static async Task<IResult> AddUserTag(Guid userId, HttpContext context)
    {
        var body = await context.Request.ReadFromJsonAsync<TagRequest>();
        if (body == null || body.TagIds.Count == 0)
        {
            return Results.BadRequest(new { message = "No tag provided" });
        }

        if (!UserTags.ContainsKey(userId))
            UserTags[userId] = new List<string>();

        foreach (var tag in body.TagIds)
        {
            if (!UserTags[userId].Contains(tag))
                UserTags[userId].Add(tag);
        }

        return Results.Ok(new { message = "Tag added" });
    }
}

public class TagRequest
{
    public List<string> TagIds { get; set; } = new();
}
