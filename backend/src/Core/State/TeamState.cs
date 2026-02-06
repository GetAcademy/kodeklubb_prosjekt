namespace Core.State;

public record TeamState(
    Guid TeamId,
    IReadOnlyList<Guid> Members,
    IReadOnlyList<Guid> PendingInvitations
    );