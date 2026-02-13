namespace Core.Commands;

public record RequestToJoinTeamCommand(
    Guid TeamId,
    Guid UserId
    );
