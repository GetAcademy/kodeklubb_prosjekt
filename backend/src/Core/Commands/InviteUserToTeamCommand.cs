namespace Core.Commands;

public record InviteUserToTeamCommand(
    Guid TeamId,
    Guid UserId,
    Guid InvitedByUserId
    );