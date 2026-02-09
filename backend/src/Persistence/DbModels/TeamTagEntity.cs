namespace Persistence.DbModels;

public class TeamTagEntity
{
    public long Id { get; init; }
    public long TeamId { get; init; }
    public long TagId { get; init; }
    public DateTime CreatedAt { get; init; }

    public TeamEntity? Team { get; init; }
    public TagEntity? Tag { get; init; }
}
