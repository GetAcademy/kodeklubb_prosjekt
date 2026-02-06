namespace Core.Commands;

public record InviteUserToTeamCommand(
    Guid TeamId,
    Guid InvitedUserId,
    Guid InvitedByUserId
    );