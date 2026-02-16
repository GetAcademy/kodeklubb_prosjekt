using Core.DomainEvents;
using Core.State;

namespace Core.Outcomes;

public record UserResult(
    Outcome Outcome,
    UserState NewState,
    IReadOnlyList<IDomainEvent> Events
    );