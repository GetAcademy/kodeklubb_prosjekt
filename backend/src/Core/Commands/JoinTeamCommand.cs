namespace Core.Commands;

public record JoinTeamCommand(
    Guid TeamId,
    Guid UserId
    );
