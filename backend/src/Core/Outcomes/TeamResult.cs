using Core.DomainEvents;
using Core.State;

namespace Core.Outcomes;

public record TeamResult(
    Outcome Outcome,
    TeamState NewState,
    IReadOnlyList<IDomainEvent> Events);