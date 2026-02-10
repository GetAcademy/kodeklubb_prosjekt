using System.Reflection;

namespace Persistence;

public static class SqlLoader
{
    private static readonly Assembly Assembly = typeof(SqlLoader).Assembly;

    public static string Load(string sqlFileName)
    {
        // Convert file path to resource name
        // Example: "Commands/Teams_Create.sql" -> "Persistence.Sql.Commands.Teams_Create.sql"
        var resourceName = $"Persistence.Sql.{sqlFileName.Replace("/", ".").Replace("\\", ".")}";
        
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded SQL resource not found: {resourceName}");
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
