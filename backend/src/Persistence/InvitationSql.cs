namespace Persistence;

public static class InvitationSql
{
    public static string SendInvitation() => SqlLoader.Load("Commands/Invitations_Insert.sql");

    public static string GetById() => SqlLoader.Load("Queries/Invitations_GetById.sql");

    public static string GetIdsByTeamId() => SqlLoader.Load("Queries/Invitations_GetIdsByTeamId.sql");

    public static string GetPendingByTeam() => SqlLoader.Load("Queries/Invitations_GetPendingByTeam.sql");

    public static string ApproveInvitation() => SqlLoader.Load("Commands/Invitations_Approve.sql");

    public static string DeclineInvitation() => SqlLoader.Load("Commands/Invitations_Decline.sql");

    public static string DeletePendingInvitation() => SqlLoader.Load("Commands/Invitations_DeletePending.sql");
}