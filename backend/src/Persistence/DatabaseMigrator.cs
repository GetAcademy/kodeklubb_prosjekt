using Dapper;
using Npgsql;
using System.Reflection;

namespace Persistence;

/// <summary>
/// Simple migration runner for database schema updates
/// Runs SQL migrations from embedded resources in order
/// </summary>
public class DatabaseMigrator
{
    private readonly string _connectionString;

    public DatabaseMigrator(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Run all pending migrations
    /// </summary>
    public async Task MigrateAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get all migration files from embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        var migrationResources = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Sql.Migrations.") && name.EndsWith(".sql"))
            .OrderBy(name => name)
            .ToList();

        foreach (var resourceName in migrationResources)
        {
            Console.WriteLine($"Running migration: {resourceName}");
            
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Could not load migration resource: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync();

            await connection.ExecuteAsync(sql);
            
            Console.WriteLine($"✓ Migration completed: {resourceName}");
        }
    }

    /// <summary>
    /// Check database connection
    /// </summary>
    public async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
