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

    public static string GetTeamAnnouncementsByTeamId() => SqlLoader.Load("Queries/TeamAnnouncements_GetByTeamId.sql");

    public static string GetTeamAnnouncementById() => SqlLoader.Load("Queries/TeamAnnouncements_GetById.sql");

    public static string InsertTeamAnnouncement() => SqlLoader.Load("Commands/TeamAnnouncements_Insert.sql");

    public static string UpdateTeamAnnouncement() => SqlLoader.Load("Commands/TeamAnnouncements_Update.sql");

    public static string DeleteTeamAnnouncement() => SqlLoader.Load("Commands/TeamAnnouncements_Delete.sql");

    public static string InsertEventLog() => SqlLoader.Load("Outbox/EventLog_Insert.sql");

    public static string InsertOutbox() => SqlLoader.Load("Outbox/Outbox_Insert.sql");

    public static string GetAllTeamTagsGrouped() => SqlLoader.Load("Queries/TeamTags_GetAllGrouped.sql");

    // --- Tags ---
    public static string GetTeamTagsByTeamId() => SqlLoader.Load("Queries/TeamTags_GetByTeamId.sql");

    public static string CheckAndInsertTeamTag() => SqlLoader.Load("Commands/TeamTags_CheckAndInsert.sql");

    public static string DeleteTeamTag() => SqlLoader.Load("Commands/TeamTags_Delete.sql");

    // Note: predefined-tag existence check for user_tags lives in
    // TagsSql.CheckExists(); for team_tags it's now combined into
    // CheckAndInsertTeamTag() above (one round trip instead of two).

    // --- Discord integration ---
    public static string SetTeamDiscordConfig() => SqlLoader.Load("Commands/Teams_SetDiscordConfig.sql");

    public static string GetTeamDiscordInfo() => SqlLoader.Load("Queries/Teams_GetDiscordInfo.sql");

    public static string ClearTeamDiscordConfig() => SqlLoader.Load("Commands/Teams_ClearDiscordConfig.sql");

    public static string InsertDiscordRoleAssignment() => SqlLoader.Load("Commands/DiscordRoleAssignments_Insert.sql");

    public static string RemoveDiscordRoleAssignment() => SqlLoader.Load("Commands/DiscordRoleAssignments_Remove.sql");

    public static string GetActiveTeamMembersWithDiscordId() => SqlLoader.Load("Queries/TeamMembers_GetActiveWithDiscordId.sql");

    // --- Notifications & join requests ---
    public static string GetPendingApprovalsForAdmin() => SqlLoader.Load("Queries/Invitations_GetPendingApprovalsForAdmin.sql");

    public static string GetRecentUpdatesForUser() => SqlLoader.Load("Queries/Invitations_GetRecentUpdatesForUser.sql");

    public static string GetAllRequestsForUser() => SqlLoader.Load("Queries/Invitations_GetAllForUser.sql");

    public static string GetPendingRequestsForUser() => SqlLoader.Load("Queries/Invitations_GetPendingForUser.sql");
}