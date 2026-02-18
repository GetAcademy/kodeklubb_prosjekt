namespace Endpoints
{
    using Api;
    using Persistence;
    using Persistence.DbModels;

    public static partial class Users
    {
        public static async Task<IResult> GetAllUsers()
        {
            await using var connection = await AppConfig.OpenConnectionAsync();
            return Results.Ok(await connection.QueryManyAsync<UserEntity>(UserSql.GetAll));
        }
    }
}