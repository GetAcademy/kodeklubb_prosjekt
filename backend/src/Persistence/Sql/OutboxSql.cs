namespace Persistence;

public static class OutboxSql
{
    public static string ClaimPending() => SqlLoader.Load("Queries/Outbox_ClaimPending.sql");

    public static string MarkProcessed() => SqlLoader.Load("Commands/Outbox_MarkProcessed.sql");

    public static string GetAdminContactByTeamId() => SqlLoader.Load("Queries/Users_GetAdminContactByTeamId.sql");

    public static string GetUserContactById() => SqlLoader.Load("Queries/Users_GetContactById.sql");
}
