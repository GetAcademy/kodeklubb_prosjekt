using Dapper;
using Npgsql;
using Persistence;

namespace Api.Endpoints.Handlers;

public sealed class DbSession : IAsyncDisposable
{
    public NpgsqlConnection Conn { get; }
    public NpgsqlTransaction Tx { get; }

    private DbSession(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        Conn = conn;
        Tx = tx;
    }

    public static async Task<DbSession> OpenAsync()
    {
        var conn = await AppConfig.OpenConnectionAsync();
        var tx = await conn.BeginTransactionAsync();
        return new DbSession(conn, tx);
    }

    public Task<T> QueryOneAsync<T>(string sql, object param) =>
        Conn.QueryOneAsync<T>(sql, param, Tx);

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null) =>
        Conn.QueryAsync<T>(sql, param, Tx);

    public Task<T?> QueryOneOrDefaultAsync<T>(string sql, object? param = null) =>
        Conn.QueryOneOrDefaultAsync<T>(sql, param, Tx);

    public Task<List<T>> QueryListAsync<T>(string sql, object? param = null) =>
        Conn.QueryListAsync<T>(sql, param, Tx);

    public Task<int> ExecuteAsync(string sql, object? param = null) =>
        Conn.ExecuteAsync(sql, param, Tx);

    public async Task<IResult> RollbackAsync(string? message)
    {
        await Tx.RollbackAsync();
        return Results.BadRequest(new { message });
    }

    public Task CommitAsync() => Tx.CommitAsync();

    public async ValueTask DisposeAsync()
    {
        await Tx.DisposeAsync();
        await Conn.DisposeAsync();
    }
}