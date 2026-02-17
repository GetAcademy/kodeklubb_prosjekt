namespace Persistence;

public static class UserSql
{
    public static string GetAll() => SqlLoader.Load("Queries/Users_GetAll.sql");

    public static string GetById() => SqlLoader.Load("Queries/Users_GetById.sql");

    public static string GetByDiscordId() => SqlLoader.Load("Queries/Users_GetByDiscordId.sql");

    public static string Insert() => SqlLoader.Load("Commands/Users_Insert.sql");

    public static string GetPredefinedTags() => SqlLoader.Load("Queries/PredefinedTags_GetAll.sql");

    public static string GetUserPredefinedTagsByDiscordId() => SqlLoader.Load("Queries/UserTags_GetPredefinedByDiscordId.sql");

    public static string InsertUserPredefinedTag() => SqlLoader.Load("Commands/UserTags_InsertPredefined.sql");
}
