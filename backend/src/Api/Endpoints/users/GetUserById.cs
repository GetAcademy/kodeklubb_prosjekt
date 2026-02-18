namespace Endpoints
{
    using Api;
    using Persistence;
    using Persistence.DbModels;

    public static partial class Users
    {
        public static async Task<IResult> GetUserById(Guid id)
        {
            await using var connection = await AppConfig.OpenConnectionAsync();
            var user = await connection.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetById, new { Id = id });
            return user is null ? Results.NotFound() : Results.Ok(user);
        }
    }
}