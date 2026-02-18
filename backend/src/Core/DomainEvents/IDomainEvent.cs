namespace Core.DomainEvents;

public interface IDomainEvent
{
    public DateTime OccurredAt { get; }
}