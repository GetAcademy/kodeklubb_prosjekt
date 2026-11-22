using System.Security.Claims;
using Dapper;
using Persistence;
using Persistence.DbModels;

namespace Api;

// One place that turns the validated token into the Kodeklubb user.
// Handlers call this instead of reading discordId from the request.
public static class CurrentUserService
{
    // Returns null if the token has no "sub" or the user is not in the database yet.
    public static async Task<UserEntity?> GetAsync(ClaimsPrincipal principal)
    {
        var auth0UserId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return null;

        await using var connection = await AppConfig.OpenConnectionAsync();
        return await connection.QueryOneOrDefaultAsync<UserEntity>(
            UserSql.GetByAuth0UserId(), new { Auth0UserId = auth0UserId });
    }
}