namespace Core.Commands;

public record RequestToJoinTeamCommand(
    long TeamId,
    long UserId
    );