namespace Persistence;

public static class TagsSql
{
    public static string GetAll() => SqlLoader.Load("Queries/PredefinedTags_GetAll.sql");

    public static string CheckExists() => SqlLoader.Load("Queries/PredefinedTags_CheckExists.sql");
}
