namespace Persistence;

public static class TeamSql
{
    public static string CheckUserExists() => SqlLoader.Load("Queries/Users_CheckExists.sql");

    public static string GetUserByDiscordId() => SqlLoader.Load("Queries/Users_GetByDiscordId.sql");

    public static string CreateTeam() => SqlLoader.Load("Commands/Teams_Create.sql");

    public static string GetById() => SqlLoader.Load("Queries/Teams_GetById.sql");

    public static string GetAvailable() => SqlLoader.Load("Queries/Teams_GetAvailable.sql");

    public static string GetUserTeams() => SqlLoader.Load("Queries/Teams_GetUserTeams.sql");

    public static string GetAdminUserByTeamId() => SqlLoader.Load("Queries/Teams_GetAdminUserByTeamId.sql");

    public static string GetMemberIdsByTeamId() => SqlLoader.Load("Queries/TeamMembers_GetByTeamId.sql");

    public static string GetMemberListByTeamId() => SqlLoader.Load("Queries/TeamMembers_GetListByTeamId.sql");

    public static string IsUserMemberByDiscordId() => SqlLoader.Load("Queries/TeamMembers_IsUserMemberByDiscordId.sql");

    public static string InsertTeamMember() => SqlLoader.Load("Commands/TeamMembers_Insert.sql");

    public static string InsertEventLog() => SqlLoader.Load("Outbox/EventLog_Insert.sql");

    public static string InsertOutbox() => SqlLoader.Load("Outbox/Outbox_Insert.sql");
}