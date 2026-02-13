namespace Core.Commands;

public record CreateTeamCommand(
    Guid TeamId,
    string Name,
    string? Description,
    Guid AdminUserId
);