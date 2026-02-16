using System.Data;
using Dapper;

namespace Persistence;

public static class DapperExtensions
{
    public static async Task<List<T>> QueryListAsync<T>(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        var rows = await connection.QueryAsync<T>(sql, parameters, transaction);
        return rows.AsList();
    }

    public static async Task<List<T>> QueryListAsync<T>(
        this IDbConnection connection,
        Func<string> sqlProvider,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        var rows = await connection.QueryAsync<T>(sqlProvider(), parameters, transaction);
        return rows.AsList();
    }

    public static Task<IEnumerable<T>> QueryManyAsync<T>(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QueryAsync<T>(sql, parameters, transaction);
    }

    public static Task<IEnumerable<T>> QueryManyAsync<T>(
        this IDbConnection connection,
        Func<string> sqlProvider,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QueryAsync<T>(sqlProvider(), parameters, transaction);
    }

    public static Task<IEnumerable<dynamic>> QueryManyAsync(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QueryAsync(sql, parameters, transaction);
    }

    public static Task<IEnumerable<dynamic>> QueryManyAsync(
        this IDbConnection connection,
        Func<string> sqlProvider,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QueryAsync(sqlProvider(), parameters, transaction);
    }

    public static Task<T?> QueryOneOrDefaultAsync<T>(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction);
    }

    public static Task<T?> QueryOneOrDefaultAsync<T>(
        this IDbConnection connection,
        Func<string> sqlProvider,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QuerySingleOrDefaultAsync<T>(sqlProvider(), parameters, transaction);
    }

    public static Task<T> QueryOneAsync<T>(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QuerySingleAsync<T>(sql, parameters, transaction);
    }

    public static Task<T> QueryOneAsync<T>(
        this IDbConnection connection,
        Func<string> sqlProvider,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.QuerySingleAsync<T>(sqlProvider(), parameters, transaction);
    }

    public static Task<int> ExecuteCommandAsync(
        this IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.ExecuteAsync(sql, parameters, transaction);
    }

    public static Task<int> ExecuteCommandAsync(
        this IDbConnection connection,
        Func<string> sqlProvider,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        return connection.ExecuteAsync(sqlProvider(), parameters, transaction);
    }
}