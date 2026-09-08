using Dapper;
using Npgsql;
using Persistence;

namespace Api.Endpoints.Handlers;

public sealed class DbSession : IAsyncDisposable
{
    // Private per review feedback: all database access must go through this
    // class's own wrapper methods, never by reaching into .Conn/.Tx directly
    // from outside — that's what guarantees the transaction is always used
    // correctly, with no way to accidentally bypass it.
    private readonly NpgsqlConnection _conn;
    private readonly NpgsqlTransaction _tx;

    private DbSession(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        _conn = conn;
        _tx = tx;
    }

    public static async Task<DbSession> OpenAsync()
    {
        var conn = await AppConfig.OpenConnectionAsync();
        var tx = await conn.BeginTransactionAsync();
        return new DbSession(conn, tx);
    }

    public Task<T> QueryOneAsync<T>(string sql, object param) =>
        _conn.QueryOneAsync<T>(sql, param, _tx);

    public Task<T> QuerySingleAsync<T>(string sql, object? param = null) =>
        _conn.QuerySingleAsync<T>(sql, param, _tx);

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null) =>
        _conn.QueryAsync<T>(sql, param, _tx);

    public Task<IEnumerable<T>> QueryManyAsync<T>(string sql, object? param = null) =>
        _conn.QueryManyAsync<T>(sql, param, _tx);

    public Task<T?> QueryOneOrDefaultAsync<T>(string sql, object? param = null) =>
        _conn.QueryOneOrDefaultAsync<T>(sql, param, _tx);

    public Task<List<T>> QueryListAsync<T>(string sql, object? param = null) =>
        _conn.QueryListAsync<T>(sql, param, _tx);

    public Task<int> ExecuteAsync(string sql, object? param = null) =>
        _conn.ExecuteAsync(sql, param, _tx);

    public Task<Dapper.SqlMapper.GridReader> QueryMultipleAsync(string sql, object? param = null) =>
    _conn.QueryMultipleAsync(sql, param, _tx);
    /// <summary>
    /// Plain rollback with no response — use inside a catch block when you're
    /// about to return your own IResult separately (e.g. Results.BadRequest
    /// built from an exception message).
    /// </summary>
    public Task RollbackAsync() => _tx.RollbackAsync();

    /// <summary>
    /// Rollback + a ready-made 400 response in one call, for the common case
    /// of a validation failure with a simple message.
    /// </summary>
    public async Task<IResult> RollbackAsync(string? message)
    {
        await _tx.RollbackAsync();
        return Results.BadRequest(new { message });
    }

    public Task CommitAsync() => _tx.CommitAsync();

    public async ValueTask DisposeAsync()
    {
        await _tx.DisposeAsync();
        await _conn.DisposeAsync();
    }
}