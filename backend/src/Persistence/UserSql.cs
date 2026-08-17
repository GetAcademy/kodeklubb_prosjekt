namespace Persistence;
public static class UserSql
{
    public static string GetAll() => SqlLoader.Load("Queries/Users_GetAll.sql");
    public static string GetById() => SqlLoader.Load("Queries/Users_GetById.sql");
    public static string GetByDiscordId() => SqlLoader.Load("Queries/Users_GetByDiscordId.sql");
    public static string Insert() => SqlLoader.Load("Commands/Users_Insert.sql");
    public static string GetUserPredefinedTagsByDiscordId() => SqlLoader.Load("Queries/UserTags_GetPredefinedByDiscordId.sql");
    public static string InsertUserPredefinedTag() => SqlLoader.Load("Commands/UserTags_InsertPredefined.sql");

    // Used by GetCurrentUser (falls back to matching by username if a
    // Discord ID isn't found — e.g. for accounts not yet linked).
    public static string GetByDiscordIdOrUsername() => SqlLoader.Load("Queries/Users_GetByDiscordIdOrUsername.sql");

    public static string DeleteAllUserTagsForUser() => SqlLoader.Load("Commands/UserTags_DeleteAllForUser.sql");
    public static string DeleteUser() => SqlLoader.Load("Commands/Users_Delete.sql");
}
