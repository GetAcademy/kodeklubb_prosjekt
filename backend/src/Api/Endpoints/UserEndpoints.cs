using Api;
using Persistence;
using Persistence.DbModels;
using Npgsql;
using Dapper;


namespace Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithName("Users");

        group.MapGet("/", Users.Users.GetAllUsers).WithName("GetAllUsers");
        group.MapGet("/{id}", Users.Users.GetUserById).WithName("GetUserById");
        group.MapPost("/", (CreateUserRequest request) => Users.Users.CreateUser(request)).WithName("CreateUser");
        group.MapGet("/tags/predefined", Tags.GetPredefinedTagsEndpoint.MapGetPredefinedTags).WithName("GetPredefinedTags");
        group.MapGet("/{discordId}/tags", Tags.GetUserTagsEndpoint.MapGetUserTags).WithName("GetUserTags");
        group.MapPost("/{discordId}/tags", (string discordId, UpdateUserTagsRequest request) => Tags.AddUserTagsEndpoint.AddUserTags(discordId, request)).WithName("AddUserTags");
    }

}

public record CreateUserRequest(string? DiscordId, string? Email, string? Username, string? AvatarUrl, string? PreferencesJson);
public record UpdateUserTagsRequest(Guid[] TagIds);