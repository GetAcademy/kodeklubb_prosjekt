namespace Persistence.DbModels;

public class TeamTagEntity
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid TagId { get; init; }
    public DateTime CreatedAt { get; init; }
}
