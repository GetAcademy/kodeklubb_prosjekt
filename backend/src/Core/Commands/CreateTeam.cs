namespace Core.Commands;

public record CreateTeamCommand(
    string Name,
    string? Description,
    long AdminUserId
    );
