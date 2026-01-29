namespace Core.DomainEvents;

public interface IDomainEvent
{
    public DateTime TimeStamp { get; }
}