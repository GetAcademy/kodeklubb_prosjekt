namespace Core.Commands;

public record JoinTeamCommand(
    long TeamId,
    long UserId
    );
