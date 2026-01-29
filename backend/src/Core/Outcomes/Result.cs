using Core.DomainEvents;
using Core.State;

namespace Core.Outcomes;

public readonly record struct Result(
    Outcome Outcome,
    UserState NewState,
    IReadOnlyList<IDomainEvent> Events
    );