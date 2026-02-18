  namespace Endpoints
  {
    using Api;
    using Api.Endpoints;
    using Persistence;
    using Persistence.DbModels;

    public static partial class Users
    {
      public static async Task<IResult> CreateUser(CreateUserRequest request)
      {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var createdUser = await connection.QueryOneAsync<UserEntity>(UserSql.Insert, new { DiscordId = request.DiscordId ?? string.Empty, Username = request.Username ?? string.Empty, Email = request.Email, AvatarUrl = request.AvatarUrl, PreferencesJson = request.PreferencesJson });
        return Results.Created($"/api/users/{createdUser.Id}", createdUser);
      }
    }
  }