using Core.Models;
using Persistence.Repositories;

namespace Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithName("Users").WithOpenApi();

        group.MapGet("/", GetAllUsers).WithName("GetAllUsers").WithOpenApi();
        group.MapGet("/{id}", GetUserById).WithName("GetUserById").WithOpenApi();
        group.MapPost("/", CreateUser).WithName("CreateUser").WithOpenApi();
    }

    private static async Task<IResult> GetAllUsers(IRepository<User> userRepository)
    {
        var users = await userRepository.GetAllAsync();
        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserById(int id, IRepository<User> userRepository)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return Results.NotFound();

        return Results.Ok(user);
    }

    private static async Task<IResult> CreateUser(CreateUserRequest request, IRepository<User> userRepository)
    {
        var user = new User
        {
            DiscordId = request.DiscordId,
            Email = request.Email,
            Username = request.Username,
            AvatarUrl = request.AvatarUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        return Results.Created($"/api/users/{user.Id}", user);
    }
}

public record CreateUserRequest(string? DiscordId, string? Email, string? Username, string? AvatarUrl);
